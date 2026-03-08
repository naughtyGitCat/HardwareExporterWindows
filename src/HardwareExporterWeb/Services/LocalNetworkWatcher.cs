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
            
            // Create range from network+1 to broadcast-1 (skip network and broadcast addresses)
            // Use IPAddressRange constructor that takes begin and end as IPAddress
            var beginBytes = networkAddress.GetAddressBytes();
            var endBytes = broadcastAddress.GetAddressBytes();
            
            // Increment begin address (network + 1)
            for (int i = beginBytes.Length - 1; i >= 0; i--)
            {
                if (++beginBytes[i] != 0) break;
            }
            
            // Decrement end address (broadcast - 1)
            for (int i = endBytes.Length - 1; i >= 0; i--)
            {
                if (endBytes[i]-- != 0) break;
            }
            
            var beginAddress = new IPAddress(beginBytes);
            var endAddress = new IPAddress(endBytes);
            
            // Check if range is valid (begin <= end)
            if (CompareIPAddresses(beginAddress, endAddress) > 0)
            {
                _logger.LogWarning("Subnet too small to scan: {}/{}", localAddress, _options.SubnetMask);
                continue;
            }
            
            var range = new IPAddressRange(beginAddress, endAddress);
            
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
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == connectTask && !connectTask.IsFaulted)
            {
                _logger.LogDebug("Port {Port} is open on {IP}", port, ip);
                return true;
            }
            
            _logger.LogDebug("Port {Port} is closed or unreachable on {IP}", port, ip);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check port {Port} on {IP}", port, ip);
            return false;
        }
    }
    
    public async Task<bool> IsHardwareExporterAvailableAsync(string ip)
    {
        const int hardwareExporterPort = 9888;
        
        try
        {
            // First check if port is open
            if (!await IsNeighborPortOpenAsync(ip, hardwareExporterPort))
            {
                _logger.LogDebug("HardwareExporter port {Port} is not open on {IP}", hardwareExporterPort, ip);
                return false;
            }
            
            // Try to fetch metrics endpoint to verify it's actually HardwareExporter
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
            
            var response = await httpClient.GetAsync($"http://{ip}:{hardwareExporterPort}/metrics");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // Check if response contains hardware metrics
                if (content.Contains("hardware_") || content.Contains("# HELP hardware_"))
                {
                    _logger.LogInformation("HardwareExporter is available on {IP}:{Port}", ip, hardwareExporterPort);
                    return true;
                }
            }
            
            _logger.LogDebug("Port {Port} is open on {IP} but doesn't appear to be HardwareExporter", hardwareExporterPort, ip);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to verify HardwareExporter on {IP}", ip);
            return false;
        }
    }
    
    private static int CompareIPAddresses(IPAddress a, IPAddress b)
    {
        var bytesA = a.GetAddressBytes();
        var bytesB = b.GetAddressBytes();
        
        if (bytesA.Length != bytesB.Length)
        {
            return bytesA.Length.CompareTo(bytesB.Length);
        }
        
        for (int i = 0; i < bytesA.Length; i++)
        {
            if (bytesA[i] != bytesB[i])
            {
                return bytesA[i].CompareTo(bytesB[i]);
            }
        }
        
        return 0;
    }
}
