// SPDX-License-Identifier: MIT
using System.IO.Compression;
using PULSAR.Core;
using PULSAR.Networking;
using PULSAR.UI;

namespace PULSAR.Recon;

/// <summary>
/// Manages wordlist acquisition for brute-force and hash-cracking operations.
/// Supports downloading the RockYou wordlist from GitHub, loading local files,
/// or providing a manual file path.
/// </summary>
public static class WordlistManager
{
    /// <summary>
    /// Provides an interactive wordlist selection menu. The user can:
    /// - Download the RockYou wordlist (0)
    /// - Select a local .txt file from the executable directory
    /// - Specify a manual file path
    /// </summary>
    /// <param name="type">Label for the wordlist type (e.g., "Dictionary", "Usernames", "Passwords").</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A list of lines from the selected wordlist.</returns>
    public static async Task<List<string>> GetWordlist(string type, CancellationToken token)
    {
        ModernUI.DrawLogo();

        var localFiles = new List<string>();
        try
        {
            localFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.txt", SearchOption.AllDirectories).ToList();
        }
        catch
        {
            // Directory may not exist or be inaccessible
        }
        Console.WriteLine($"   {ModernUI.C_BOLD}Select {type} Source:{ModernUI.C_RESET}\n");

        ModernUI.DrawMenuOption("0", "Download Huge List (RockYou - ~130MB Unpacked)");

        int fileIdxStart = 1;
        if (localFiles.Count > 0)
        {
            Console.WriteLine($"   {ModernUI.C_GRAY}--- Local Files Found ---{ModernUI.C_RESET}");
            for (int i = 0; i < localFiles.Count; i++)
            {
                string displayPath = localFiles[i].Replace(AppDomain.CurrentDomain.BaseDirectory, "");
                if (displayPath.Length > 40) displayPath = "..." + displayPath.Substring(displayPath.Length - 37);
                ModernUI.DrawMenuOption((fileIdxStart + i).ToString(), displayPath);
            }
        }
        ModernUI.DrawMenuOption("M", "Manual File Path");

        Console.WriteLine();
        string choice = ModernUI.Prompt("Option", "0");
        if (choice == "0")
        {
            return await DownloadAndUnpackRockYou(token);
        }
        else if (choice.ToLower() == "m")
        {
            string path = ModernUI.Prompt("Enter full file path");
            if (File.Exists(path)) return (await File.ReadAllLinesAsync(path, token)).ToList();
            ModernUI.Print("File not found.", ModernUI.MsgType.Error);
            return new List<string>();
        }
        else
        {
            if (int.TryParse(choice, out int idx))
            {
                int arrayIdx = idx - fileIdxStart;
                if (arrayIdx >= 0 && arrayIdx < localFiles.Count)
                {
                    try
                    {
                        ModernUI.Print($"Loading {Path.GetFileName(localFiles[arrayIdx])}...", ModernUI.MsgType.Wait);
                        var lines = await File.ReadAllLinesAsync(localFiles[arrayIdx], token);
                        return lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    }
                    catch { ModernUI.Print("Read error.", ModernUI.MsgType.Error); }
                }
            }
        }
        return new List<string>();
    }

    /// <summary>
    /// Downloads the RockYou wordlist tar.gz from GitHub, extracts it, and returns the lines.
    /// Falls back to direct connection if proxy download fails (with user warning).
    /// </summary>
    private static async Task<List<string>> DownloadAndUnpackRockYou(CancellationToken token)
    {
        string outputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rockyou.txt");
        string tempGz = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_rockyou.tar.gz");

        if (File.Exists(outputFile))
        {
            string reuse = ModernUI.Prompt("rockyou.txt already exists. Use it? (y/n/re-download)", "y");
            if (reuse.ToLower().StartsWith("y"))
                return (await File.ReadAllLinesAsync(outputFile, token)).ToList();
        }

        ModernUI.Print("Downloading rockyou.txt.tar.gz from GitHub...", ModernUI.MsgType.Wait);
        ModernUI.Print("This is a large file, please wait...", ModernUI.MsgType.Info);

        bool success = false;
        HttpClient? client = null;

        try
        {
            if (GlobalState.GlobalProxy != null)
            {
                client = ProxyManager.GetClient();
                ModernUI.Print("Attempting download via Proxy...", ModernUI.MsgType.Wait);
                using (var s = await client.GetStreamAsync(Constants.RockYouGzUrl, token))
                using (var fs = File.Create(tempGz))
                {
                    await s.CopyToAsync(fs, token);
                }
                success = true;
            }
            else
            {
                throw new Exception("No proxy set, falling back.");
            }
        }
        catch (Exception ex)
        {
            if (File.Exists(tempGz)) File.Delete(tempGz);
            ModernUI.Print($"Proxy Download Failed: {ex.Message}", ModernUI.MsgType.Error);

            ModernUI.Print("DANGER: Proxy failed.", ModernUI.MsgType.Warning);
            string q = ModernUI.Prompt("Download using DIRECT connection? (Your IP will be exposed) (y/n)", "n");

            if (q.ToLower() == "y")
            {
                try
                {
                    client = new HttpClient();
                    ModernUI.Print("Downloading DIRECTLY...", ModernUI.MsgType.Wait);
                    using (var s = await client.GetStreamAsync(Constants.RockYouGzUrl, token))
                    using (var fs = File.Create(tempGz))
                    {
                        await s.CopyToAsync(fs, token);
                    }
                    success = true;
                }
                catch (Exception ex2)
                {
                    ModernUI.Print($"Direct Download Failed: {ex2.Message}", ModernUI.MsgType.Error);
                }
            }
        }

        if (!success) return new List<string>();

        ModernUI.Print("Download complete. Unpacking...", ModernUI.MsgType.Wait);
        try
        {
            using (var fsIn = File.OpenRead(tempGz))
            using (var gzipStream = new GZipStream(fsIn, CompressionMode.Decompress))
            using (var fsOut = File.Create(outputFile))
            {
                // Skip tar header (512 bytes of file metadata)
                byte[] header = new byte[512];
                int totalRead = 0;
                while (totalRead < 512)
                {
                    int read = await gzipStream.ReadAsync(header, totalRead, 512 - totalRead, token);
                    if (read == 0) break;
                    totalRead += read;
                }
                await gzipStream.CopyToAsync(fsOut, token);
            }

            File.Delete(tempGz);

            ModernUI.Print("Extraction Complete!", ModernUI.MsgType.Success);
            ModernUI.Print("Loading into memory...", ModernUI.MsgType.Wait);
            return (await File.ReadAllLinesAsync(outputFile, token)).ToList();
        }
        catch (Exception ex)
        {
            ModernUI.Print($"Unpacking Failed: {ex.Message}", ModernUI.MsgType.Error);
            return new List<string>();
        }
    }
}