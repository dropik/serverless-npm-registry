using System.Text.Json.Serialization;

namespace NPMRegistry.Models;

public class RegistryResponse
{
    [JsonPropertyName("db_name")]
    public string DBName { get; set; } = "";

    [JsonPropertyName("doc_count")]
    public int DocCount { get; set; }
    
    [JsonPropertyName("doc_del_count")]
    public int DocDelCount { get; set; }
    
    [JsonPropertyName("update_seq")]
    public int UpdateSeq { get; set; }
    
    [JsonPropertyName("purge_seq")]
    public int PurgeSeq { get; set; }
    
    [JsonPropertyName("compact_running")]
    public bool CompactRunning { get; set; }
    
    [JsonPropertyName("disk_size")]
    public int DiskSize { get; set; }
    
    [JsonPropertyName("data_size")]
    public int DataSize { get; set; }
    
    [JsonPropertyName("instance_start_time")]
    public string InstanceStartTime { get; set; } = "";
    
    [JsonPropertyName("disk_format_version")]
    public int DiskFormatVersion { get; set; }

    [JsonPropertyName("committed_update_seq")]
    public int CommittedUpdateSeq { get; set; }
}
