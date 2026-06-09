// SPDX-License-Identifier: MIT
#nullable disable
#pragma warning disable CA1416
#pragma warning disable SYSLIB0014

using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using PULSAR.Cluster;
using PULSAR.Configuration;
using PULSAR.Core;
using PULSAR.Networking;
using PULSAR.PostExploitation;
using PULSAR.Recon;
using PULSAR.Settings;
using PULSAR.UI;

namespace PULSAR;

/// <summary>
/// PULSAR v3.5 — Open-source network security toolkit.
/// Entry point and main menu loop.
/// </summary>
class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        PlatformHelper.EnableAnsi();
        Console.Title = $"PULSAR v{Constants.VersionStr} | Open Source Edition";
        ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        ThreadPool.SetMinThreads(200, 200);

        PathManager.LoadSettings();
        StartupManager.LoadSettings();

        string startupMode = null;
        if (args.Length > 0)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--startup-mode="))
                {
                    startupMode = arg.Substring("--startup-mode=".Length);
                }
            }
        }
        ModernUI.DrawLogo();

        await ModernUI.LoadingBar("Initializing Core");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            await ModernUI.LoadingBar("Checking Linux Deps");

        GlobalState.TargetBlacklist.Add("127.0.0.1");
        GlobalState.TargetBlacklist.Add("localhost");

        await PathManager.CheckFirstRunPathPrompt();

        if (startupMode == null && !GlobalState.SkipDeepScanPrompt)
        {
            await HandleStartupProxyPrompt();
        }
        else if (GlobalState.CachedBestProxy != null && GlobalState.UseDeepScanCache)
        {
            GlobalState.GlobalProxyAddress = GlobalState.CachedBestProxy.Address;
            GlobalState.GlobalProxy = CreateProxyFromEntry(GlobalState.CachedBestProxy);
            GlobalState.CurrentProxyType = GlobalState.CachedBestProxy.Type;
            GlobalState.LastProxyChangeTimestamp = GlobalState.CachedBestProxy.Timestamp;

            ModernUI.DrawLogo();
            ModernUI.Print($"Using cached proxy from memory: {GlobalState.GlobalProxyAddress}", ModernUI.MsgType.Success);
            ModernUI.Print($"Type: {GlobalState.CurrentProxyType} | Latency: {GlobalState.CachedBestProxy.LatencyMs}ms", ModernUI.MsgType.Info);
            await Task.Delay(2000);
        }

        using (var cts = new CancellationTokenSource())
        {
            _ = ProxyRotatorTask(cts.Token);
            if (startupMode == "cluster_master")
            {
                await PlatformHelper.RunCancellable(token => ClusterSystem.RunMasterAutoStart(token));
                return;
            }
            else if (startupMode == "cluster_slave")
            {
                await PlatformHelper.RunCancellable(token => ClusterSystem.RunSlaveAutoStart(token));
                return;
            }

            while (true)
            {
                try
                {
                    ModernUI.DrawLogo();
                    ModernUI.DrawStatusBar();
                    Console.WriteLine($"   {ModernUI.C_BOLD}OFFENSIVE OPERATIONS{ModernUI.C_RESET}");
                    ModernUI.DrawMenuOption("1", "DoS / Stresser (Layer 4/7 & Amp)", "2", "Traffic Monitor (Sniffer)");
                    ModernUI.DrawMenuOption("3", "Local Network Scanner", "20", "WiFi Deauth Attack");
                    ModernUI.DrawMenuOption("X", "Exit System");
                    Console.WriteLine();
                    Console.WriteLine($"   {ModernUI.C_BOLD}RECONNAISSANCE{ModernUI.C_RESET}");
                    ModernUI.DrawMenuOption("4", "GeoIP Lookup", "5", "Traceroute");
                    ModernUI.DrawMenuOption("6", "WHOIS Lookup", "7", "WiFi Scanner");
                    ModernUI.DrawMenuOption("8", "Subnet Calculator", "9", "Port Scanner");
                    Console.WriteLine();
                    Console.WriteLine($"   {ModernUI.C_BOLD}WEB & CRYPTO{ModernUI.C_RESET}");
                    ModernUI.DrawMenuOption("10", "Directory Scanner", "11", "Subdomain Enumerator");
                    ModernUI.DrawMenuOption("12", "Header Analyzer", "13", "Hash Gen / Crypto");
                    ModernUI.DrawMenuOption("14", "SSL Inspector", "15", "Tech Detector");
                    ModernUI.DrawMenuOption("16", "Web Crawler", "17", "Web Brute-Force");
                    ModernUI.DrawMenuOption("18", "URL Traffic Gen", "19", "Hash Cracker");
                    Console.WriteLine();

                    Console.WriteLine($"   {ModernUI.C_GRAY}──────────────────────────────────────────────────{ModernUI.C_RESET}");
                    Console.WriteLine($"   {ModernUI.C_BOLD}POST-EXPLOITATION (Win){ModernUI.C_RESET}");
                    ModernUI.DrawMenuOption("21", "Privilege Escalation Scan");
                    ModernUI.DrawMenuOption("22", "Dump Creds (SAM/SYSTEM)");
                    ModernUI.DrawMenuOption("23", "System Cleanup (Logs/Tracks)");
                    Console.WriteLine($"   {ModernUI.C_GRAY}──────────────────────────────────────────────────{ModernUI.C_RESET}");
                    ModernUI.DrawMenuOption("S", "SYSTEM SETTINGS (Proxy/Rotation/PATH/Startup)");
                    ModernUI.DrawMenuOption("C", "CLUSTER MODE (DDoS Hive)");
                    ModernUI.DrawMenuOption("P", "PROXY DATABASE SCAN");
                    Console.WriteLine();

                    string selection = ModernUI.Prompt("Select Module");
                    switch (selection.ToLower())
                    {
                        case "1": await PlatformHelper.RunCancellable(token => ReconModules.RunAttackMenu(token)); break;
                        case "2":
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                if (PlatformHelper.IsAdministrator()) await PlatformHelper.RunCancellable(token => ReconModules.RunPassiveSniffer(token));
                                else { ModernUI.Print("Admin Required", ModernUI.MsgType.Error); ModernUI.Pause(); }
                            }
                            else await PlatformHelper.RunCancellable(token => ReconModules.RunLinuxPacketCapture(token));
                            break;
                        case "3": await PlatformHelper.RunCancellable(token => ReconModules.RunAdvancedScanner(token)); break;
                        case "4": await PlatformHelper.RunCancellable(token => ReconModules.RunGeoLookup(token)); break;
                        case "5": await PlatformHelper.RunCancellable(token => ReconModules.RunTraceroute(token)); break;
                        case "6": await PlatformHelper.RunCancellable(token => ReconModules.RunWhois(token)); break;
                        case "7": await PlatformHelper.RunCancellable(token => ReconModules.RunWifiScan(token)); break;
                        case "8": ReconModules.RunIpCalculator(); break;
                        case "9": await PlatformHelper.RunCancellable(token => ReconModules.RunTargetPortScan(token)); break;
                        case "10": await PlatformHelper.RunCancellable(token => ScannerManager.RunDirectoryScanner(token)); break;
                        case "11": await PlatformHelper.RunCancellable(token => ScannerManager.RunSubdomainScanner(token)); break;
                        case "12": await PlatformHelper.RunCancellable(token => ReconModules.RunHeaderAnalyzer(token)); break;
                        case "13": ReconModules.RunCryptoTool(); break;
                        case "14": await PlatformHelper.RunCancellable(token => ReconModules.RunSSLInspector(token)); break;
                        case "15": await PlatformHelper.RunCancellable(token => ReconModules.RunTechDetector(token)); break;
                        case "16": await PlatformHelper.RunCancellable(token => ReconModules.RunWebCrawler(token)); break;
                        case "17": await PlatformHelper.RunCancellable(token => ReconModules.RunBruteForce(token)); break;
                        case "18": await PlatformHelper.RunCancellable(token => ReconModules.RunUrlSpammer(token)); break;
                        case "19": await PlatformHelper.RunCancellable(token => ReconModules.RunHashCracker(token)); break;
                        case "20": await PlatformHelper.RunCancellable(token => ReconModules.RunDeauthAttack(token)); break;
                        case "21": await PlatformHelper.RunCancellable(token => PostExploitationManager.RunPrivEscScan(token)); break;
                        case "22": await PlatformHelper.RunCancellable(token => PostExploitationManager.RunCredentialDump(token)); break;
                        case "23": await PlatformHelper.RunCancellable(token => SystemCleaner.RunCleanup(token)); break;
                        case "c": await PlatformHelper.RunCancellable(token => ClusterSystem.RunClusterMenu(token)); break;
                        case "p": await ProxyDatabaseScan.RunProxyScanMenu(); break;
                        case "s": await SettingsMenu.RunSettingsMenu(); break;
                        case "x": return;
                    }
                }
                catch (Exception ex)
                {
                    ModernUI.Print($"Module Error: {ex.Message}", ModernUI.MsgType.Error);
                    ModernUI.Pause();
                }
            }
        }
    }

    private static async Task HandleStartupProxyPrompt()
    {
        ModernUI.DrawLogo();
        Console.WriteLine($"\n   {ModernUI.C_BOLD}Network Configuration{ModernUI.C_RESET}\n");
        var cached = ConfigManager.LoadProxyCache();
        if (cached != null && GlobalState.UseDeepScanCache)
        {
            ModernUI.Print($"Found cached proxy: {cached.Address} ({cached.Type})", ModernUI.MsgType.Info);
            string useCached = ModernUI.Prompt("Use cached proxy? (y/n/scan)", "y");

            if (useCached.ToLower() == "y")
            {
                GlobalState.CachedBestProxy = cached;
                GlobalState.GlobalProxyAddress = cached.Address;
                GlobalState.GlobalProxy = CreateProxyFromEntry(cached);
                GlobalState.CurrentProxyType = cached.Type;
                GlobalState.LastProxyChangeTimestamp = cached.Timestamp;
                ModernUI.Print("Using cached proxy from previous session.", ModernUI.MsgType.Success);
                await Task.Delay(1500);
                return;
            }
            else if (useCached.ToLower() == "n")
            {
                ConfigManager.ClearProxyCache();
            }
        }

        string askProxy = ModernUI.Prompt("Perform deep scan for fastest proxy node? (y/n)", "y");
        if (askProxy.ToLower() == "y")
        {
            await ProxyDatabaseScan.RunDeepScanWithTypeSelection();
        }
        else
        {
            GlobalState.GlobalProxy = null;
            GlobalState.GlobalProxyAddress = "None";
            ModernUI.Print("Using Direct Connection.", ModernUI.MsgType.Info);
            await Task.Delay(1000);
        }
    }

    private static IWebProxy CreateProxyFromEntry(ProxyCacheEntry entry)
    {
        var proxy = new WebProxy(entry.Address);
        // WebProxy does not natively support SOCKS5 - requires MihaZupan.HttpToSocks5Proxy
        // Full SOCKS5 support can be added via the HttpToSocks5Proxy NuGet package
        return proxy;
    }

    private static async Task ProxyRotatorTask(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            if (GlobalState.ProxyRotationMinutes > 0 && GlobalState.LastProxyChangeTimestamp > 0 && !GlobalState.RotateOnEveryRequest)
            {
                long elapsed = DateTimeOffset.Now.ToUnixTimeSeconds() - GlobalState.LastProxyChangeTimestamp;
                if (elapsed >= GlobalState.ProxyRotationMinutes * 60)
                    await ProxyManager.FindAndSetGlobalProxy(true);
            }
        }
    }
}