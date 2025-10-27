using System.Text.Json.Serialization;

namespace Nuggy.Models;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(FeedConfiguration))]
[JsonSerializable(typeof(List<FeedConfiguration>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppSettingsJsonContext : JsonSerializerContext
{
}