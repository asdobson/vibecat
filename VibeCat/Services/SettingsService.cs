using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VibeCat.Models;

namespace VibeCat.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VibeCat",
        "settings.json"
    );

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService()
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();

            if (!string.IsNullOrEmpty(settings.EncryptedSpotifyToken))
                settings.SpotifyRefreshToken = Decrypt(settings.EncryptedSpotifyToken);

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            if (!string.IsNullOrEmpty(settings.SpotifyRefreshToken))
                settings.EncryptedSpotifyToken = Encrypt(settings.SpotifyRefreshToken);
            else
                settings.EncryptedSpotifyToken = null;

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
        }
    }

    private static string Encrypt(string text)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Decrypt(string encryptedText)
    {
        try
        {
            var encrypted = Convert.FromBase64String(encryptedText);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}