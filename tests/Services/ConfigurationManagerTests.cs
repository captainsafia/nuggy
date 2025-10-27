using Nuggy.Models;
using Nuggy.Services;
using Shouldly;
using System.Text.Json;

namespace Nuggy.Tests.Services;

public class ConfigurationManagerTests : IDisposable
{
    private readonly string _tempHomeDirectory;
    private readonly string _testConfigDirectory;
    private readonly string _testSettingsPath;
    private readonly ConfigurationManager _configManager;

    public ConfigurationManagerTests()
    {
        // Create a temporary directory for testing
        _tempHomeDirectory = Path.Combine(Path.GetTempPath(), "nuggy-tests-" + Guid.NewGuid().ToString("N")[..8]);
        _testConfigDirectory = Path.Combine(_tempHomeDirectory, ".nuggy");
        _testSettingsPath = Path.Combine(_testConfigDirectory, "settings.json");

        // Ensure the test directory structure exists
        Directory.CreateDirectory(_tempHomeDirectory);

        // Create a configuration manager that uses the temporary home directory
        _configManager = new ConfigurationManager(_tempHomeDirectory);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ShouldReturnDefaultSettings()
    {
        // Arrange
        // File doesn't exist by default in our temp directory

        // Act
        var settings = await _configManager.LoadSettingsAsync();

        // Assert
        settings.ShouldNotBeNull();
        settings.Feeds.ShouldNotBeEmpty();
        settings.Feeds.ShouldContain(f => f.Name == "nuget.org" && f.IsDefault);
        settings.ConfigDirectory.ShouldBe(_testConfigDirectory);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileExists_ShouldLoadFromFile()
    {
        // Arrange
        var expectedSettings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = [
                new FeedConfiguration
                {
                    Name = "custom-feed",
                    Source = "https://custom.com/v3/index.json",
                    IsDefault = true,
                }
            ]
        };

        var json = JsonSerializer.Serialize(expectedSettings, AppSettingsJsonContext.Default.AppSettings);
        Directory.CreateDirectory(_testConfigDirectory);
        await File.WriteAllTextAsync(_testSettingsPath, json);

        // Act
        var settings = await _configManager.LoadSettingsAsync();

        // Assert
        settings.ShouldNotBeNull();
        settings.Feeds.Count.ShouldBe(1);
        settings.Feeds[0].Name.ShouldBe("custom-feed");
        settings.Feeds[0].Source.ShouldBe("https://custom.com/v3/index.json");
        settings.Feeds[0].IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveSettingsAsync_ShouldSerializeAndWriteToFile()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = [
                new FeedConfiguration
                {
                    Name = "test-feed",
                    Source = "https://test.com/v3/index.json",
                    IsDefault = false,
                }
            ]
        };

        // Act
        await _configManager.SaveSettingsAsync(settings);

        // Assert
        File.Exists(_testSettingsPath).ShouldBeTrue();
        var writtenContent = await File.ReadAllTextAsync(_testSettingsPath);
        writtenContent.ShouldNotBeNull();

        var deserializedSettings = JsonSerializer.Deserialize(writtenContent, AppSettingsJsonContext.Default.AppSettings);
        deserializedSettings!.Feeds.Count.ShouldBe(1);
        deserializedSettings.Feeds[0].Name.ShouldBe("test-feed");
    }

    [Fact]
    public async Task AddFeedAsync_ShouldAddFeedToExistingSettings()
    {
        // Arrange
        var existingSettings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = [
                new FeedConfiguration
                {
                    Name = "existing-feed",
                    Source = "https://existing.com/v3/index.json",
                    IsDefault = true,
                }
            ]
        };

        SetupExistingSettings(existingSettings);

        var newFeed = new FeedConfiguration
        {
            Name = "new-feed",
            Source = "https://new.com/v3/index.json",
            IsDefault = false
        };

        // Act
        await _configManager.AddFeedAsync(newFeed);

