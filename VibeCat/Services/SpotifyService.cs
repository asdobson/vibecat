using System.Linq;
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
    public bool? IsPlaying { get; private set; }

    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            DebugLogger.Info("SpotifyService", "Starting authentication flow");
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
            if (string.IsNullOrEmpty(authCode))
            {
                DebugLogger.Warn("SpotifyService", "No auth code received from callback");
                return false;
            }
            DebugLogger.Debug("SpotifyService", () => $"Auth code received: {authCode?.Substring(0, 10)}...");

            var tokenResponse = await new OAuthClient().RequestToken(
                new PKCETokenRequest(ClientId, authCode, new Uri(RedirectUri), verifier));
            return await InitializeSpotifyClient(tokenResponse);
        }
        catch (Exception ex)
        {
            DebugLogger.Error("SpotifyService", "Authentication failed", ex);
            return false;
        }
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
        catch (Exception ex)
        {
            DebugLogger.Error("SpotifyService", "Failed to connect with refresh token", ex);
            return false;
        }
    }

    private Task<bool> InitializeSpotifyClient(PKCETokenResponse tokenResponse)
    {
        _refreshToken = tokenResponse.RefreshToken;
        DebugLogger.Info("SpotifyService", () => $"Token received - Expires in: {tokenResponse.ExpiresIn}s, Has refresh token: {!string.IsNullOrEmpty(tokenResponse.RefreshToken)}");

        _authenticator = new PKCEAuthenticator(ClientId, tokenResponse);
        _authenticator.TokenRefreshed += (_, token) =>
        {
            _refreshToken = token.RefreshToken;
            DebugLogger.Info("SpotifyService", () => $"Token refreshed - New expiry: {token.ExpiresIn}s");
        };

        _spotify = new SpotifyClient(SpotifyClientConfig.CreateDefault().WithAuthenticator(_authenticator));

        ConnectionStatusChanged?.Invoke(this, true);
        DebugLogger.Info("SpotifyService", "Spotify client initialized successfully");
        StartPolling();
        return Task.FromResult(true);
    }

    private void StartPolling()
    {
        _pollingCancellationTokenSource?.Cancel();
        _pollingCancellationTokenSource = new CancellationTokenSource();
        DebugLogger.Info("SpotifyService", "Starting polling loop");

        Task.Run(async () =>
        {
            var token = _pollingCancellationTokenSource.Token;
            var pollCount = 0;
            DebugLogger.Debug("SpotifyService", () => $"Polling thread started - Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            while (!token.IsCancellationRequested && _spotify != null)
            {
                try
                {
                    pollCount++;
                    DebugLogger.Debug("SpotifyService", () => $"Poll attempt {pollCount}: Getting current playback");
                    var current = await _spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                    DebugLogger.Debug("SpotifyService", () => $"Poll {pollCount} response: Item={current?.Item != null}, IsPlaying={current?.IsPlaying}, Type={current?.CurrentlyPlayingType}");
                    if (HasTrackChanged(current) || HasPlaybackStateChanged(current))
                    {
                        var track = current?.Item as SpotifyAPI.Web.FullTrack;
                        DebugLogger.Info("SpotifyService", () => $"Track changed: {track?.Name} by {string.Join(", ", track?.Artists?.Select(a => a.Name) ?? new[] { "Unknown" })}");
                        CurrentTrack = current;
                        IsPlaying = current?.IsPlaying;
                        CurrentTrackChanged?.Invoke(this, current);
                    }
                    await Task.Delay(500, token);
                }
                catch (Exception ex)
                {
                    DebugLogger.Error("SpotifyService", () => $"Polling error at attempt {pollCount}", ex);
                    DebugLogger.Debug("SpotifyService", "Waiting 3 seconds before retry...");
                    await Task.Delay(3000, token);
                }
            }
            DebugLogger.Warn("SpotifyService", () => $"Polling loop exited - Token cancelled: {token.IsCancellationRequested}, Spotify null: {_spotify == null}");
        });
    }

    private bool HasTrackChanged(CurrentlyPlaying? current)
    {
        var currentId = (current?.Item as FullTrack)?.Id;
        var previousId = (CurrentTrack?.Item as FullTrack)?.Id;
        return currentId != previousId;
    }

    private bool HasPlaybackStateChanged(CurrentlyPlaying? current) =>
        current?.IsPlaying != IsPlaying;

    public void Disconnect()
    {
        DebugLogger.Info("SpotifyService", "Disconnecting from Spotify");
        _pollingCancellationTokenSource?.Cancel();
        _spotify = null;
        _authenticator = null;
        CurrentTrack = null;
        IsPlaying = null;
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