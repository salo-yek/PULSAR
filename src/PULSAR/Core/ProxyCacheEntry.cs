// SPDX-License-Identifier: MIT
namespace PULSAR.Core;

/// <summary>
/// Represents a cached proxy entry with addressing, type, and latency information.
/// </summary>
public class ProxyCacheEntry
{
    /// <summary>Proxy address in IP:Port format.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Proxy protocol type.</summary>
    public ProxyType Type { get; set; }

    /// <summary>Measured latency in milliseconds.</summary>
    public long LatencyMs { get; set; }

    /// <summary>Unix timestamp when this entry was last updated.</summary>
    public long Timestamp { get; set; }
}