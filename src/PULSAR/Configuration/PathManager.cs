// SPDX-License-Identifier: MIT
using System.Runtime.InteropServices;
using System.Text.Json;
using PULSAR.UI;

namespace PULSAR.Configuration;

/// <summary>
/// Manages system PATH integration on Windows, allowing PULSAR to be launched
/// from any command prompt using a user-defined command name.
/// </summary>
public static class PathManager
{
    private const string SettingsFile = "pulsar_path_settings.json";

    /// <summary>
    /// JSON-serializable settings for PATH integration.
    /// </summary>
    public class PathSettings
    {
        /// <summary>Whether PATH integration is enabled.</summary>
        public bool PathEnabled { get; set; } = false;
        /// <summary>The command name used to launch PULSAR (e.g., "PULSAR").</summary>
        public string CommandName { get; set; } = "PULSAR";
        /// <summary>Whether the first-run prompt has been shown.</summary>
        public bool AskedOnFirstRun { get; set; } = false;
    }

    private static PathSettings _settings = new();

    /// <summary>
    /// Loads PATH settings from the JSON settings file.
    /// </summary>
    public static void LoadSettings()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFile);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _settings = JsonSerializer.Deserialize<PathSettings>(json) ?? new PathSettings();
            }
        }
        catch
        {
            // Missing or corrupted settings - use defaults
        }
    }

    /// <summary>
    /// Saves the current PATH settings to the JSON settings file.
    /// </summary>
    public static void SaveSettings()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFile);
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Non-critical settings write failure
        }
    }

    /// <summary>
    /// Checks if this is the first run and optionally prompts the user to add PULSAR to PATH.
    /// On subsequent runs, updates the batch file if the executable path has changed.
    /// </summary>
    public static async Task CheckFirstRunPathPrompt()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        LoadSettings();

        if (!_settings.AskedOnFirstRun)
        {
            _settings.AskedOnFirstRun = true;
            SaveSettings();

            ModernUI.DrawLogo();
            Console.WriteLine();
            ModernUI.Print("First-time setup detected!", ModernUI.MsgType.Info);
            Console.WriteLine();
            ModernUI.DrawBox("PATH INTEGRATION", () =>
            {
                Console.WriteLine("   Add PULSAR to your system PATH to run it from anywhere");
                Console.WriteLine("   by typing 'pulsar' in any terminal/command prompt.");
                Console.WriteLine();
                Console.WriteLine("   This will allow you to launch PULSAR without navigating");
                Console.WriteLine("   to its installation directory.");
            });
            Console.WriteLine();

            string answer = ModernUI.Prompt("Add PULSAR to PATH? (y/n)", "y");

            if (answer.ToLower() == "y")
            {
                await AddToPath();
            }

            await Task.Delay(1000);
        }
        else if (_settings.PathEnabled)
        {
            await UpdatePathIfNeeded();
        }
    }

    /// <summary>
    /// Adds the PULSAR executable directory to the user's PATH environment variable
    /// and creates a batch file launcher.
    /// </summary>
    public static async Task AddToPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ModernUI.Print("PATH integration is only available on Windows.", ModernUI.MsgType.Error);
            return;
        }
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule!.FileName;
            string exeDir = Path.GetDirectoryName(exePath)!;
            string cmdName = _settings.CommandName;
            string batchContent = $"@echo off\r\n\"{exePath}\" %*";
            string batchPath = Path.Combine(exeDir, $"{cmdName}.bat");
            File.WriteAllText(batchPath, batchContent);

            string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";

            if (!userPath.Split(';').Any(p => p.Trim().Equals(exeDir, StringComparison.OrdinalIgnoreCase)))
            {
                string newPath = string.IsNullOrEmpty(userPath) ? exeDir : userPath + ";" + exeDir;
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            }
            _settings.PathEnabled = true;
            SaveSettings();
            ModernUI.Print($"Successfully added to PATH! You can now run '{cmdName}' from anywhere.", ModernUI.MsgType.Success);
            ModernUI.Print("Note: You may need to restart your terminal for changes to take effect.", ModernUI.MsgType.Info);
        }
        catch (Exception ex)
        {
            ModernUI.Print($"Failed to add to PATH: {ex.Message}", ModernUI.MsgType.Error);
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Removes PULSAR from the user's PATH and deletes the batch file launcher.
    /// </summary>
    public static async Task RemoveFromPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ModernUI.Print("PATH integration is only available on Windows.", ModernUI.MsgType.Error);
            return;
        }
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule!.FileName;
            string exeDir = Path.GetDirectoryName(exePath)!;
            string cmdName = _settings.CommandName;
            string batchPath = Path.Combine(exeDir, $"{cmdName}.bat");
            if (File.Exists(batchPath))
                File.Delete(batchPath);

            string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            var paths = userPath.Split(';').Where(p => !p.Trim().Equals(exeDir, StringComparison.OrdinalIgnoreCase)).ToList();
            string newPath = string.Join(";", paths);
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);

            _settings.PathEnabled = false;
            SaveSettings();
            ModernUI.Print("Successfully removed from PATH.", ModernUI.MsgType.Success);
        }
        catch (Exception ex)
        {
            ModernUI.Print($"Failed to remove from PATH: {ex.Message}", ModernUI.MsgType.Error);
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Prompts the user to change the command name used for PATH launching.
    /// </summary>
    public static async Task ChangeCommandName()
    {
        ModernUI.DrawLogo();
        Console.WriteLine();
        ModernUI.Print($"Current command: {_settings.CommandName}", ModernUI.MsgType.Info);
        string newName = ModernUI.Prompt("Enter new command name (e.g., PULSAR, pulsar, psr)");
        if (string.IsNullOrWhiteSpace(newName))
        {
            ModernUI.Print("Invalid command name.", ModernUI.MsgType.Error);
            return;
        }

        if (_settings.PathEnabled)
        {
            try
            {
                string exeDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
                string oldBatch = Path.Combine(exeDir, $"{_settings.CommandName}.bat");
                if (File.Exists(oldBatch))
                    File.Delete(oldBatch);
            }
            catch
            {
                // Old batch file may not exist or may be locked
            }
        }

        _settings.CommandName = newName;
        SaveSettings();

        if (_settings.PathEnabled)
        {
            await AddToPath();
        }
        ModernUI.Print($"Command name changed to: {newName}", ModernUI.MsgType.Success);
    }

    private static async Task UpdatePathIfNeeded()
    {
        try
        {
            string currentExe = Process.GetCurrentProcess().MainModule!.FileName;
            string exeDir = Path.GetDirectoryName(currentExe)!;
            string batchPath = Path.Combine(exeDir, $"{_settings.CommandName}.bat");
            if (File.Exists(batchPath))
            {
                string batchContent = File.ReadAllText(batchPath);
                if (!batchContent.Contains(currentExe))
                {
                    string newBatchContent = $"@echo off\r\n\"{currentExe}\" %*";
                    File.WriteAllText(batchPath, newBatchContent);
                }
            }
        }
        catch
        {
            // Batch file update is best-effort; version upgrade should still proceed
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Returns the current PATH settings.
    /// </summary>
    public static PathSettings GetSettings() => _settings;
}