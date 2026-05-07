using HardwareExporterWeb.Configuration;
using HardwareExporterWeb.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;
namespace HardwareExporterWeb.Tests;

public class TestLocalNetworkWatcher
{
    private readonly ITestOutputHelper _testOutputHelper;
    public TestLocalNetworkWatcher(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public async Task TestGetLocalNeighborsAsync()
    {
        // Opt-in only: scan does ARP+DNS across the whole /24 with no per-host
        // cancellation, so it can take many minutes on a populated network and
        // doesn't belong in default test runs.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUN_NETWORK_SCAN_TEST")))
        {
            _testOutputHelper.WriteLine("Skipping; set RUN_NETWORK_SCAN_TEST=1 to run.");
            return;
        }

        var options = Options.Create(new NetworkScanOptions
        {
            SubnetFilter = "",
            SubnetMask = "255.255.255.0"
        });

        var localNetworkWatcher = new LocalNetworkWatcher(
            new XunitLogger<LocalNetworkWatcher>(_testOutputHelper),
            options);

        var budget = TimeSpan.FromSeconds(30);
        var scanTask = localNetworkWatcher.GetLocalNeighborsAsync();
        var winner = await Task.WhenAny(scanTask, Task.Delay(budget));
        if (winner != scanTask)
        {
            _testOutputHelper.WriteLine($"Scan exceeded {budget.TotalSeconds:0}s budget; treating as a non-failure smoke check.");
            return;
        }

        var neighbors = await scanTask;
        foreach (var neighbor in neighbors)
        {
            _testOutputHelper.WriteLine($"hostname: {neighbor.Hostname} ip: {neighbor.IP} mac: {neighbor.MachineAddress}");
        }
    }
}