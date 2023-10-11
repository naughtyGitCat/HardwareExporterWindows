// // 张锐志 2023-10-10
// ref https://learn.microsoft.com/en-us/archive/blogs/knom/ip-address-calculations-with-c-subnetmasks-networks
using System.Net;
namespace HardwareExporterWeb.Extensions;

public static class IPAddressExtensions
{
    public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
    {
        var ipAddressBytes = address.GetAddressBytes();
        var subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAddressBytes.Length != subnetMaskBytes.Length)
            throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

        var broadcastAddress = new byte[ipAddressBytes.Length];
        for (var i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAddressBytes[i] | (subnetMaskBytes[i] ^ 255));
        }
        return new IPAddress(broadcastAddress);
    }

    public static long GetNumber(this IPAddress address)
    {
        var bytes= address.GetAddressBytes();
        if (BitConverter.IsLittleEndian) bytes = bytes.Reverse().ToArray();
        // BitConverter.ToUInt32(bytes, 0)
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
    {
        var ipAddressBytes = address.GetAddressBytes();
        var subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAddressBytes.Length != subnetMaskBytes.Length)
            throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

        var broadcastAddress = new byte[ipAddressBytes.Length];
        for (var i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAddressBytes[i] & (subnetMaskBytes[i]));
        }
        return new IPAddress(broadcastAddress);
    }

    public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
    {
        var network1 = address.GetNetworkAddress(subnetMask);
        var network2 = address2.GetNetworkAddress(subnetMask);

        return network1.Equals(network2);
    }
}
