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
        // Skip this test in CI environments (GitHub Actions, etc.)
        // Network scanning tests are slow and may not work properly in CI
        var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
        
        if (isCI)
        {
            _testOutputHelper.WriteLine("Skipping network scan test in CI environment");
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
            
        var neighbors = await localNetworkWatcher.GetLocalNeighborsAsync();
        foreach (var neighbor in neighbors)
        {
            _testOutputHelper.WriteLine($"hostname: {neighbor.Hostname} ip: {neighbor.IP} mac: {neighbor.MachineAddress}");
        }
    }
}