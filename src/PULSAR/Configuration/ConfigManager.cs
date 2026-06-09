// SPDX-License-Identifier: MIT
using System.Text.Json;
using PULSAR.Core;

namespace PULSAR.Configuration;

/// <summary>
/// Manages persistent proxy cache storage using JSON serialization.
/// Caches the best known proxy between sessions to avoid re-scanning on every launch.
/// </summary>
public static class ConfigManager
{
    private const string ProxyCacheFile = "pulsar_proxy_cache.dat";

    /// <summary>
    /// JSON-serializable configuration object for proxy cache storage.
    /// </summary>
    public class ProxyCacheConfig
    {
        /// <summary>Proxy address (IP:Port).</summary>
        public string ProxyAddress { get; set; } = string.Empty;
        /// <summary>Proxy type string (e.g., "HTTP", "SOCKS5").</summary>
        public string ProxyType { get; set; } = string.Empty;
        /// <summary>Measured latency in milliseconds.</summary>
        public long LatencyMs { get; set; }
        /// <summary>Unix timestamp when cached.</summary>
        public long Timestamp { get; set; }
        /// <summary>Unix timestamp when cache expires.</summary>
        public long Expiry { get; set; }
    }

    /// <summary>
    /// Saves a proxy cache entry to disk with a 7-day expiry.
    /// </summary>
    /// <param name="entry">The proxy entry to cache.</param>
    public static void SaveProxyCache(ProxyCacheEntry entry)
    {
        try
        {
            long expiry = DateTimeOffset.Now.AddDays(7).ToUnixTimeSeconds();
            var cfg = new ProxyCacheConfig
            {
                ProxyAddress = entry.Address,
                ProxyType = entry.Type.ToString(),
                LatencyMs = entry.LatencyMs,
                Timestamp = entry.Timestamp,
                Expiry = expiry
            };
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProxyCacheFile);
            string json = JsonSerializer.Serialize(cfg);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Non-critical proxy cache write failure
        }
    }

    /// <summary>
    /// Loads a proxy cache entry from disk if it exists and has not expired.
    /// </summary>
    /// <returns>The cached entry, or null if not available or expired.</returns>
    public static ProxyCacheEntry? LoadProxyCache()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProxyCacheFile);
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<ProxyCacheConfig>(json);
            if (cfg is null) return null;
            if (cfg.Expiry > 0 && DateTimeOffset.Now.ToUnixTimeSeconds() > cfg.Expiry)
            {
                File.Delete(path);
                return null;
            }
            if (Enum.TryParse<ProxyType>(cfg.ProxyType, out var pType))
            {
                return new ProxyCacheEntry
                {
                    Address = cfg.ProxyAddress,
                    Type = pType,
                    LatencyMs = cfg.LatencyMs,
                    Timestamp = cfg.Timestamp
                };
            }
        }
        catch
        {
            // Corrupted or missing cache file - return null
        }
        return null;
    }

    /// <summary>
    /// Deletes the proxy cache file from disk.
    /// </summary>
    public static void ClearProxyCache()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProxyCacheFile);
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // File already deleted or inaccessible
        }
    }
}