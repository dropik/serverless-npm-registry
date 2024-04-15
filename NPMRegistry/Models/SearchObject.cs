using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class SearchObject
{
    [JsonPropertyName("package")]
    public PackageData Package { get; set; } = new();
}