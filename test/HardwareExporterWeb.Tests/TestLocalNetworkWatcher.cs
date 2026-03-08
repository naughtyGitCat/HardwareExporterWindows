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