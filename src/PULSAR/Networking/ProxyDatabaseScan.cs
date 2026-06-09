// SPDX-License-Identifier: MIT
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using PULSAR.Configuration;
using PULSAR.Core;
using PULSAR.UI;

namespace PULSAR.Networking;

/// <summary>
/// Provides a comprehensive proxy database scanning and selection system.
/// Allows users to scan from multiple public proxy sources, test nodes, and select the fastest working proxy.
/// </summary>
public static class ProxyDatabaseScan
{
    /// <summary>
    /// Shows the proxy database scan menu.
    /// </summary>
    public static async Task RunProxyScanMenu()
    {
        while (true)
        {
            ModernUI.DrawLogo();
            ModernUI.DrawBox("PROXY DATABASE SCAN", () =>
            {
                Console.WriteLine("   Scan entire proxy database for specific types");
                Console.WriteLine("   and select fastest working node automatically.");
                Console.WriteLine();
                Console.WriteLine($"   Current Mode: {GlobalState.CurrentProxyType}");
                Console.WriteLine($"   Active Proxy: {GlobalState.GlobalProxyAddress}");
                Console.WriteLine($"   Cache Status: {(GlobalState.CachedBestProxy != null ? "ACTIVE" : "EMPTY")}");
            });
            Console.WriteLine();
            ModernUI.DrawMenuOption("1", "Deep Scan - HTTP Proxies");
            ModernUI.DrawMenuOption("2", "Deep Scan - HTTPS Proxies");
            ModernUI.DrawMenuOption("3", "Deep Scan - SOCKS5 Proxies");
            ModernUI.DrawMenuOption("4", "Quick Test Current Proxy");
            ModernUI.DrawMenuOption("5", "Clear Proxy Cache");
            ModernUI.DrawMenuOption("6", "Configure Startup Behavior");
            ModernUI.DrawMenuOption("X", "Back to Main Menu");
            Console.WriteLine();
            string choice = ModernUI.Prompt("Select Option");
            switch (choice)
            {
                case "1": await RunDeepScanWithTypeSelection(ProxyType.HTTP); break;
                case "2": await RunDeepScanWithTypeSelection(ProxyType.HTTPS); break;
                case "3": await RunDeepScanWithTypeSelection(ProxyType.SOCKS5); break;
                case "4": await TestCurrentProxy(); break;
                case "5":
                    ConfigManager.ClearProxyCache();
                    GlobalState.CachedBestProxy = null;
                    ModernUI.Print("Cache cleared.", ModernUI.MsgType.Success);
                    ModernUI.Pause();
                    break;
                case "6": await ConfigureStartupBehavior(); break;
                case "x": return;
            }
        }
    }

    /// <summary>
    /// Initiates a deep scan with proxy type selection.
    /// </summary>
    /// <param name="forcedType">Optional forced proxy type. If null, user is prompted.</param>
    public static async Task RunDeepScanWithTypeSelection(ProxyType? forcedType = null)
    {
        ProxyType selectedType = forcedType ?? ProxyType.HTTP;

        if (forcedType == null)
        {
            ModernUI.DrawLogo();
            ModernUI.Print("Select Proxy Type for Deep Scan:", ModernUI.MsgType.Info);
            ModernUI.DrawMenuOption("1", "HTTP");
            ModernUI.DrawMenuOption("2", "HTTPS");
            ModernUI.DrawMenuOption("3", "SOCKS5");
            Console.WriteLine();
            string typeChoice = ModernUI.Prompt("Type", "1");
            selectedType = typeChoice switch
            {
                "2" => ProxyType.HTTPS,
                "3" => ProxyType.SOCKS5,
                _ => ProxyType.HTTP
            };
        }
        GlobalState.CurrentProxyType = selectedType;
        await PerformDeepScan(selectedType);
    }

