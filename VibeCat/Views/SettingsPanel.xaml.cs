using System.Windows;
using System.Windows.Controls;
using VibeCat.Services;
using VibeCat.Models;
using SpotifyAPI.Web;

namespace VibeCat.Views;

public partial class SettingsPanel : UserControl
{
    public event EventHandler<double>? OpacityChanged;
    public event EventHandler<bool>? SnappingEnabledChanged;
    public event EventHandler<double>? SnapDistanceChanged;
    public event EventHandler<bool>? AutoFlipEnabledChanged;
    public event EventHandler? ManualFlipRequested;
    public event EventHandler<double>? BPMChanged;
    public event EventHandler<bool>? ClickThroughChanged;
    public event EventHandler? SpotifyConnectRequested;
    public event EventHandler? SpotifyDisconnectRequested;
    public event EventHandler<bool>? SpotifySyncEnabledChanged;
    public event EventHandler<bool>? FadeEnabledChanged;
    public event EventHandler<double>? FadeDurationChanged;

    private SpotifyService? _spotifyService;
    private IBpmProvider? _bpmProvider;
    private AppSettings? _settings;

    public SettingsPanel()
    {
        InitializeComponent();
        _bpmProvider = new SongBpmProvider();
    }

    public void LoadSettings(AppSettings settings)
    {
        _settings = settings;
        if (OpacitySlider != null) OpacitySlider.Value = settings.Opacity * 100;
        if (SnappingEnabledCheckBox != null) SnappingEnabledCheckBox.IsChecked = settings.IsSnappingEnabled;
        if (SnapDistanceSlider != null) SnapDistanceSlider.Value = settings.SnapDistance;
        if (AutoFlipEnabledCheckBox != null) AutoFlipEnabledCheckBox.IsChecked = settings.IsAutoFlipEnabled;
        if (ManualFlipButton != null) ManualFlipButton.IsEnabled = !settings.IsAutoFlipEnabled;
        if (BPMSlider != null) BPMSlider.Value = settings.BPM;
        if (ClickThroughCheckBox != null) ClickThroughCheckBox.IsChecked = settings.IsClickThrough;
        if (SpotifySyncEnabledCheckBox != null) SpotifySyncEnabledCheckBox.IsChecked = settings.SpotifySyncEnabled;
        if (FadeEnabledCheckBox != null) FadeEnabledCheckBox.IsChecked = settings.EnableFadeOnPlaybackChange;
        if (FadeDurationSlider != null) FadeDurationSlider.Value = settings.FadeAnimationDuration;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        OpacityChanged?.Invoke(this, e.NewValue / 100.0);

    private void SnappingEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            SnappingEnabledChanged?.Invoke(this, checkBox.IsChecked ?? true);
    }

    private void SnapDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        SnapDistanceChanged?.Invoke(this, e.NewValue);

    private void AutoFlipEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        var isEnabled = checkBox.IsChecked ?? true;
        AutoFlipEnabledChanged?.Invoke(this, isEnabled);
        if (ManualFlipButton != null) ManualFlipButton.IsEnabled = !isEnabled;
    }

    private void ManualFlipButton_Click(object sender, RoutedEventArgs e) =>
        ManualFlipRequested?.Invoke(this, EventArgs.Empty);

    private void BPMSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        BPMChanged?.Invoke(this, e.NewValue);

    public double BPM
    {
        get => BPMSlider?.Value ?? 115;
        set { if (BPMSlider != null) BPMSlider.Value = Math.Max(60, Math.Min(180, value)); }
    }

    private void ClickThroughCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            ClickThroughChanged?.Invoke(this, checkBox.IsChecked ?? false);
    }

    public void UpdateClickThroughState(bool isClickThrough)
    {
        if (ClickThroughCheckBox != null) ClickThroughCheckBox.IsChecked = isClickThrough;
    }

    public void SetSpotifyService(SpotifyService spotifyService)
    {
        _spotifyService = spotifyService;
        _spotifyService.ConnectionStatusChanged += OnSpotifyConnectionStatusChanged;
        _spotifyService.CurrentTrackChanged += OnSpotifyCurrentTrackChanged;
        UpdateSpotifyUI();
    }

    private void SpotifyConnectButton_Click(object sender, RoutedEventArgs e) =>
        (_spotifyService?.IsConnected == true ? SpotifyDisconnectRequested : SpotifyConnectRequested)
            ?.Invoke(this, EventArgs.Empty);

    private void SpotifySyncEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        SpotifySyncEnabledChanged?.Invoke(this, checkBox.IsChecked ?? false);
        if (BPMSlider != null) BPMSlider.IsEnabled = checkBox.IsChecked != true;
    }

    private void OnSpotifyConnectionStatusChanged(object? sender, bool isConnected) =>
        Dispatcher.Invoke(UpdateSpotifyUI);

    private void OnSpotifyCurrentTrackChanged(object? sender, CurrentlyPlaying? context) =>
        Dispatcher.Invoke(() => UpdateSpotifyTrackInfo(context));

    private void UpdateSpotifyUI()
    {
        if (_spotifyService == null) return;
        var isConnected = _spotifyService.IsConnected;

        if (SpotifyConnectButton != null)
            SpotifyConnectButton.Content = isConnected ? "Disconnect from Spotify" : "Connect to Spotify";

        if (SpotifyStatusText != null)
        {
            SpotifyStatusText.Text = isConnected ? "Connected" : "Not connected";
            SpotifyStatusText.Visibility = isConnected ? Visibility.Visible : Visibility.Collapsed;
        }

        if (SpotifySyncEnabledCheckBox != null) SpotifySyncEnabledCheckBox.IsEnabled = isConnected;
        if (FadeEnabledCheckBox != null) FadeEnabledCheckBox.IsEnabled = isConnected;
        if (!isConnected && SpotifyTrackInfo != null) SpotifyTrackInfo.Visibility = Visibility.Collapsed;
    }

    private async void UpdateSpotifyTrackInfo(CurrentlyPlaying? context)
    {
        if (context?.Item is not FullTrack track)
        {
            if (SpotifyTrackInfo != null) SpotifyTrackInfo.Visibility = Visibility.Collapsed;
            return;
        }

        if (SpotifyTrackInfo != null) SpotifyTrackInfo.Visibility = Visibility.Visible;
        if (SpotifyTrackName != null) SpotifyTrackName.Text = track.Name ?? "Unknown Track";
        if (SpotifyArtistName != null)
            SpotifyArtistName.Text = track.Artists?.Any() == true
                ? string.Join(", ", track.Artists.Where(a => a?.Name != null).Select(a => a.Name))
                : "Unknown Artist";

        if (SpotifyBPMText != null && _bpmProvider != null)
        {
            var artistName = track.Artists?.FirstOrDefault()?.Name ?? "Unknown";
            var songName = track.Name ?? "Unknown";

            var bpm = await _bpmProvider.GetBpmAsync(artistName, songName, track.Id);

            SpotifyBPMText.Text = bpm.HasValue && bpm.Value > 0
                ? $"Tempo: {bpm.Value:F1} BPM"
                : "Tempo: Unknown";
        }
    }

    private void FadeEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            FadeEnabledChanged?.Invoke(this, checkBox.IsChecked ?? true);
    }

    private void FadeDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
        FadeDurationChanged?.Invoke(this, e.NewValue);
}