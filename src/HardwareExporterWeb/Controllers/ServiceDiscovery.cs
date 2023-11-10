// // 张锐志 2023-10-09
using HardwareExporterWeb.Models;
using HardwareExporterWeb.Services;
using Microsoft.AspNetCore.Mvc;
namespace HardwareExporterWeb.Controllers;


[Route("/api/[controller]")]
public class ServiceDiscoveryController : ControllerBase
{
    private readonly HostInfoManager _hostInfoManager;
    private readonly ILogger<ServiceDiscoveryController> _logger;
    public ServiceDiscoveryController(HostInfoManager hostInfoManager, ILogger<ServiceDiscoveryController> logger)
    {
        _logger = logger;
        _hostInfoManager = hostInfoManager;
    }

    [HttpGet("WindowsExporter")]
    public IEnumerable<ServiceDiscoveryDTO> GetWindowsExporters()
    {
        IEnumerable<ServiceDiscoveryDTO> ret;
        try
        {
            var rawEntities= _hostInfoManager.GetHostInfoEntities();
            ret = rawEntities.Select(e => new ServiceDiscoveryDTO
            {
                Targets = new[]
                {
                    $"{e.HostIP}:{e.WindowsExporterPort}"
                },
                Labels = new Dictionary<string, string>
                {
                    {
                        "ip", e.HostIP
                    },
                    {
                        "hostname", e.HostName
                    }
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogWarning("{e}", e);
            ret = Array.Empty<ServiceDiscoveryDTO>();
        }
        return ret;
    }
    
    [HttpGet("HardwareExporter")]
    public IEnumerable<ServiceDiscoveryDTO> GetExporters()
    {
        IEnumerable<ServiceDiscoveryDTO> ret;
        try
        {
            var rawEntities= _hostInfoManager.GetHostInfoEntities();
            ret = rawEntities.Select(e => new ServiceDiscoveryDTO
            {
                Targets = new[]
                {
                    $"{e.HostIP}:{e.HardwareExporterPort}"
                },
                Labels = new Dictionary<string, string>
                {
                    {
                        "ip", e.HostIP
                    },
                    {
                        "hostname", e.HostName
                    }
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogWarning("{e}", e);
            ret = Array.Empty<ServiceDiscoveryDTO>();
        }
        return ret;
    }
}
