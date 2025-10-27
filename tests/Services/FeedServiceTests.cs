using Nuggy.Models;
using Nuggy.Services;
using Nuggy.Tests.Utilities;
using Shouldly;
using System.Text.Json;

namespace Nuggy.Tests.Services;

public class FeedServiceTests : IDisposable
{
    private readonly string _tempHomeDirectory;
    private readonly string _testConfigDirectory;
    private readonly string _testSettingsPath;
    private readonly ConfigurationManager _configManager;
    private readonly FeedService _feedService;

    public FeedServiceTests()
    {
        // Create a temporary directory for testing
        _tempHomeDirectory = Path.Combine(Path.GetTempPath(), "nuggy-feedtests-" + Guid.NewGuid().ToString("N")[..8]);
        _testConfigDirectory = Path.Combine(_tempHomeDirectory, ".nuggy");
        _testSettingsPath = Path.Combine(_testConfigDirectory, "settings.json");

        // Ensure the test directory structure exists
        Directory.CreateDirectory(_tempHomeDirectory);

        // Create real instances using the temporary directory
        _configManager = new ConfigurationManager(_tempHomeDirectory);
        _feedService = new FeedService(_configManager);
    }

    [Fact]
    public async Task GetAllFeedsAsync_ShouldReturnFeedsFromConfigurationManager()
    {
        // Arrange
        var expectedFeeds = TestDataBuilders.AppSettingsBuilder.WithFeeds(
            TestDataBuilders.FeedConfigurationBuilder.NuGetOrg(),
            TestDataBuilders.FeedConfigurationBuilder.PrivateFeed()
        );

        await SetupExistingSettings(expectedFeeds);

        // Act
        var feeds = await _feedService.GetAllFeedsAsync();

        // Assert
        feeds.ShouldNotBeNull();
        feeds.Count.ShouldBe(2);
        feeds.ShouldContain(f => f.Name == "nuget.org");
        feeds.ShouldContain(f => f.Name == "private-feed");
    }

    [Fact]
    public async Task SetDefaultFeedAsync_ShouldCallConfigurationManager()
    {
        // Arrange
        const string feedName = "test-feed";
        var settings = TestDataBuilders.AppSettingsBuilder.WithFeeds(
            TestDataBuilders.FeedConfigurationBuilder.WithName(feedName),
            TestDataBuilders.FeedConfigurationBuilder.NuGetOrg()
        );
        await SetupExistingSettings(settings);

        // Act
        await _feedService.SetDefaultFeedAsync(feedName);

        // Assert
        var savedSettings = await GetSavedSettings();
        var defaultFeed = savedSettings.Feeds.FirstOrDefault(f => f.IsDefault);
        defaultFeed.ShouldNotBeNull();
        defaultFeed!.Name.ShouldBe(feedName);
    }

    [Fact]
    public async Task GetRepositoryAsync_WithFeedName_ShouldUseSpecifiedFeed()
    {
        // Arrange
        const string feedName = "custom-feed";
        var customFeed = TestDataBuilders.FeedConfigurationBuilder.WithName(feedName);
        var settings = TestDataBuilders.AppSettingsBuilder.WithFeeds(customFeed);
        await SetupExistingSettings(settings);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _feedService.GetRepositoryAsync(feedName));

        // The method should find the specified feed and not throw an exception for invalid feed name
        exception.ShouldBeNull();
    }

    [Fact]
    public async Task GetRepositoryAsync_WithNonExistentFeedName_ShouldThrowArgumentException()
    {
        // Arrange
        const string feedName = "non-existent-feed";
        var settings = TestDataBuilders.AppSettingsBuilder.WithFeeds(
            TestDataBuilders.FeedConfigurationBuilder.NuGetOrg()
        );
        await SetupExistingSettings(settings);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _feedService.GetRepositoryAsync(feedName));
        exception.Message.ShouldContain($"Feed '{feedName}' not found");
    }

    [Fact]
    public async Task GetRepositoryAsync_WithoutFeedName_ShouldUseDefaultFeed()
    {
        // Arrange - Set up with a specific default feed
        var defaultFeed = TestDataBuilders.FeedConfigurationBuilder.WithName("default-test-feed");
        defaultFeed.IsDefault = true;
        var settings = TestDataBuilders.AppSettingsBuilder.WithFeeds(defaultFeed);
        await SetupExistingSettings(settings);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _feedService.GetRepositoryAsync());

        // Should not throw an exception and should use the default feed
        exception.ShouldBeNull();
    }

    private async Task SetupExistingSettings(AppSettings settings)
    {
        // Set the correct config directory for our temp location
        settings.ConfigDirectory = _testConfigDirectory;

        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
        Directory.CreateDirectory(_testConfigDirectory);
        await File.WriteAllTextAsync(_testSettingsPath, json);

        // Clear any cached settings in the config manager
        await _configManager.LoadSettingsAsync();
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