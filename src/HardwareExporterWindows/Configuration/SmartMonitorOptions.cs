namespace HardwareExporterWindows.Configuration;

public class SmartMonitorOptions
{
    public const string SectionName = "SmartMonitor";

    public bool Enable { get; set; } = true;

    /// <summary>
    /// Override path to smartctl.exe. Empty = auto-detect from
    /// &lt;exe-dir&gt;\smartctl\smartctl.exe, then PATH.
    /// </summary>
    public string SmartctlPath { get; set; } = string.Empty;

    public int RefreshIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Per-smartctl-invocation timeout. Slow / sleeping HDDs can take a few seconds.
    /// </summary>
    public int InvocationTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Devices matching any of these globs (against /dev/sdN form) are skipped.
    /// Empty = scan all devices reported by --scan-open.
    /// </summary>
    public string[] DeviceExcludePatterns { get; set; } = Array.Empty<string>();
}
