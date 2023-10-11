using HardwareExporterWeb.Services;
using Microsoft.Extensions.Logging.Abstractions;
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
        var localNetworkWatcher = new LocalNetworkWatcher(new NullLogger<LocalNetworkWatcher>());
        var neighbors = await localNetworkWatcher.GetLocalNeighborsAsync();
        foreach (var neighbor in neighbors)
        {
            _testOutputHelper.WriteLine($"hostname: {neighbor.Hostname} ip: {neighbor.IP} mac: {neighbor.MachineAddress}");
        }
    }
}