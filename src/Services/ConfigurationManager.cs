using Nuggy.Models;
using System.Text.Json;

namespace Nuggy.Services;

public class ConfigurationManager
{
    private readonly string _configDirectory;
    private readonly string _settingsFilePath;
    private AppSettings? _cachedSettings;
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigurationManager(string? homeDirectory = null)
    {
        homeDirectory ??= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDirectory = Path.Combine(homeDirectory, ".nuggy");
        _settingsFilePath = Path.Combine(_configDirectory, "settings.json");

        EnsureConfigDirectoryExists();
    }

    public string ConfigDirectory => _configDirectory;

    private void EnsureConfigDirectoryExists()
    {
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        if (!File.Exists(_settingsFilePath))
        {
            _cachedSettings = new AppSettings
            {
                ConfigDirectory = _configDirectory,
                Feeds =
                [
                    new()
                    {
                        Name = "nuget.org",
                        Source = "https://api.nuget.org/v3/index.json",
                        IsDefault = true
                    }
                ],
                DefaultFeed = "nuget.org"
            };

            await SaveSettingsAsync(_cachedSettings);
            return _cachedSettings;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            _cachedSettings.ConfigDirectory = _configDirectory;

            // Ensure we have at least the default NuGet feed
            if (_cachedSettings.Feeds.Count == 0)
            {
                _cachedSettings.Feeds.Add(new FeedConfiguration
                {
                    Name = "nuget.org",
                    Source = "https://api.nuget.org/v3/index.json",
                    IsDefault = true
                });
                _cachedSettings.DefaultFeed = "nuget.org";
                await SaveSettingsAsync(_cachedSettings);
            }

            return _cachedSettings;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load configuration: {ex.Message}", ex);
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, jsonSerializerOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _cachedSettings = settings;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    public async Task<FeedConfiguration?> GetDefaultFeedAsync()
    {
        var settings = await LoadSettingsAsync();
        return settings.Feeds.FirstOrDefault(f => f.Name == settings.DefaultFeed)
               ?? settings.Feeds.FirstOrDefault(f => f.IsDefault)
               ?? settings.Feeds.FirstOrDefault();
    }

    public async Task<FeedConfiguration?> GetFeedByNameAsync(string name)
    {
        var settings = await LoadSettingsAsync();
        return settings.Feeds.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddFeedAsync(FeedConfiguration feed)
    {
        var settings = await LoadSettingsAsync();

        // Remove existing feed with same name if it exists
        settings.Feeds.RemoveAll(f => f.Name.Equals(feed.Name, StringComparison.OrdinalIgnoreCase));
        settings.Feeds.Add(feed);

        await SaveSettingsAsync(settings);
    }

    public async Task SetDefaultFeedAsync(string feedName)
    {
        var settings = await LoadSettingsAsync();
        var feed = settings.Feeds.FirstOrDefault(f => f.Name.Equals(feedName, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"Feed '{feedName}' not found.");

        // Update default settings
        foreach (var f in settings.Feeds)
        {
            f.IsDefault = f.Name.Equals(feedName, StringComparison.OrdinalIgnoreCase);
        }

        settings.DefaultFeed = feedName;
        await SaveSettingsAsync(settings);
    }

    public async Task RemoveFeedAsync(string feedName)
    {
        var settings = await LoadSettingsAsync();
        var removed = settings.Feeds.RemoveAll(f => f.Name.Equals(feedName, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
        {
            throw new ArgumentException($"Feed '{feedName}' not found.");
        }

        // If we removed the default feed, set a new default
        if (settings.DefaultFeed?.Equals(feedName, StringComparison.OrdinalIgnoreCase) == true)
        {
            var newDefault = settings.Feeds.FirstOrDefault();
            if (newDefault != null)
            {
                newDefault.IsDefault = true;
                settings.DefaultFeed = newDefault.Name;
            }
            else
            {
                settings.DefaultFeed = null;
            }
        }

        await SaveSettingsAsync(settings);
    }
}