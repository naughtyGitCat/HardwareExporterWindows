using System.Diagnostics;
using System.Text.Json;
using HardwareExporterWindows.Configuration;
using Microsoft.Extensions.Options;

namespace HardwareExporterWindows.Services;

public record SmartctlScanDevice(string Name, string Type);

public record AtaAttribute(int Id, string Name, int Value, int Worst, int Threshold, long Raw);

public record NvmeHealthLog(
    int? CriticalWarning,
    int? AvailableSpare,
    int? AvailableSpareThreshold,
    int? PercentageUsed,
    long? DataUnitsRead,
    long? DataUnitsWritten,
    long? HostReads,
    long? HostWrites,
    long? ControllerBusyTime,
    long? PowerCycles,
    long? PowerOnHours,
    long? UnsafeShutdowns,
    long? MediaErrors,
    long? NumErrLogEntries,
    int? WarningTempTime,
    int? CriticalCompTime);

public record SmartctlReport(
    string DeviceName,
    string Protocol,
    string ModelName,
    string SerialNumber,
    string FirmwareVersion,
    long? CapacityBytes,
    bool? HealthPassed,
    int? TemperatureCurrent,
    long? PowerOnHours,
    long? PowerCycleCount,
    int ExitStatus,
    IReadOnlyList<AtaAttribute>? AtaAttributes,
    NvmeHealthLog? NvmeHealth);

public class SmartctlInvoker
{
    private readonly ILogger<SmartctlInvoker> _logger;
    private readonly SmartMonitorOptions _options;
    private readonly string? _smartctlPath;

    public SmartctlInvoker(ILogger<SmartctlInvoker> logger, IOptions<SmartMonitorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _smartctlPath = ResolveSmartctlPath(_options.SmartctlPath);
        if (_smartctlPath != null)
        {
            _logger.LogInformation("smartctl resolved to: {Path}", _smartctlPath);
        }
        else
        {
            _logger.LogWarning("smartctl.exe not found; SMART collection will be disabled");
        }
    }

    public bool IsAvailable => _smartctlPath != null;

