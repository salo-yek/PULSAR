// SPDX-License-Identifier: MIT
using PULSAR.Configuration;
using PULSAR.Core;
using PULSAR.Networking;
using PULSAR.UI;

namespace PULSAR.Settings;

/// <summary>
/// System settings menus including proxy configuration, PATH management,
/// and Windows startup configuration.
/// </summary>
public static class SettingsMenu
{
    /// <summary>
    /// Shows the main system settings menu.
    /// </summary>
    public static async Task RunSettingsMenu()
    {
        while (true)
        {
            ModernUI.DrawLogo();
            var pathSettings = PathManager.GetSettings();
            var startupSettings = StartupManager.GetSettings();

            ModernUI.DrawBox("SYSTEM CONFIGURATION", () =>
            {
                Console.WriteLine($"   Proxy Node:    {GlobalState.GlobalProxyAddress}");
                Console.WriteLine($"   Proxy Type:    {GlobalState.CurrentProxyType}");
                Console.WriteLine($"   Rotation Time: {(GlobalState.ProxyRotationMinutes > 0 ? GlobalState.ProxyRotationMinutes + " mins" : "Disabled")}");
                Console.WriteLine($"   Auto-Rotate:   {(GlobalState.RotateOnEveryRequest ? "ENABLED (Per Request)" : "DISABLED")}");
                Console.WriteLine($"   Source:        {(GlobalState.UseDeepScanCache ? "Deep Scan Cache" : "Live Global Scan")}");
                Console.WriteLine($"   PATH Enabled:  {(pathSettings.PathEnabled ? ModernUI.C_GREEN + "YES" + ModernUI.C_RESET + " (" + pathSettings.CommandName + ")" : ModernUI.C_GRAY + "NO" + ModernUI.C_RESET)}");
                Console.WriteLine($"   Startup:       {(startupSettings.StartupEnabled ? ModernUI.C_GREEN + "YES" + ModernUI.C_RESET + " (" + startupSettings.StartupMode + ")" : ModernUI.C_GRAY + "NO" + ModernUI.C_RESET)}");
            });
            Console.WriteLine();
            Console.WriteLine($"   {ModernUI.C_BOLD}PROXY & NETWORK{ModernUI.C_RESET}");
            ModernUI.DrawMenuOption("1", "Set Rotation Interval");
            ModernUI.DrawMenuOption("2", "Toggle Auto-Rotate");
            ModernUI.DrawMenuOption("3", "Toggle Source (Global/Cache)");
            ModernUI.DrawMenuOption("4", "Force New Proxy Connection");
            ModernUI.DrawMenuOption("5", "Enable/Disable Proxy");
            ModernUI.DrawMenuOption("9", "FORCE DEEP SCAN (Best Latency Search)");
            ModernUI.DrawMenuOption("P", "Proxy Database Scan Menu");
            Console.WriteLine();

            Console.WriteLine($"   {ModernUI.C_BOLD}SYSTEM INTEGRATION{ModernUI.C_RESET}");
            ModernUI.DrawMenuOption("A", "PATH Management");
            ModernUI.DrawMenuOption("B", "Startup Apps Configuration");
            Console.WriteLine();

            ModernUI.DrawMenuOption("X", "Back to Main Menu");
            Console.WriteLine();
            string choice = ModernUI.Prompt("Option");

            switch (choice)
            {
                case "1":
                    string input = ModernUI.Prompt("Minutes (0=Disable)");
                    if (int.TryParse(input, out int val) && val >= 0)
                    {
                        GlobalState.ProxyRotationMinutes = val;
                        ModernUI.Print("Saved.", ModernUI.MsgType.Success);
                    }
                    break;
                case "2":
                    GlobalState.RotateOnEveryRequest = !GlobalState.RotateOnEveryRequest;
                    ModernUI.Print($"Auto-Rotate: {GlobalState.RotateOnEveryRequest}", ModernUI.MsgType.Success);
                    break;
                case "3":
                    GlobalState.UseDeepScanCache = !GlobalState.UseDeepScanCache;
                    ModernUI.Print("Source Changed", ModernUI.MsgType.Success);
                    break;
                case "4":
                    await ProxyManager.FindAndSetGlobalProxy(true);
                    ModernUI.Pause();
                    break;
                case "5":
                    if (GlobalState.GlobalProxy != null)
                    {
                        GlobalState.GlobalProxy = null;
                        GlobalState.GlobalProxyAddress = "None";
                        ModernUI.Print("Proxy Disabled (Direct Connection).", ModernUI.MsgType.Success);
                    }
                    else
                    {
                        await ProxyManager.FindAndSetGlobalProxy(true);
                    }
                    ModernUI.Pause();
                    break;
                case "a":
                    await PathManagementMenu();
                    break;
                case "b":
                    await StartupAppsMenu();
                    break;
                case "9":
                    await ProxyDatabaseScan.RunDeepScanWithTypeSelection();
                    ModernUI.Pause();
                    break;
                case "p":
                    await ProxyDatabaseScan.RunProxyScanMenu();
                    break;
                case "x": return;
            }
        }
    }

