using System.Net;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace VibeCat.Services;

public class SpotifyService
{
    private const string ClientId = "3ff292b74f6340f4895b157cddd4947b";
    private const string RedirectUri = "http://127.0.0.1:5543/callback";
    private const int CallbackPort = 5543;

    private SpotifyClient? _spotify;
    private PKCEAuthenticator? _authenticator;
    private string? _refreshToken;
    private HttpListener? _httpListener;
    private CancellationTokenSource? _pollingCancellationTokenSource;

    public event EventHandler<CurrentlyPlaying?>? CurrentTrackChanged;
    public event EventHandler<bool>? ConnectionStatusChanged;

    public bool IsConnected => _spotify != null;
    public CurrentlyPlaying? CurrentTrack { get; private set; }

    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var (verifier, challenge) = PKCEUtil.GenerateCodes();
            var loginRequest = new LoginRequest(new Uri(RedirectUri), ClientId, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = challenge,
                CodeChallengeMethod = "S256",
                Scope = new[] { Scopes.UserReadPlaybackState, Scopes.UserReadCurrentlyPlaying }
            };

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = loginRequest.ToUri().ToString(),
                UseShellExecute = true
            });

            var authCode = await WaitForCallbackAsync();
            if (string.IsNullOrEmpty(authCode)) return false;

            var tokenResponse = await new OAuthClient().RequestToken(
                new PKCETokenRequest(ClientId, authCode, new Uri(RedirectUri), verifier));
            return await InitializeSpotifyClient(tokenResponse);
        }
        catch { return false; }
    }

    private async Task<string?> WaitForCallbackAsync()
    {
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://127.0.0.1:{CallbackPort}/");
        _httpListener.Start();

        try
        {
            var context = await _httpListener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            var html = "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;text-align:center;margin-top:50px'>" +
                      "<h2>&#10003; Authentication successful!</h2><p>You can close this window.</p></body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(html);

            var response = context.Response;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            return code;
        }
        finally { _httpListener.Stop(); }
    }

    public async Task<bool> ConnectWithRefreshTokenAsync(string refreshToken)
    {
        try
        {
            _refreshToken = refreshToken;
            var tokenResponse = await new OAuthClient().RequestToken(
                new PKCETokenRefreshRequest(ClientId, refreshToken));
            return await InitializeSpotifyClient(tokenResponse);
        }
        catch { return false; }
    }

    private Task<bool> InitializeSpotifyClient(PKCETokenResponse tokenResponse)
    {
        _refreshToken = tokenResponse.RefreshToken;
        _authenticator = new PKCEAuthenticator(ClientId, tokenResponse);
        _authenticator.TokenRefreshed += (_, token) => _refreshToken = token.RefreshToken;

        _spotify = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(_authenticator));

        ConnectionStatusChanged?.Invoke(this, true);
        StartPolling();
        return Task.FromResult(true);
    }

    private void StartPolling()
    {
        _pollingCancellationTokenSource?.Cancel();
        _pollingCancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            var token = _pollingCancellationTokenSource.Token;
            while (!token.IsCancellationRequested && _spotify != null)
            {
                try
                {
                    var current = await _spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                    if (HasTrackChanged(current))
                    {
                        CurrentTrack = current;
                        CurrentTrackChanged?.Invoke(this, current);
                    }
                    await Task.Delay(1500, token);
                }
                catch { await Task.Delay(3000, token); }
            }
        });
    }

    private bool HasTrackChanged(CurrentlyPlaying? current)
    {
        var currentId = (current?.Item as FullTrack)?.Id;
        var previousId = (CurrentTrack?.Item as FullTrack)?.Id;
        return currentId != previousId;
    }

    public void Disconnect()
    {
        _pollingCancellationTokenSource?.Cancel();
        _spotify = null;
        _authenticator = null;
        CurrentTrack = null;
        ConnectionStatusChanged?.Invoke(this, false);
    }

    public string? GetRefreshToken() => _refreshToken;

    public async Task<TrackAudioFeatures?> GetAudioFeaturesAsync(string trackId)
    {
        try
        {
            if (_spotify == null || string.IsNullOrEmpty(trackId))
                return null;

            return await _spotify.Tracks.GetAudioFeatures(trackId).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}