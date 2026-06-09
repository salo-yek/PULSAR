// SPDX-License-Identifier: MIT
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using PULSAR.Core;

namespace PULSAR.Networking;

/// <summary>
/// Network utility methods for IP address resolution and subnet scanning.
/// Uses ARP on Windows and ICMP ping on Linux for device discovery.
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// Gets the local IP address of the primary network interface by opening
    /// a UDP socket to a known external address.
    /// </summary>
    /// <returns>The local IPv4 address, or "127.0.0.1" if detection fails.</returns>
    public static string GetLocalIPAddress()
    {
        try
        {
            using var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    /// <summary>
    /// Scans the /24 subnet of the given IP address for active hosts.
    /// Uses ARP requests on Windows and ICMP echo (ping) on Linux.
    /// </summary>
    /// <param name="ip">An IP address within the target subnet.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A list of discovered devices as "IP (Status)" strings.</returns>
    public static async Task<List<string>> ScanSubnet(string ip, CancellationToken token)
    {
        var results = new List<string>();
        string baseIp = ip.Substring(0, ip.LastIndexOf('.'));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var tasks = Enumerable.Range(1, 254).Select(i => Task.Run(() =>
            {
                try
                {
                    string target = $"{baseIp}.{i}";
                    byte[] mac = new byte[6];
                    uint len = (uint)mac.Length;
                    int intAddr = BitConverter.ToInt32(IPAddress.Parse(target).GetAddressBytes(), 0);
                    if (Win32Native.SendARP((uint)intAddr, 0, mac, ref len) == 0)
                    {
                        lock (results) results.Add($"{target} (Active)");
                    }
                }
                catch
                {
                    // Host unreachable via ARP
                }
            }));
            await Task.WhenAll(tasks);
        }
        else
        {
            try
            {
                var tasks = Enumerable.Range(1, 254).Select(i => Task.Run(async () =>
                {
                    try
                    {
                        using var p = new Ping();
                        var r = await p.SendPingAsync($"{baseIp}.{i}", 200);
                        if (r.Status == IPStatus.Success)
                            lock (results) results.Add($"{baseIp}.{i} (Active)");
                    }
                    catch
                    {
                        // Host unreachable via ICMP
                    }
                }));
                await Task.WhenAll(tasks);
            }
            catch
            {
                // Ping subsystem unavailable
            }
        }
        return results;
    }
}