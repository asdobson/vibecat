using VibeCat.Models;

namespace VibeCat.Services;

/// <summary>
/// Provides BPM (beats per minute) information for songs.
/// </summary>
public interface IBpmProvider
{
    /// <summary>
    /// Gets the BPM for a specific song.
    /// </summary>
    /// <param name="artist">The artist name. Required.</param>
    /// <param name="song">The song title. Required.</param>
    /// <param name="spotifyTrackId">Optional Spotify track ID for exact matching when multiple versions exist.</param>
    /// <returns>
    /// The BPM value as a float if found, null if not found or on error.
    /// Implementations should handle errors gracefully and return null rather than throwing.
    /// Valid BPM values typically range from 60 to 200.
    /// </returns>
    Task<float?> GetBpmAsync(string artist, string song, string? spotifyTrackId = null);
}