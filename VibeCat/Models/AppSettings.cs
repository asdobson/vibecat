using System.Text.Json.Serialization;

namespace VibeCat.Models;

public class AppSettings
{
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 533;
    public double WindowHeight { get; set; } = 400;

    public double Opacity { get; set; } = 1.0;
    public double BPM { get; set; } = 120;

    public bool IsSnappingEnabled { get; set; } = true;
    public double SnapDistance { get; set; } = 20;

    public bool IsAutoFlipEnabled { get; set; } = true;
    public bool IsFlipped { get; set; } = false;

    public bool IsClickThrough { get; set; } = false;
    public bool SpotifySyncEnabled { get; set; } = false;

    public bool EnableFadeOnPlaybackChange { get; set; } = true;
    public double FadeAnimationDuration { get; set; } = 5.0;

    [JsonIgnore]
    public string? SpotifyRefreshToken { get; set; }

    public string? EncryptedSpotifyToken { get; set; }
}