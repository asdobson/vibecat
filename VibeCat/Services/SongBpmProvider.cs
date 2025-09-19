using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using VibeCat.Models;

namespace VibeCat.Services;

public class SongBpmProvider : IBpmProvider
{
    private readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36" },
            { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" },
            { "Origin", "https://songbpm.com" },
            { "Referer", "https://songbpm.com/" }
        }
    };

    private readonly ConcurrentDictionary<string, (float bpm, DateTime timestamp)> _cache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public async Task<float?> GetBpmAsync(string artist, string song, string? spotifyTrackId = null)
    {
        DebugLogger.Debug("SongBpmProvider", () => $"BPM request for: {artist} - {song} (Spotify ID: {spotifyTrackId ?? "none"})");
        var cacheKey = spotifyTrackId ?? $"{artist}-{song}";

        if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.Now - cached.timestamp < _cacheExpiration)
        {
            DebugLogger.Debug("SongBpmProvider", () => $"Cache hit for {cacheKey}: {cached.bpm} BPM");
            return cached.bpm;
        }

        DebugLogger.Debug("SongBpmProvider", () => $"Cache miss, searching songbpm.com for: {artist} - {song}");
        var results = await SearchSongsAsync($"{artist} - {song}");

        var bpm = spotifyTrackId != null
            ? results.FirstOrDefault(r => r.SpotifyTrackId == spotifyTrackId)?.Bpm
            : null;

        bpm ??= results.FirstOrDefault(r => r.Bpm.HasValue)?.Bpm;

        if (bpm.HasValue)
        {
            _cache[cacheKey] = (bpm.Value, DateTime.Now);
            DebugLogger.Info("SongBpmProvider", () => $"Found BPM for {artist} - {song}: {bpm} BPM");
        }
        else
        {
            DebugLogger.Warn("SongBpmProvider", () => $"No BPM found for: {artist} - {song}");
        }

        CleanupCache();
        return bpm;
    }

    private async Task<List<SongResult>> SearchSongsAsync(string query)
    {
        try
        {
            DebugLogger.Debug("SongBpmProvider", () => $"Sending POST to https://songbpm.com/searches with query: {query}");
            var response = await _httpClient.PostAsync(
                "https://songbpm.com/searches",
                new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("query", query) })
            );
            DebugLogger.Debug("SongBpmProvider", () => $"Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                DebugLogger.Warn("SongBpmProvider", () => $"Failed response from songbpm.com: {response.StatusCode}");
                return new List<SongResult>();
            }

            var html = await response.Content.ReadAsStringAsync();
            DebugLogger.Debug("SongBpmProvider", () => $"Received HTML response, length: {html.Length} chars");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var songCards = doc.DocumentNode.SelectNodes("//div[contains(@class, 'bg-card')]");
            if (songCards == null)
            {
                DebugLogger.Warn("SongBpmProvider", "No song cards found in HTML response");
                return new List<SongResult>();
            }

            var results = songCards
                .Select(ParseSongCard)
                .Where(r => r != null)
                .ToList()!;
            DebugLogger.Debug("SongBpmProvider", () => $"Found {results.Count} songs in search results");
            return results;
        }
        catch (Exception ex)
        {
            DebugLogger.Error("SongBpmProvider", "Failed to search songs", ex);
            return new List<SongResult>();
        }
    }

    private SongResult? ParseSongCard(HtmlNode card)
    {
        var result = new SongResult
        {
            Artist = card.SelectSingleNode(".//p[contains(@class, 'font-light') and contains(@class, 'uppercase')]")?.InnerText.Trim(),
            Title = card.SelectSingleNode(".//p[contains(@class, 'text-lg') or contains(@class, 'text-2xl')]")?.InnerText.Trim()
        };

        var styleAttr = card.SelectSingleNode(".//figure[@style]")?.GetAttributeValue("style", "");
        if (!string.IsNullOrEmpty(styleAttr))
        {
            var match = Regex.Match(styleAttr, @"url\((.*?)\)");
            if (match.Success)
                result.ImageUrl = match.Groups[1].Value;
        }

        var metricDivs = card.SelectNodes(".//div[contains(@class, 'flex') and contains(@class, 'flex-col') and contains(@class, 'items-center')]");
        if (metricDivs != null)
        {
            foreach (var div in metricDivs)
            {
                var label = div.SelectSingleNode(".//span[contains(@class, 'uppercase')]")?.InnerText.Trim().ToUpper();
                var value = div.SelectSingleNode(".//span[contains(@class, 'text-2xl') or contains(@class, 'text-3xl')]")?.InnerText.Trim();

                if (label == null || value == null) continue;

                switch (label)
                {
                    case "BPM" when float.TryParse(value, out var bpm):
                        result.Bpm = bpm;
                        break;
                    case "KEY":
                        result.Key = value;
                        break;
                    case "DURATION":
                        result.Duration = value;
                        break;
                }
            }
        }

        var spotifyHref = card.SelectSingleNode(".//a[contains(@href, 'spotify.com/track/')]")?.GetAttributeValue("href", "");
        if (!string.IsNullOrEmpty(spotifyHref))
        {
            var match = Regex.Match(spotifyHref, @"track/([a-zA-Z0-9]+)");
            if (match.Success)
                result.SpotifyTrackId = match.Groups[1].Value;
        }

        return result.Artist != null || result.Title != null || result.Bpm != null ? result : null;
    }

    private void CleanupCache()
    {
        var expiredKeys = _cache
            .Where(kvp => DateTime.Now - kvp.Value.timestamp > _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
            _cache.TryRemove(key, out _);
    }
}