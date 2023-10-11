// // 张锐志 2023-10-10
using System;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using ArpLookup;
using HardwareExporterWeb.Extensions;
using NetTools;
namespace HardwareExporterWeb.Services;

public interface ILocalNetworkWatcher
{
    public Task<IEnumerable<Neighbor>> GetLocalNeighborsAsync();
    public Task<bool> IsNeighborPortOpenAsync(string ip, int port);
    public Task<bool> IsHardwareExporterAvailableAsync(string ip);
}

public record Neighbor
{
    public string Hostname { get; init; } = string.Empty;
    public string MachineAddress { get; init; } = string.Empty;
    public string IP { get; init; } = string.Empty;
}
public class LocalNetworkWatcher : ILocalNetworkWatcher
{

    private readonly ILogger<LocalNetworkWatcher> _logger;
    public LocalNetworkWatcher(ILogger<LocalNetworkWatcher> logger)
    {
        _logger = logger;
    }
    
    
    public async Task<IEnumerable<Neighbor>> GetLocalNeighborsAsync()
    {
        IEnumerable<Neighbor> neighbors = Array.Empty<Neighbor>();
        var ipHostEntry = await Dns.GetHostEntryAsync(Dns.GetHostName());
        foreach (var localAddress in ipHostEntry.AddressList)
        {
            _logger.LogInformation("now scan with interface {}", localAddress.ToString());
            var maskAddress = IPAddress.Parse("255.255.255.0");
            var broadcastAddress = localAddress.GetBroadcastAddress(maskAddress);
            var networkAddress = localAddress.GetNetworkAddress(maskAddress);
            var range = new IPAddressRange(networkAddress, broadcastAddress);
            foreach (var neighborAddress in range)
            {
                _logger.LogInformation("neighborAddress: {}",neighborAddress);
                var macAddress = await Arp.LookupAsync(neighborAddress);
                if (macAddress is null)
                {
                    _logger.LogWarning("neighborAddress {} arp lookup result is null", neighborAddress);
                    continue;
                }
                var neighborHostname = (await Dns.GetHostEntryAsync(neighborAddress)).HostName;
                _logger.LogInformation("neighbor: {}, mac: {}, hostname: {}",neighborAddress, macAddress, neighborHostname);
                neighbors = neighbors.Append(new Neighbor
                {
                    Hostname = neighborHostname,
                    IP = neighborAddress.ToString(),
                    MachineAddress = macAddress.ToString()
                });
            }   
        }
        return neighbors;
    }
    public async Task<bool> IsNeighborPortOpenAsync(string ip, int port)
    {
        throw new NotImplementedException();
    }
    public async Task<bool> IsHardwareExporterAvailableAsync(string ip)
    {
        throw new NotImplementedException();
    }
}