    /// <summary>
    /// Shows the PATH management submenu.
    /// </summary>
    private static async Task PathManagementMenu()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ModernUI.Print("PATH management is only available on Windows.", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }
        while (true)
        {
            ModernUI.DrawLogo();
            var settings = PathManager.GetSettings();
            ModernUI.DrawBox("PATH INTEGRATION SETTINGS", () =>
            {
                Console.WriteLine($"   Status:   {(settings.PathEnabled ? ModernUI.C_GREEN + "ENABLED" : ModernUI.C_RED + "DISABLED")}{ModernUI.C_RESET}");
                Console.WriteLine($"   Command:  {settings.CommandName}");
                Console.WriteLine();
                Console.WriteLine("   PATH integration allows you to run PULSAR from anywhere");
                Console.WriteLine("   by typing your custom command in any terminal.");
            });
            Console.WriteLine();
            ModernUI.DrawMenuOption("1", "Enable PATH Integration");
            ModernUI.DrawMenuOption("2", "Disable PATH Integration");
            ModernUI.DrawMenuOption("3", "Change Command Name");
            ModernUI.DrawMenuOption("X", "Back");
            Console.WriteLine();
            string choice = ModernUI.Prompt("Option");
            switch (choice)
            {
                case "1":
                    await PathManager.AddToPath();
                    ModernUI.Pause();
                    break;
                case "2":
                    await PathManager.RemoveFromPath();
                    ModernUI.Pause();
                    break;
                case "3":
                    await PathManager.ChangeCommandName();
                    ModernUI.Pause();
                    break;
                case "x": return;
            }
        }
    }

    /// <summary>
    /// Shows the startup apps configuration submenu.
    /// </summary>
    private static async Task StartupAppsMenu()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ModernUI.Print("Startup apps configuration is only available on Windows.", ModernUI.MsgType.Error);
            ModernUI.Pause();
            return;
        }
        while (true)
        {
            ModernUI.DrawLogo();
            var settings = StartupManager.GetSettings();
            ModernUI.DrawBox("STARTUP APPS CONFIGURATION", () =>
            {
                Console.WriteLine($"   Status:        {(settings.StartupEnabled ? ModernUI.C_GREEN + "ENABLED" : ModernUI.C_RED + "DISABLED")}{ModernUI.C_RESET}");
                Console.WriteLine($"   Startup Mode:  {settings.StartupMode}");
                Console.WriteLine();
                Console.WriteLine("   Configure PULSAR to automatically launch when");
                Console.WriteLine("   Windows starts up.");
            });
            Console.WriteLine();
            ModernUI.DrawMenuOption("1", "Enable Startup (Normal Menu)");
            ModernUI.DrawMenuOption("2", "Enable Startup (Cluster Master - Auto)");
            ModernUI.DrawMenuOption("3", "Enable Startup (Cluster Slave - Auto)");
            ModernUI.DrawMenuOption("4", "Disable Startup");
            ModernUI.DrawMenuOption("X", "Back");
            Console.WriteLine();
            string choice = ModernUI.Prompt("Option");
            switch (choice)
            {
                case "1":
                    StartupManager.EnableStartup("menu");
                    ModernUI.Pause();
                    break;
                case "2":
                    StartupManager.EnableStartup("cluster_master");
                    ModernUI.Print("PULSAR will start as Cluster Master on system boot.", ModernUI.MsgType.Info);
                    ModernUI.Pause();
                    break;
                case "3":
                    StartupManager.EnableStartup("cluster_slave");
                    ModernUI.Print("PULSAR will start as Cluster Slave on system boot.", ModernUI.MsgType.Info);
                    ModernUI.Pause();
                    break;
                case "4":
                    StartupManager.DisableStartup();
                    ModernUI.Pause();
                    break;
                case "x": return;
            }
        }
    }
}