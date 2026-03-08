namespace HardwareExporterWeb.Configuration;

/// <summary>
/// Configuration options for network scanning
/// </summary>
public class NetworkScanOptions
{
    public const string SectionName = "NetworkScan";

    /// <summary>
    /// Optional subnet filter (e.g., "10.100.100" or "192.168.1")
    /// If not set, scans all local subnets
    /// </summary>
    public string? SubnetFilter { get; set; }

    /// <summary>
    /// Subnet mask for scanning (default: 255.255.255.0)
    /// </summary>
    public string SubnetMask { get; set; } = "255.255.255.0";
}
