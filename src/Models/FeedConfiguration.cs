using System.Text.Json.Serialization;

namespace Nuggy.Models;

public class FeedConfiguration
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

}