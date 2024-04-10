using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class UnitySample
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";
}