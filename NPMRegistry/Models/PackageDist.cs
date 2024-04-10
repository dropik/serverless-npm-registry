using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class PackageDist
{
    [JsonPropertyName("shasum")]
    public string SHASum { get; set; } = "";

    [JsonPropertyName("tarball")]
    public string Tarball { get; set; } = "";
}