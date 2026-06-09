// SPDX-License-Identifier: MIT
using PULSAR.Core;
using PULSAR.Networking;
using PULSAR.UI;

namespace PULSAR.Recon;

/// <summary>
/// Web application scanning utilities including directory enumeration and subdomain discovery.
/// All HTTP operations route through the configured proxy.
/// </summary>
public static class ScannerManager
{
    /// <summary>
    /// Runs a web directory scanner against the specified URL.
    /// Checks common paths and reports found (200) and forbidden (403) directories.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    public static async Task RunDirectoryScanner(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string url = ModernUI.Prompt("URL (e.g. google.com)");
        if (!url.StartsWith("http")) url = "http://" + url;
        url = url.TrimEnd('/');

        await ProxyManager.PrepareConnection();
        ModernUI.Print($"Scanning {Constants.DefaultDirectories.Count} directories...", ModernUI.MsgType.Wait);

        using var c = ProxyManager.GetClient();
        c.Timeout = TimeSpan.FromSeconds(5);

        foreach (var d in Constants.DefaultDirectories)
        {
            if (token.IsCancellationRequested) break;

            Console.Write($"\r   Checking: /{d,-20}");

            try
            {
                var r = await c.GetAsync($"{url}/{d}", HttpCompletionOption.ResponseHeadersRead, token);
                Console.Write($"\r   {new string(' ', 40)}\r");

                if (r.IsSuccessStatusCode)
                {
                    ModernUI.Print($"FOUND: /{d} (HTTP {r.StatusCode})", ModernUI.MsgType.Success);
                }
                else if ((int)r.StatusCode == 403)
                {
                    ModernUI.Print($"FORBIDDEN: /{d} (HTTP 403)", ModernUI.MsgType.Warning);
                }
            }
            catch
            {
                // Path not found or request timed out
            }
        }
        Console.WriteLine();
        ModernUI.Print("Scan complete.", ModernUI.MsgType.Info);
        ModernUI.Pause();
    }

    /// <summary>
    /// Runs a subdomain scanner against the specified domain.
    /// Tries DNS resolution first, then falls back to HTTP probing.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    public static async Task RunSubdomainScanner(CancellationToken token)
    {
        ModernUI.DrawLogo();
        ModernUI.ShowConnectionInfo(true);
        string domain = ModernUI.Prompt("Domain");

        await ProxyManager.PrepareConnection();
        ModernUI.Print($"Scanning {Constants.DefaultSubdomains.Count} subdomains via Proxy...", ModernUI.MsgType.Wait);

        using var c = ProxyManager.GetClient();
        c.Timeout = TimeSpan.FromSeconds(5);

        foreach (var sub in Constants.DefaultSubdomains)
        {
            if (token.IsCancellationRequested) break;
            try
            {
                var addresses = await Dns.GetHostAddressesAsync($"{sub}.{domain}", token);
                if (addresses.Length > 0)
                {
                    ModernUI.Print($"Found: {sub}.{domain} -> {addresses[0]}", ModernUI.MsgType.Success);
                }
            }
            catch
            {
                try
                {
                    var r = await c.GetAsync($"http://{sub}.{domain}", HttpCompletionOption.ResponseHeadersRead, token);
                    if (r.IsSuccessStatusCode || (int)r.StatusCode < 500)
                    {
                        ModernUI.Print($"Found: {sub}.{domain} (HTTP {(int)r.StatusCode})", ModernUI.MsgType.Success);
                    }
                }
                catch
                {
                    // Subdomain does not resolve via DNS or HTTP
                }
            }
        }
        ModernUI.Pause();
    }
}