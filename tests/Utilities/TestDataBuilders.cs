using Nuggy.Models;

namespace Nuggy.Tests.Utilities;

public static class TestDataBuilders
{
    public static class FeedConfigurationBuilder
    {
        public static FeedConfiguration Default() => new()
        {
            Name = "test-feed",
            Source = "https://api.nuget.org/v3/index.json",
            IsDefault = false
        };

        public static FeedConfiguration WithName(string name)
        {
            var feed = Default();
            feed.Name = name;
            return feed;
        }

        public static FeedConfiguration WithSource(string source)
        {
            var feed = Default();
            feed.Source = source;
            return feed;
        }

        public static FeedConfiguration AsDefault()
        {
            var feed = Default();
            feed.IsDefault = true;
            return feed;
        }


        public static FeedConfiguration NuGetOrg() => new()
        {
            Name = "nuget.org",
            Source = "https://api.nuget.org/v3/index.json",
            IsDefault = true
        };

        public static FeedConfiguration PrivateFeed() => new()
        {
            Name = "private-feed",
            Source = "https://private.company.com/v3/index.json",
            IsDefault = false
        };
    }

    public static class AppSettingsBuilder
    {
        public static AppSettings Default() => new()
        {
            ConfigDirectory = "/test/.nuggy",
            Feeds = [FeedConfigurationBuilder.NuGetOrg()]
        };

        public static AppSettings WithFeeds(params FeedConfiguration[] feeds)
        {
            var settings = Default();
            settings.Feeds = feeds.ToList();
            return settings;
        }

        public static AppSettings WithConfigDirectory(string directory)
        {
            var settings = Default();
            settings.ConfigDirectory = directory;
            return settings;
        }

        public static AppSettings Empty() => new()
        {
            ConfigDirectory = "/test/.nuggy",
            Feeds = []
        };
    }
}