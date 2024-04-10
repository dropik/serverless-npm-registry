using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class PackageData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";
    
    [JsonPropertyName("author")]
    public User Author { get; set; } = new();
    
    [JsonPropertyName("license")]
    public string License { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
    
    [JsonPropertyName("unity")]
    public string Unity { get; set; } = "";

    [JsonPropertyName("samples")]
    public List<UnitySample> Samples { get; set; } = new();

    [JsonPropertyName("relatedPackages")]
    public Dictionary<string, string> RelatedPackages { get; set; } = new();

    [JsonPropertyName("documentationUrl")]
    public string DocumentationUrl { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("host")]
    public string Host { get; set; } = "";

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new();

    [JsonPropertyName("readme")]
    public string Readme { get; set; } = "";

    [JsonPropertyName("readmeFilename")]
    public string ReadmeFilename { get; set; } = "";

    [JsonPropertyName("scripts")]
    public Dictionary<string, string> Scripts { get; set; } = new();

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}