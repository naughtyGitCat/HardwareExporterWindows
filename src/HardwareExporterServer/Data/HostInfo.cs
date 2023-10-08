// // 张锐志 2023-09-15
using NPoco;
namespace HardwareExporterServer.Data;

[TableName("HostInfo")]
[PrimaryKey("id")]
public class HostInfo
{
    public int ID { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string HostIP { get; set; } = string.Empty;
    public int ExporterPort { get; set; }
    public DateTime CreateTimestamp { get; set; }
    public DateTime UpdateTimestamp { get; set; }
}
