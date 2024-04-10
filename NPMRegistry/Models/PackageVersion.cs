using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class PackageVersion : PackageData
{
    [JsonPropertyName("dist")]
    public PackageDist Dist { get; set; } = new();
}