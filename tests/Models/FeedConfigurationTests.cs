using Nuggy.Models;
using Shouldly;
using System.Text.Json;

namespace Nuggy.Tests.Models;

public class FeedConfigurationTests
{
    [Fact]
    public void FeedConfiguration_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var feed = new FeedConfiguration
        {
            Name = "test-feed",
            Source = "https://api.nuget.org/v3/index.json"
        };

        // Assert
        feed.Name.ShouldBe("test-feed");
        feed.Source.ShouldBe("https://api.nuget.org/v3/index.json");
        feed.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void FeedConfiguration_ShouldSerializeToJson()
    {
        // Arrange
        var feed = new FeedConfiguration
        {
            Name = "nuget.org",
            Source = "https://api.nuget.org/v3/index.json",
            IsDefault = true
        };

        // Act
        var json = JsonSerializer.Serialize(feed);
        var deserialized = JsonSerializer.Deserialize<FeedConfiguration>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized!.Name.ShouldBe("nuget.org");
        deserialized.Source.ShouldBe("https://api.nuget.org/v3/index.json");
        deserialized.IsDefault.ShouldBeTrue();
    }


    [Fact]
    public void FeedConfiguration_ShouldDeserializeWithJsonPropertyNames()
    {
        // Arrange
        var json = """
        {
            "name": "test-feed",
            "source": "https://test.com/v3/index.json",
            "isDefault": true
        }
        """;

        // Act
        var feed = JsonSerializer.Deserialize<FeedConfiguration>(json);

        // Assert
        feed.ShouldNotBeNull();
        feed!.Name.ShouldBe("test-feed");
        feed.Source.ShouldBe("https://test.com/v3/index.json");
        feed.IsDefault.ShouldBeTrue();
    }
}