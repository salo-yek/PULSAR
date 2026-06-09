// SPDX-License-Identifier: MIT
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using PULSAR.Core;
using PULSAR.UI;

namespace PULSAR.Networking;

/// <summary>
/// Manages proxy discovery, testing, rotation, and HttpClient creation.
/// Provides the core networking layer that all external HTTP requests route through.
/// </summary>
public static class ProxyManager
{
    /// <summary>
    /// Prepares the connection by optionally rotating the proxy if <see cref="GlobalState.RotateOnEveryRequest"/> is enabled.
    /// </summary>
    public static async Task PrepareConnection()
    {
        if (GlobalState.RotateOnEveryRequest)
            await FindAndSetGlobalProxy(true);
    }

    /// <summary>
    /// Performs a full deep scan across all proxy sources to find the fastest available node.
    /// Tests proxies in batches and selects the best one.
    /// </summary>
    public static async Task RunDeepScan()
    {
        const int BATCH_SIZE = 10;

        ModernUI.DrawLogo();
        ModernUI.Print("FORCE DEEP SCAN INITIATED", ModernUI.MsgType.Warning);
        ModernUI.Print("Collecting all available proxy nodes...", ModernUI.MsgType.Wait);

        var allProxies = new HashSet<string>();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        foreach (var source in Constants.ProxySources)
        {
            try
            {
                var data = await client.GetStringAsync(source);
                var matches = GlobalState.IpPortRegex.Matches(data);
                foreach (Match m in matches) allProxies.Add(m.Value);
                Console.Write($"\r   Discovered: {allProxies.Count} nodes...");
            }
            catch
            {
                // Source may be unreachable - skip and continue
            }
        }
        Console.WriteLine();

        if (allProxies.Count == 0)
        {
            ModernUI.Print("No proxies found.", ModernUI.MsgType.Error);
            return;
        }

        ModernUI.Print($"Testing {allProxies.Count} unique nodes with early exit (< 1s target)...", ModernUI.MsgType.Wait);

        var proxyList = allProxies.ToList();
        var results = new ConcurrentBag<(string ip, long ms)>();
        int tested = 0;
        int total = proxyList.Count;
        string? bestFallback = null;
        long bestFallbackLatency = long.MaxValue;

        using var cts = new CancellationTokenSource();
        var batches = proxyList
            .Select((p, i) => new { Proxy = p, Index = i })
            .GroupBy(x => x.Index / BATCH_SIZE)
            .Select(g => g.Select(x => x.Proxy).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            if (cts.Token.IsCancellationRequested) break;

            var batchResults = new ConcurrentBag<(string ip, long ms)>();
            var semaphore = new SemaphoreSlim(BATCH_SIZE);
            var batchTasks = new List<Task>();

            foreach (var proxy in batch)
            {
                await semaphore.WaitAsync(cts.Token);
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var (ok, latency) = await TestProxy(proxy);
                        if (ok)
                            batchResults.Add((proxy, latency));
                    }
                    catch
                    {
                        // Proxy unreachable - skip
                    }
                    finally
                    {
                        semaphore.Release();
                        Interlocked.Increment(ref tested);
                    }
                });
                batchTasks.Add(task);
            }

            await Task.WhenAll(batchTasks);
            Console.Write($"\r   Scanned: {tested}/{total} | Working: {batchResults.Count} nodes...   ");

            foreach (var (ip, ms) in batchResults)
            {
                results.Add((ip, ms));
                if (ms < bestFallbackLatency)
                {
                    bestFallbackLatency = ms;
                    bestFallback = ip;
                }
                if (ms < 1000)
                {
                    cts.Cancel();
                    break;
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine();

        if (bestFallback != null)
        {
            GlobalState.GlobalProxyAddress = bestFallback;
            GlobalState.GlobalProxy = new WebProxy(bestFallback);
            GlobalState.LastProxyChangeTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

            ModernUI.Print("Deep Scan Complete!", ModernUI.MsgType.Success);
            ModernUI.Print($"Selected Node: {bestFallback} (Latency: {bestFallbackLatency}ms)", ModernUI.MsgType.Info);
        }
        else
        {
            ModernUI.Print("Deep Scan failed to find any working nodes.", ModernUI.MsgType.Error);
        }
    }

    /// <summary>
    /// Finds and sets a global proxy by scanning available proxy sources.
    /// Stops early if a sub-1000ms proxy is found.
    /// </summary>
    /// <param name="force">If true, always scan; if false, only scan if no proxy is set.</param>
    public static async Task FindAndSetGlobalProxy(bool force)
    {
        if (!force)
            ModernUI.Print("Searching for secure nodes...", ModernUI.MsgType.Wait);

        using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        foreach (var s in Constants.ProxySources)
        {
            try
            {
                var d = await c.GetStringAsync(s);
                var matches = GlobalState.IpPortRegex.Matches(d);
                foreach (Match m in matches)
                {
                    string p = m.Value;
                    var (ok, latency) = await TestProxy(p);
                    if (GlobalState.GlobalProxyAddress == "None" && ok)
                    {
                        GlobalState.GlobalProxyAddress = p;
                        GlobalState.GlobalProxy = new WebProxy(p);
                        GlobalState.LastProxyChangeTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                        if (!force)
                            ModernUI.Print($"Tunnel established: {p} ({latency}ms)", ModernUI.MsgType.Success);
                        if (latency < 1000) return;
                    }
                }
            }
            catch
            {
                // Source fetch failed - skip to next
            }
        }

        if (GlobalState.GlobalProxyAddress == "None" && !force)
            ModernUI.Print("Failed to connect via proxy. Using Direct.", ModernUI.MsgType.Error);
    }

    /// <summary>
    /// Tests a single proxy by attempting to reach http://clients3.google.com/generate_204.
    /// </summary>
    /// <param name="proxyAddress">The proxy address in IP:Port format.</param>
    /// <returns>A tuple of (success, latencyMs).</returns>
    internal static async Task<(bool success, long latencyMs)> TestProxy(string proxyAddress)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = new WebProxy(proxyAddress),
                UseProxy = true,
                ConnectTimeout = TimeSpan.FromSeconds(3),
                PooledConnectionLifetime = TimeSpan.Zero
            };
            using var c = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            var r = await c.GetAsync("http://clients3.google.com/generate_204");
            sw.Stop();
            return (r.IsSuccessStatusCode, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            return (false, long.MaxValue);
        }
    }

    /// <summary>
    /// Creates an HttpClient configured to route through the currently active global proxy (if any).
    /// </summary>
    /// <returns>A configured HttpClient instance.</returns>
    public static HttpClient GetClient()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.Zero
        };
        if (GlobalState.GlobalProxy != null)
        {
            handler.Proxy = GlobalState.GlobalProxy;
            handler.UseProxy = true;
        }
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }
}