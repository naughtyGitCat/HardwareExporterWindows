using System.Collections.Concurrent;
using HardwareExporterWindows.Configuration;
using Microsoft.Extensions.Options;

namespace HardwareExporterWindows.Services;

public class SmartMonitorService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<SmartMonitorService> _logger;
    private readonly SmartMonitorOptions _options;
    private readonly SmartctlInvoker _invoker;
    private readonly ConcurrentDictionary<string, SmartctlReport> _snapshots = new();
    private readonly CancellationTokenSource _stopCts = new();
    private Task? _loopTask;

    public SmartMonitorService(
        ILogger<SmartMonitorService> logger,
        IOptions<SmartMonitorOptions> options,
        SmartctlInvoker invoker)
    {
        _logger = logger;
        _options = options.Value;
        _invoker = invoker;
    }

    public IReadOnlyCollection<SmartctlReport> GetSnapshots() => _snapshots.Values.ToArray();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enable)
        {
            _logger.LogInformation("SMART monitor disabled by config");
            return Task.CompletedTask;
        }
        if (!_invoker.IsAvailable)
        {
            _logger.LogWarning("SMART monitor enabled but smartctl is unavailable; nothing will be collected");
            return Task.CompletedTask;
        }
        _loopTask = Task.Run(() => RefreshLoopAsync(_stopCts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopCts.Cancel();
        if (_loopTask != null)
        {
            try { await _loopTask.WaitAsync(cancellationToken); }
            catch (OperationCanceledException) { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _stopCts.Dispose();
    }

    private async Task RefreshLoopAsync(CancellationToken ct)
    {
        // Best-effort initial population so the first /metrics scrape after startup has data.
        try { await RefreshOnceAsync(ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "SMART initial refresh failed"); }

        var interval = TimeSpan.FromSeconds(Math.Max(15, _options.RefreshIntervalSeconds));
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try { await RefreshOnceAsync(ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { _logger.LogWarning(ex, "SMART refresh cycle failed"); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RefreshOnceAsync(CancellationToken ct)
    {
        var devices = await _invoker.ScanAsync(ct);
        if (devices.Count == 0)
        {
            _logger.LogDebug("smartctl --scan-open returned no devices");
            return;
        }

        var filtered = devices
            .Where(d => !IsExcluded(d.Name))
            .ToList();

        // Fetch in parallel; smartctl is independent per device.
        var tasks = filtered.Select(async d =>
        {
            var report = await _invoker.FetchAsync(d, ct);
            return (Device: d, Report: report);
        }).ToList();

        var results = await Task.WhenAll(tasks);
        var seen = new HashSet<string>();
        foreach (var (device, report) in results)
        {
            if (report == null) continue;
            seen.Add(device.Name);
            _snapshots[device.Name] = report;
        }

        // Drop snapshots for devices that vanished (drive removed, etc).
        foreach (var key in _snapshots.Keys.ToList())
        {
            if (!seen.Contains(key)) _snapshots.TryRemove(key, out _);
        }

        _logger.LogDebug("SMART refresh: {Total} discovered, {Captured} captured",
            filtered.Count, _snapshots.Count);
    }

    private bool IsExcluded(string deviceName)
    {
        foreach (var pattern in _options.DeviceExcludePatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern)) continue;
            if (GlobMatch(deviceName, pattern)) return true;
        }
        return false;
    }

    private static bool GlobMatch(string input, string pattern)
    {
        // Tiny glob: supports * (any chars) and ? (single char). Sufficient for /dev/sd? style.
        int i = 0, p = 0, starI = -1, starP = -1;
        while (i < input.Length)
        {
            if (p < pattern.Length && (pattern[p] == '?' || pattern[p] == input[i]))
            {
                i++; p++;
            }
            else if (p < pattern.Length && pattern[p] == '*')
            {
                starP = p++;
                starI = i;
            }
            else if (starP != -1)
            {
                p = starP + 1;
                i = ++starI;
            }
            else return false;
        }
        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }
}
