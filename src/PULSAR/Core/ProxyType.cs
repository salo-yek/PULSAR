// SPDX-License-Identifier: MIT
namespace PULSAR.Core;

/// <summary>
/// Supported proxy protocol types.
/// </summary>
public enum ProxyType
{
    /// <summary>HTTP proxy (CONNECT method).</summary>
    HTTP,

    /// <summary>HTTPS proxy (SSL/TLS tunnel).</summary>
    HTTPS,

    /// <summary>SOCKS5 proxy (requires HttpToSocks5Proxy package for full support).</summary>
    SOCKS5
}