        // Assert
        var savedSettings = await GetSavedSettings();
        savedSettings.Feeds.Count.ShouldBe(2);
        savedSettings.Feeds.ShouldContain(f => f.Name == "existing-feed");
        savedSettings.Feeds.ShouldContain(f => f.Name == "new-feed");
    }

    [Fact]
    public async Task GetFeedByNameAsync_WhenFeedExists_ShouldReturnFeed()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = [
                new FeedConfiguration
                {
                    Name = "target-feed",
                    Source = "https://target.com/v3/index.json",
                    IsDefault = false,
                }
            ]
        };

        SetupExistingSettings(settings);

        // Act
        var feed = await _configManager.GetFeedByNameAsync("target-feed");

        // Assert
        feed.ShouldNotBeNull();
        feed!.Name.ShouldBe("target-feed");
        feed.Source.ShouldBe("https://target.com/v3/index.json");
    }

    [Fact]
    public async Task GetFeedByNameAsync_WhenFeedDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = []
        };

        SetupExistingSettings(settings);

        // Act
        var feed = await _configManager.GetFeedByNameAsync("non-existent-feed");

        // Assert
        feed.ShouldBeNull();
    }

    [Fact]
    public async Task GetDefaultFeedAsync_WhenDefaultFeedExists_ShouldReturnDefaultFeed()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = [
                new FeedConfiguration
                {
                    Name = "feed1",
                    Source = "https://feed1.com/v3/index.json",
                    IsDefault = false,
                },
                new FeedConfiguration
                {
                    Name = "default-feed",
                    Source = "https://default.com/v3/index.json",
                    IsDefault = true,
                }
            ]
        };

        SetupExistingSettings(settings);

        // Act
        var defaultFeed = await _configManager.GetDefaultFeedAsync();

        // Assert
        defaultFeed.ShouldNotBeNull();
        defaultFeed!.Name.ShouldBe("default-feed");
        defaultFeed.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task SetDefaultFeedAsync_ShouldUpdateDefaultFeed()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = [
                new FeedConfiguration
                {
                    Name = "feed1",
                    Source = "https://feed1.com/v3/index.json",
                    IsDefault = true,
                },
                new FeedConfiguration
                {
                    Name = "feed2",
                    Source = "https://feed2.com/v3/index.json",
                    IsDefault = false,
                }
            ]
        };

        SetupExistingSettings(settings);

        // Act
        await _configManager.SetDefaultFeedAsync("feed2");

        // Assert
        var savedSettings = await GetSavedSettings();
        savedSettings.Feeds.Single(f => f.Name == "feed1").IsDefault.ShouldBeFalse();
        savedSettings.Feeds.Single(f => f.Name == "feed2").IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveFeedAsync_ShouldRemoveFeedFromSettings()
    {
        // Arrange
        var settings = new AppSettings
        {
            ConfigDirectory = _testConfigDirectory,
            Feeds = [
                new FeedConfiguration
                {
                    Name = "feed-to-remove",
                    Source = "https://remove.com/v3/index.json",
                    IsDefault = false,
                },
                new FeedConfiguration
                {
                    Name = "feed-to-keep",
                    Source = "https://keep.com/v3/index.json",
                    IsDefault = true,
                }
            ]
        };

        SetupExistingSettings(settings);

        // Act
        await _configManager.RemoveFeedAsync("feed-to-remove");

        // Assert
        var savedSettings = await GetSavedSettings();
        savedSettings.Feeds.Count.ShouldBe(1);
        savedSettings.Feeds.ShouldNotContain(f => f.Name == "feed-to-remove");
        savedSettings.Feeds.ShouldContain(f => f.Name == "feed-to-keep");
    }

    private void SetupExistingSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
        Directory.CreateDirectory(_testConfigDirectory);
        File.WriteAllText(_testSettingsPath, json);
    }

    private async Task<AppSettings> GetSavedSettings()
    {
        File.Exists(_testSettingsPath).ShouldBeTrue();
        var json = await File.ReadAllTextAsync(_testSettingsPath);
        return JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings)!;
    }

    public void Dispose()
    {
        // Clean up the temporary test directory
        if (Directory.Exists(_tempHomeDirectory))
        {
            try
            {
                Directory.Delete(_tempHomeDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}