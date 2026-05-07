using System.IO;
using HardwareExporterWeb.Models;
using HardwareExporterWeb.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace HardwareExporterWeb.Tests;

public class HostInfoManagerSqliTest : IDisposable
{
    private readonly ITestOutputHelper _out;
    private readonly string _dir;
    private readonly string _origDir;

    public HostInfoManagerSqliTest(ITestOutputHelper output)
    {
        _out = output;
        _origDir = Directory.GetCurrentDirectory();
        _dir = Path.Combine(Path.GetTempPath(), "hwe-sqli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Directory.SetCurrentDirectory(_dir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void InsertUpdateDelete_HandlesMaliciousInputSafely()
    {
        var mgr = new HostInfoManager(NullLogger<HostInfoManager>.Instance);

        mgr.InsertHostInfo(new HostInfoEntity
        {
            ID = 1, HostName = "alpha", HostIP = "10.0.0.1",
            HardwareExporterPort = 9888, WindowsExporterPort = 9182
        });
        mgr.InsertHostInfo(new HostInfoEntity
        {
            ID = 2, HostName = "beta'; DROP TABLE HostInfo; --", HostIP = "10.0.0.2",
            HardwareExporterPort = 9888, WindowsExporterPort = 9182
        });

        var rows = mgr.GetHostInfoEntities().ToList();
        Assert.Equal(2, rows.Count);
        _out.WriteLine($"after insert: {rows.Count} rows");

        // Update: should work with simple input
        mgr.UpdateHostInfo("10.0.0.1", "alpha-renamed", windowsExporterPort: 9999, hardwareExporterPort: null);
        var alpha = mgr.GetHostInfoEntity("10.0.0.1");
        Assert.Equal("alpha-renamed", alpha.HostName);
        Assert.Equal(9999, alpha.WindowsExporterPort);
        Assert.Equal(9888, alpha.HardwareExporterPort);
        _out.WriteLine($"alpha after update: name={alpha.HostName} winPort={alpha.WindowsExporterPort} hwPort={alpha.HardwareExporterPort}");

        // Update with malicious-looking hostname value — must store literally, not execute
        mgr.UpdateHostInfo("10.0.0.2", "x'; DELETE FROM HostInfo; --", null, null);
        var beta = mgr.GetHostInfoEntity("10.0.0.2");
        Assert.Equal("x'; DELETE FROM HostInfo; --", beta.HostName);
        Assert.Equal(2, mgr.GetHostInfoEntities().Count());

        // Update with malicious WHERE clause IP — must match nothing, not be parsed as SQL
        mgr.UpdateHostInfo("10.0.0.1' OR '1'='1", "should-not-apply", null, null);
        Assert.Equal("alpha-renamed", mgr.GetHostInfoEntity("10.0.0.1").HostName);
        Assert.Equal(2, mgr.GetHostInfoEntities().Count());
        _out.WriteLine("update injection blocked");

        // Delete with malicious IP — must not nuke the table
        mgr.DeleteHostInfo("10.0.0.99'; DELETE FROM HostInfo; --");
        Assert.Equal(2, mgr.GetHostInfoEntities().Count());
        _out.WriteLine("delete injection blocked");

        // Normal delete works
        mgr.DeleteHostInfo("10.0.0.1");
        var remain = mgr.GetHostInfoEntities().ToList();
        Assert.Single(remain);
        Assert.Equal("10.0.0.2", remain[0].HostIP);
        _out.WriteLine($"after delete: {remain.Count} row, ip={remain[0].HostIP}");
    }
}
