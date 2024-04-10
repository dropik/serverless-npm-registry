using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class User
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}