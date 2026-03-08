namespace HardwareExporterWindows.Configuration;

public class HardwareMonitorOptions
{
    public const string SectionName = "HardwareMonitor";

    /// <summary>
    /// Enable CPU monitoring
    /// </summary>
    public bool EnableCpu { get; set; } = true;

    /// <summary>
    /// Enable GPU monitoring
    /// </summary>
    public bool EnableGpu { get; set; } = true;

    /// <summary>
    /// Enable Memory monitoring
    /// </summary>
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// Enable Motherboard monitoring
    /// </summary>
    public bool EnableMotherboard { get; set; } = true;

    /// <summary>
    /// Enable Controller monitoring
    /// </summary>
    public bool EnableController { get; set; } = true;

    /// <summary>
    /// Enable Network monitoring
    /// </summary>
    public bool EnableNetwork { get; set; } = true;

    /// <summary>
    /// Enable Storage monitoring
    /// </summary>
    public bool EnableStorage { get; set; } = true;

    /// <summary>
    /// Scrape interval in seconds
    /// </summary>
    public int ScrapeIntervalSeconds { get; set; } = 15;
}
