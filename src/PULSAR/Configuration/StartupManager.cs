// SPDX-License-Identifier: MIT
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;
using PULSAR.UI;

namespace PULSAR.Configuration;

/// <summary>
/// Manages Windows startup registration via the Run registry key.
/// Allows PULSAR to automatically launch with a specified startup mode on system boot.
/// </summary>
public static class StartupManager
{
    private const string SettingsFile = "pulsar_startup_settings.json";

    /// <summary>
    /// JSON-serializable settings for Windows startup integration.
    /// </summary>
    public class StartupSettings
    {
        /// <summary>Whether startup registration is enabled.</summary>
        public bool StartupEnabled { get; set; } = false;
        /// <summary>The startup mode (e.g., "menu", "cluster_master", "cluster_slave").</summary>
        public string StartupMode { get; set; } = "menu";
    }

    private static StartupSettings _settings = new();

    /// <summary>
    /// Loads startup settings from the JSON settings file.
    /// </summary>
    public static void LoadSettings()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFile);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _settings = JsonSerializer.Deserialize<StartupSettings>(json) ?? new StartupSettings();
            }
        }
        catch
        {
            // Missing or corrupted settings - use defaults
        }
    }

    /// <summary>
    /// Saves the current startup settings to the JSON settings file.
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
    /// Registers PULSAR in the Windows Run registry key for automatic startup.
    /// </summary>
    /// <param name="mode">The startup mode to use when launched automatically.</param>
    public static void EnableStartup(string mode)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ModernUI.Print("Startup apps feature is only available on Windows.", ModernUI.MsgType.Error);
            return;
        }
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule!.FileName;
            string args = mode != "menu" ? $"--startup-mode={mode}" : "";
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    key.SetValue("PULSAR", $"\"{exePath}\" {args}");
                }
            }
            _settings.StartupEnabled = true;
            _settings.StartupMode = mode;
            SaveSettings();
            ModernUI.Print("Added to Windows startup successfully!", ModernUI.MsgType.Success);
        }
        catch (Exception ex)
        {
            ModernUI.Print($"Failed to add to startup: {ex.Message}", ModernUI.MsgType.Error);
        }
    }

    /// <summary>
    /// Removes PULSAR from the Windows Run registry key.
    /// </summary>
    public static void DisableStartup()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ModernUI.Print("Startup apps feature is only available on Windows.", ModernUI.MsgType.Error);
            return;
        }
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key != null)
                {
                    if (key.GetValue("PULSAR") != null)
                    {
                        key.DeleteValue("PULSAR");
                    }
                }
            }
            _settings.StartupEnabled = false;
            SaveSettings();
            ModernUI.Print("Removed from Windows startup successfully!", ModernUI.MsgType.Success);
        }
        catch (Exception ex)
        {
            ModernUI.Print($"Failed to remove from startup: {ex.Message}", ModernUI.MsgType.Error);
        }
    }

    /// <summary>
    /// Returns the current startup settings.
    /// </summary>
    public static StartupSettings GetSettings() => _settings;
}