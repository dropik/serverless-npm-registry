using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class PackageResponse : PackageBaseData
{
    [JsonPropertyName("_attachments")] public Dictionary<string, string> Attachments { get; set; } = new();
    [JsonPropertyName("_rev")] public string Rev { get; set; } = "";
    [JsonPropertyName("dist-tags")] public Dictionary<string, string> DistTags { get; set; } = new();
    [JsonPropertyName("time")] public Dictionary<string, string> Time { get; set; } = new();
    [JsonPropertyName("versions")] public Dictionary<string, PackageVersion> Versions { get; set; } = new();
}

public class User
{
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public class PackageVersion : PackageData
{
    [JsonPropertyName("_from")] public string From { get; set; } = ".";
    [JsonPropertyName("_nodeVersion")] public string NodeVersion { get; set; } = "18.17.1";

    [JsonPropertyName("_npmUser")]
    public User NPMUser { get; set; } = new() {Email = "daniil.ryzhkov.97@gmail.com", Name = "Daniil Ryzhkov"};
    
    [JsonPropertyName("_npmVersion")] public string NPMVersion { get; set; } = "10.3.0";
    [JsonPropertyName("_shasum")] public string SHASum { get; set; } = "";
    [JsonPropertyName("directories")] public Dictionary<string, string> Directories { get; set; } = new();
}

public abstract class PackageBaseData
{
    [JsonPropertyName("_id")] public string Id { get; set; } = "";
    [JsonPropertyName("author")] public User Author { get; set; } = new();
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("license")] public string License { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("maintainers")] public List<User> Maintainers { get; set; } = new();
    [JsonPropertyName("readme")] public string Readme { get; set; } = "";
    [JsonPropertyName("readmeFilename")] public string ReadmeFilename { get; set; } = "";
}

public class PackageDist
{
    [JsonPropertyName("shasum")] public string SHASum { get; set; } = "";
    [JsonPropertyName("tarball")] public string Tarball { get; set; } = "";
}

public class PackageData : PackageBaseData
{
    [JsonPropertyName("dist")] public PackageDist Dist { get; set; } = new();
    [JsonPropertyName("main")] public string Main { get; set; } = "";
    [JsonPropertyName("scripts")] public Dictionary<string, string> Scripts { get; set; } = new();
    [JsonPropertyName("version")] public string Version { get; set; } = "";
}