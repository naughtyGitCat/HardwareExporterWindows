// // 张锐志 2023-09-15
using NPoco;
namespace HardwareExporterServer.Model;

[TableName("HostInfo")]
[PrimaryKey("id")]
public record HostInfoEntity
{
    public int ID { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string HostIP { get; set; } = string.Empty;
    public int ExporterPort { get; set; }
    public long? CreateTimestamp { get; set; }
    public long? UpdateTimestamp { get; set; }
}

public record HostInfo
{
    public string HostName { get; set; } = string.Empty;
    public string HostIP { get; set; } = string.Empty;
    public int ExporterPort { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}