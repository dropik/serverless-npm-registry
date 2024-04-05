namespace NPMRegistry.Models;

public class RegistryResponse
{
    public string db_name { get; set; } = "";
    public int doc_count { get; set; }
    public int doc_del_count { get; set; }
    public int update_seq { get; set; }
    public int purge_seq { get; set; }
    public bool compact_running { get; set; }
    public int disk_size { get; set; }
    public int data_size { get; set; }
    public string instance_start_time { get; set; } = "";
    public int disk_format_version { get; set; }
    public int committed_update_seq { get; set; }
}
