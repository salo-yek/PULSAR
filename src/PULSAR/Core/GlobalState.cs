// SPDX-License-Identifier: MIT
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

namespace PULSAR.Core;

/// <summary>
/// Global application state. Holds the active proxy configuration, blacklist,
/// cached proxy data, and runtime flags used across all modules.
/// </summary>
public static class GlobalState
{
    // --- Proxy ---
    /// <summary>The currently active global proxy object (null = direct connection).</summary>
    public static IWebProxy? GlobalProxy { get; set; } = null;

    /// <summary>Human-readable address of the active proxy ("None" for direct).</summary>
    public static string GlobalProxyAddress { get; set; } = "None";

    /// <summary>Minutes between automatic proxy rotations (0 = disabled).</summary>
    public static int ProxyRotationMinutes { get; set; } = 0;

    /// <summary>Unix timestamp of the last proxy change.</summary>
    public static long LastProxyChangeTimestamp { get; set; } = 0;

    /// <summary>If true, rotates proxy on every HTTP request.</summary>
    public static bool RotateOnEveryRequest { get; set; } = true;

    /// <summary>Current proxy protocol type in use.</summary>
    public static ProxyType CurrentProxyType { get; set; } = ProxyType.HTTP;

    // --- Deep Scan ---
    /// <summary>If true, uses previously cached proxy scan results.</summary>
    public static bool UseDeepScanCache { get; set; } = false;

    /// <summary>If true, runs a deep proxy scan automatically on startup.</summary>
    public static bool DeepScanOnStartup { get; set; } = false;

    /// <summary>If true, skips the startup proxy prompt and uses direct connection.</summary>
    public static bool SkipDeepScanPrompt { get; set; } = false;

    /// <summary>Cached best proxy from last deep scan session.</summary>
    public static ProxyCacheEntry? CachedBestProxy { get; set; } = null;

    // --- Proxy Lists ---
    /// <summary>List of discovered proxy addresses (IP:Port).</summary>
    public static List<string> CachedProxies { get; set; } = new();

    /// <summary>Set of IPs to blacklist (never target).</summary>
    public static HashSet<string> TargetBlacklist { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Set of known proxy IPs to avoid duplicate testing.</summary>
    public static HashSet<string> KnownProxyIPs { get; set; } = new();

    // --- Regex ---
    /// <summary>Compiled regex for matching IP:Port patterns.</summary>
    public static Regex IpPortRegex { get; } = new(@"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}:[0-9]{1,5}\b", RegexOptions.Compiled);
}