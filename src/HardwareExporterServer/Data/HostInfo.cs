// // 张锐志 2023-09-15
using NPoco;
namespace HardwareExporterServer.Data;

[TableName("host_info")]
[PrimaryKey("id")]
public class HostInfo
{
    public int ID { get; set; }
    public string HostIP { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}
