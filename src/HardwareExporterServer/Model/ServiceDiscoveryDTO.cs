// // 张锐志 2023-10-09
using Newtonsoft.Json;

namespace HardwareExporterServer.Model;

public record ServiceDiscoveryDTO
{
    [JsonProperty("targets")]
    public IEnumerable<string> Targets { get; set; }
    [JsonProperty("labels")]
    public Dictionary<string, string> Labels { get; set; }
}