    public async Task<IReadOnlyList<SmartctlScanDevice>> ScanAsync(CancellationToken ct)
    {
        if (_smartctlPath == null) return Array.Empty<SmartctlScanDevice>();

        var (json, _) = await RunAsync(new[] { "--scan-open", "-j" }, ct);
        if (json == null) return Array.Empty<SmartctlScanDevice>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("devices", out var devices) || devices.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<SmartctlScanDevice>();
            }
            var list = new List<SmartctlScanDevice>();
            foreach (var d in devices.EnumerateArray())
            {
                var name = d.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var type = d.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(name)) list.Add(new SmartctlScanDevice(name, type));
            }
            return list;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "failed to parse smartctl --scan-open output");
            return Array.Empty<SmartctlScanDevice>();
        }
    }

    public async Task<SmartctlReport?> FetchAsync(SmartctlScanDevice device, CancellationToken ct)
    {
        if (_smartctlPath == null) return null;

        var args = string.IsNullOrEmpty(device.Type)
            ? new[] { "-j", "-a", device.Name }
            : new[] { "-j", "-a", "-d", device.Type, device.Name };

        var (json, exit) = await RunAsync(args, ct);
        if (json == null) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return ParseReport(doc.RootElement, device.Name, exit);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "failed to parse smartctl output for {Device}", device.Name);
            return null;
        }
    }

    private static SmartctlReport ParseReport(JsonElement root, string deviceName, int exitStatus)
    {
        var protocol = root.TryGetProperty("device", out var dev) && dev.TryGetProperty("protocol", out var p)
            ? p.GetString() ?? "" : "";
        var model = root.TryGetProperty("model_name", out var m) ? m.GetString() ?? "" : "";
        var serial = root.TryGetProperty("serial_number", out var s) ? s.GetString() ?? "" : "";
        var firmware = root.TryGetProperty("firmware_version", out var fw) ? fw.GetString() ?? "" : "";

        long? capacity = null;
        if (root.TryGetProperty("user_capacity", out var uc) && uc.TryGetProperty("bytes", out var ucb))
        {
            capacity = ucb.GetInt64();
        }
        else if (root.TryGetProperty("nvme_total_capacity", out var ntc))
        {
            capacity = ntc.GetInt64();
        }

        bool? healthPassed = null;
        if (root.TryGetProperty("smart_status", out var ss) && ss.TryGetProperty("passed", out var sp))
        {
            healthPassed = sp.GetBoolean();
        }

        int? temp = null;
        if (root.TryGetProperty("temperature", out var t) && t.TryGetProperty("current", out var tc))
        {
            temp = tc.GetInt32();
        }

        long? powerOnHours = null;
        if (root.TryGetProperty("power_on_time", out var pot) && pot.TryGetProperty("hours", out var poh))
        {
            powerOnHours = poh.GetInt64();
        }

        long? powerCycle = null;
        if (root.TryGetProperty("power_cycle_count", out var pcc))
        {
            powerCycle = pcc.GetInt64();
        }

        List<AtaAttribute>? ataAttrs = null;
        if (root.TryGetProperty("ata_smart_attributes", out var asa) &&
            asa.TryGetProperty("table", out var table) &&
            table.ValueKind == JsonValueKind.Array)
        {
            ataAttrs = new List<AtaAttribute>(table.GetArrayLength());
            foreach (var attr in table.EnumerateArray())
            {
                int id = attr.TryGetProperty("id", out var ai) ? ai.GetInt32() : 0;
                string name = attr.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
                int value = attr.TryGetProperty("value", out var av) ? av.GetInt32() : 0;
                int worst = attr.TryGetProperty("worst", out var aw) ? aw.GetInt32() : 0;
                int thresh = attr.TryGetProperty("thresh", out var at) ? at.GetInt32() : 0;
                long raw = 0;
                if (attr.TryGetProperty("raw", out var ar) && ar.TryGetProperty("value", out var arv))
                {
                    raw = arv.GetInt64();
                }
                ataAttrs.Add(new AtaAttribute(id, name, value, worst, thresh, raw));
            }
        }

        NvmeHealthLog? nvme = null;
        if (root.TryGetProperty("nvme_smart_health_information_log", out var nh))
        {
            nvme = new NvmeHealthLog(
                CriticalWarning: ReadIntOpt(nh, "critical_warning"),
                AvailableSpare: ReadIntOpt(nh, "available_spare"),
                AvailableSpareThreshold: ReadIntOpt(nh, "available_spare_threshold"),
                PercentageUsed: ReadIntOpt(nh, "percentage_used"),
                DataUnitsRead: ReadLongOpt(nh, "data_units_read"),
                DataUnitsWritten: ReadLongOpt(nh, "data_units_written"),
                HostReads: ReadLongOpt(nh, "host_reads"),
                HostWrites: ReadLongOpt(nh, "host_writes"),
                ControllerBusyTime: ReadLongOpt(nh, "controller_busy_time"),
                PowerCycles: ReadLongOpt(nh, "power_cycles"),
                PowerOnHours: ReadLongOpt(nh, "power_on_hours"),
                UnsafeShutdowns: ReadLongOpt(nh, "unsafe_shutdowns"),
                MediaErrors: ReadLongOpt(nh, "media_errors"),
                NumErrLogEntries: ReadLongOpt(nh, "num_err_log_entries"),
                WarningTempTime: ReadIntOpt(nh, "warning_temp_time"),
                CriticalCompTime: ReadIntOpt(nh, "critical_comp_time"));
        }

        return new SmartctlReport(
            deviceName, protocol, model, serial, firmware, capacity,
            healthPassed, temp, powerOnHours, powerCycle, exitStatus,
            ataAttrs, nvme);
    }

    private static int? ReadIntOpt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

    private static long? ReadLongOpt(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : null;

    private async Task<(string? Stdout, int ExitCode)> RunAsync(string[] args, CancellationToken ct)
    {
        if (_smartctlPath == null) return (null, -1);

        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _smartctlPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };
        foreach (var a in args) proc.StartInfo.ArgumentList.Add(a);

        try
        {
            proc.Start();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.InvocationTimeoutSeconds));

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            try
            {
                await proc.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("smartctl {Args} exceeded {Timeout}s timeout, killing",
                    string.Join(' ', args), _options.InvocationTimeoutSeconds);
                try { proc.Kill(entireProcessTree: true); } catch { }
                return (null, -1);
            }

            var stdout = await stdoutTask;
            // smartctl exit codes are bit-encoded; 0 and bits 6/7 (past errors / self-test errors) are
            // all "device responded fine, here's the data" — JSON is still valid.
            return (stdout, proc.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "smartctl invocation failed: {Args}", string.Join(' ', args));
            return (null, -1);
        }
    }

    private static string? ResolveSmartctlPath(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return configured;

        var exeDir = AppContext.BaseDirectory;
        var bundled = Path.Combine(exeDir, "smartctl", "smartctl.exe");
        if (File.Exists(bundled)) return bundled;
        // Defense in depth: if a bad MSI lands smartctl.exe directly in INSTALLFOLDER
        // instead of the smartctl\ subfolder, still find it.
        var sideBySide = Path.Combine(exeDir, "smartctl.exe");
        if (File.Exists(sideBySide)) return sideBySide;

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                var candidate = Path.Combine(dir.Trim(), "smartctl.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }
}