    /// <summary>
    /// Performs a deep scan across all proxy sources for the specified type.
    /// Tests nodes in batches and selects the best one (fastest < 1s, or best available).
    /// </summary>
    private static async Task PerformDeepScan(ProxyType proxyType)
    {
        const int BATCH_SIZE = 10;

        ModernUI.DrawLogo();
        ModernUI.Print($"INITIATING DEEP SCAN - {proxyType}", ModernUI.MsgType.Warning);
        ModernUI.Print("Collecting proxy nodes from database...", ModernUI.MsgType.Wait);

        var sources = proxyType switch
        {
            ProxyType.HTTP => Constants.ProxySources,
            ProxyType.HTTPS => Constants.HttpsSources,
            ProxyType.SOCKS5 => Constants.Socks5Sources,
            _ => Constants.ProxySources
        };

        var allProxies = new HashSet<string>();
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        foreach (var source in sources)
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
                // Individual proxy source may be unreachable - skip and continue
            }
        }
        Console.WriteLine();

        if (allProxies.Count == 0)
        {
            ModernUI.Print("No proxies found in database.", ModernUI.MsgType.Error);
            ModernUI.Pause();
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
                        var (ok, latency) = await ProxyManager.TestProxy(proxy);
                        if (ok) batchResults.Add((proxy, latency));
                    }
                    catch
                    {
                        // Proxy unreachable - skip
                    }
                    finally
                    {
                        semaphore.Release();
                        int current = Interlocked.Increment(ref tested);
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

        string? selectedIp = null;
        long selectedLatency = 0;

        if (bestFallback != null)
        {
            selectedIp = bestFallback;
            selectedLatency = bestFallbackLatency;
        }
        else if (results.Count > 0)
        {
            var best = results.OrderBy(x => x.ms).First();
            selectedIp = best.ip;
            selectedLatency = best.ms;
        }

        if (selectedIp != null)
        {
            var cacheEntry = new ProxyCacheEntry
            {
                Address = selectedIp,
                Type = proxyType,
                LatencyMs = selectedLatency,
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
            };

            GlobalState.CachedBestProxy = cacheEntry;
            GlobalState.GlobalProxyAddress = selectedIp;
            GlobalState.GlobalProxy = new WebProxy(selectedIp);
            GlobalState.LastProxyChangeTimestamp = cacheEntry.Timestamp;

            ConfigManager.SaveProxyCache(cacheEntry);

            ModernUI.Print("Deep Scan Complete!", ModernUI.MsgType.Success);
            Console.WriteLine();
            ModernUI.DrawBox($"BEST {proxyType} PROXY SELECTED", () =>
            {
                Console.WriteLine($"   Address:  {ModernUI.C_GREEN}{selectedIp}{ModernUI.C_RESET}");
                Console.WriteLine($"   Latency:  {ModernUI.C_CYAN}{selectedLatency}ms{ModernUI.C_RESET}");
                Console.WriteLine($"   Type:     {ModernUI.C_YELLOW}{proxyType}{ModernUI.C_RESET}");
                Console.WriteLine($"   Saved:    Session Cache");
            });
        }
        else
        {
            ModernUI.Print("Deep Scan failed to find any working nodes.", ModernUI.MsgType.Error);
            ModernUI.Print("Falling back to direct connection.", ModernUI.MsgType.Warning);
            GlobalState.GlobalProxy = null;
            GlobalState.GlobalProxyAddress = "None";
        }
        ModernUI.Pause();
    }

    /// <summary>
    /// Tests the currently active proxy for connectivity and latency.
    /// </summary>
    private static async Task TestCurrentProxy()
    {
        if (GlobalState.GlobalProxy == null)
        {
            ModernUI.Print("No proxy currently active.", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }

        ModernUI.Print("Testing current proxy...", ModernUI.MsgType.Wait);
        var sw = Stopwatch.StartNew();
        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = GlobalState.GlobalProxy,
                UseProxy = true
            };
            using var c = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            var r = await c.GetAsync("http://clients3.google.com/generate_204");
            sw.Stop();
            ModernUI.Print($"Proxy is working! Latency: {sw.ElapsedMilliseconds}ms", ModernUI.MsgType.Success);
        }
        catch (Exception ex)
        {
            sw.Stop();
            ModernUI.Print($"Proxy test failed: {ex.Message}", ModernUI.MsgType.Error);
            ModernUI.Print("Consider running a new deep scan.", ModernUI.MsgType.Warning);
        }
        ModernUI.Pause();
    }

    /// <summary>
    /// Configures startup behavior for proxy selection.
    /// </summary>
    private static async Task ConfigureStartupBehavior()
    {
        ModernUI.DrawLogo();
        ModernUI.DrawBox("PROXY STARTUP CONFIGURATION", () =>
        {
            Console.WriteLine("   Configure how PULSAR handles proxy selection");
            Console.WriteLine("   when the application starts.");
            Console.WriteLine();
            Console.WriteLine($"   Current Settings:");
            Console.WriteLine($"   • Deep Scan on Startup: {(GlobalState.DeepScanOnStartup ? "YES" : "NO")}");
            Console.WriteLine($"   • Skip Prompt:          {(GlobalState.SkipDeepScanPrompt ? "YES" : "NO")}");
            Console.WriteLine($"   • Use Cache:            {(GlobalState.UseDeepScanCache ? "YES" : "NO")}");
        });
        Console.WriteLine();
        ModernUI.DrawMenuOption("1", "Enable Deep Scan on Startup");
        ModernUI.DrawMenuOption("2", "Disable Deep Scan Prompt (Use Direct)");
        ModernUI.DrawMenuOption("3", "Enable Cache-First Mode");
        ModernUI.DrawMenuOption("4", "Reset to Defaults (Always Ask)");
        ModernUI.DrawMenuOption("X", "Back");
        Console.WriteLine();
        string choice = ModernUI.Prompt("Option");
        switch (choice)
        {
            case "1":
                GlobalState.DeepScanOnStartup = true;
                GlobalState.SkipDeepScanPrompt = false;
                ModernUI.Print("Deep scan will run automatically on startup.", ModernUI.MsgType.Success);
                break;
            case "2":
                GlobalState.SkipDeepScanPrompt = true;
                GlobalState.DeepScanOnStartup = false;
                ModernUI.Print("Will use direct connection by default.", ModernUI.MsgType.Success);
                break;
            case "3":
                GlobalState.UseDeepScanCache = true;
                ModernUI.Print("Cache-first mode enabled.", ModernUI.MsgType.Success);
                break;
            case "4":
                GlobalState.DeepScanOnStartup = false;
                GlobalState.SkipDeepScanPrompt = false;
                GlobalState.UseDeepScanCache = false;
                ModernUI.Print("Settings reset to defaults.", ModernUI.MsgType.Success);
                break;
        }
        ModernUI.Pause();
    }
}