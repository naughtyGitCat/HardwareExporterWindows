// // 张锐志 2023-10-09
using HardwareExporterServer.Model;
using HardwareExporterServer.Services;
// using Microsoft.AspNetCore.Mvc;
// namespace HardwareExporterServer.Controllers;
//
//
// [Route("/ServiceDiscovery")]
// public class ServiceDiscoveryController : ControllerBase
// {
//     private readonly HostInfoManager _hostInfoManager;
//     private readonly ILogger<ServiceDiscoveryController> _logger;
//     public ServiceDiscoveryController(HostInfoManager hostInfoManager, ILogger<ServiceDiscoveryController> logger)
//     {
//         _logger = logger;
//         _hostInfoManager = hostInfoManager;
//     }
//
//     public IEnumerable<ServiceDiscoveryDTO> GetExporters()
//     {
//         IEnumerable<ServiceDiscoveryDTO> ret;
//         try
//         {
//             var rawEntities= _hostInfoManager.GetHostInfoEntities();
//             ret = rawEntities.Select(e => new ServiceDiscoveryDTO
//             {
//                 Targets = new[]
//                 {
//                     $"{e.HostIP}:{e.ExporterPort}"
//                 },
//                 Labels = new Dictionary<string, string>
//                 {
//                     {
//                         "ip", e.HostIP
//                     },
//                     {
//                         "hostname", e.HostName
//                     }
//                 }
//             });
//         }
//         catch (Exception e)
//         {
//             _logger.LogWarning("{e}", e);
//             ret = Array.Empty<ServiceDiscoveryDTO>();
//         }
//         return ret;
//     }
// }
