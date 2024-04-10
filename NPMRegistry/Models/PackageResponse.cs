using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class PackageResponse
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = "";
    
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

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "aws package manager";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
    
    [JsonPropertyName("dist-tags")]
    public Dictionary<string, string> DistTags { get; set; } = new();
    
    [JsonPropertyName("time")]
    public Dictionary<string, string> Time { get; set; } = new();
    
    [JsonPropertyName("versions")]
    public Dictionary<string, PackageVersion> Versions { get; set; } = new();

    [JsonPropertyName("etag")]
    public string ETag { get; set; } = "";
}