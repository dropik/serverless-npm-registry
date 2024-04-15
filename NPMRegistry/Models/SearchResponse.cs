using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class SearchResponse
{
    [JsonPropertyName("objects")]
    public List<SearchObject> Objects { get; set; } = new();
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("time")]
    public DateTime Time { get; set; } = DateTime.UtcNow;
}