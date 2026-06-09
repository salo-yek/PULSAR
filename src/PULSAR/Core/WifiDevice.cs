// SPDX-License-Identifier: MIT
namespace PULSAR.Core;

/// <summary>
/// Represents a WiFi device (access point or client station) discovered during scanning.
/// </summary>
public class WifiDevice
{
    /// <summary>MAC address of the device.</summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>SSID or descriptive name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Device type ("Access Point" or "Client Device").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>IP address (if known).</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>WiFi channel.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Observed packet count (activity indicator).</summary>
    public int PacketCount { get; set; }

    /// <summary>BSSID of the access point this client is connected to.</summary>
    public string ConnectedToBssid { get; set; } = string.Empty;
}