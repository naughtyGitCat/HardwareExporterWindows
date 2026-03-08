// // 张锐志 2023-10-10
using System;
using System.Net;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using ArpLookup;
using HardwareExporterWeb.Extensions;
using HardwareExporterWeb.Configuration;
using Microsoft.Extensions.Options;
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
    private readonly NetworkScanOptions _options;

    public LocalNetworkWatcher(
        ILogger<LocalNetworkWatcher> logger,
        IOptions<NetworkScanOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    
    public async Task<IEnumerable<Neighbor>> GetLocalNeighborsAsync()
    {
        IEnumerable<Neighbor> neighbors = Array.Empty<Neighbor>();
        var ipHostEntry = await Dns.GetHostEntryAsync(Dns.GetHostName());
        
        foreach (var localAddress in ipHostEntry.AddressList)
        {
            // Skip loopback and IPv6 addresses
            if (IPAddress.IsLoopback(localAddress) || localAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                continue;
            }

            // Apply subnet filter if configured
            if (!string.IsNullOrEmpty(_options.SubnetFilter))
            {
                if (!localAddress.ToString().Contains(_options.SubnetFilter))
                {
                    _logger.LogDebug("Skipping interface {} (doesn't match filter {})", 
                        localAddress, _options.SubnetFilter);
                    continue;
                }
            }

            _logger.LogInformation("Scanning subnet for interface {}", localAddress.ToString());
            
            var maskAddress = IPAddress.Parse(_options.SubnetMask);
            var broadcastAddress = localAddress.GetBroadcastAddress(maskAddress);
            var networkAddress = localAddress.GetNetworkAddress(maskAddress);
            var range = new IPAddressRange(
                new IPAddress(networkAddress.Address + 1), 
                new IPAddress(broadcastAddress.Address - 1));
            
            foreach (var neighborAddress in range)
            {
                _logger.LogDebug("Checking neighbor: {}", neighborAddress);
                var macAddress = await Arp.LookupAsync(neighborAddress);
                if (macAddress is null)
                {
                    _logger.LogDebug("Neighbor {} ARP lookup returned null", neighborAddress);
                    continue;
                }
                
                try
                {
                    var neighborHostname = (await Dns.GetHostEntryAsync(neighborAddress)).HostName;
                    _logger.LogInformation("Found neighbor: {}, MAC: {}, Hostname: {}", 
                        neighborAddress, macAddress, neighborHostname);
                    
                    neighbors = neighbors.Append(new Neighbor
                    {
                        Hostname = neighborHostname,
                        IP = neighborAddress.ToString(),
                        MachineAddress = macAddress.ToString()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve hostname for {}", neighborAddress);
                    // Still add the neighbor without hostname
                    neighbors = neighbors.Append(new Neighbor
                    {
                        Hostname = neighborAddress.ToString(),
                        IP = neighborAddress.ToString(),
                        MachineAddress = macAddress.ToString()
                    });
                }
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
