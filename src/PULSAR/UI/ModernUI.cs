// SPDX-License-Identifier: MIT
using PULSAR.Core;

namespace PULSAR.UI;

/// <summary>
/// Console user interface framework providing ANSI-colored output, prompts,
/// menus, status bars, and loading animations.
/// </summary>
public static class ModernUI
{
    // ── ANSI Color Constants ──────────────────────────────────────────
    /// <summary>Reset all formatting.</summary>
    public const string C_RESET = "\u001b[0m";
    /// <summary>Cyan foreground.</summary>
    public const string C_CYAN = "\u001b[36m";
    /// <summary>Blue foreground.</summary>
    public const string C_BLUE = "\u001b[34m";
    /// <summary>Green foreground.</summary>
    public const string C_GREEN = "\u001b[32m";
    /// <summary>Red foreground.</summary>
    public const string C_RED = "\u001b[31m";
    /// <summary>Yellow foreground.</summary>
    public const string C_YELLOW = "\u001b[33m";
    /// <summary>Magenta foreground.</summary>
    public const string C_MAGENTA = "\u001b[35m";
    /// <summary>White foreground.</summary>
    public const string C_WHITE = "\u001b[97m";
    /// <summary>Gray foreground.</summary>
    public const string C_GRAY = "\u001b[90m";
    /// <summary>Bold text.</summary>
    public const string C_BOLD = "\u001b[1m";

    /// <summary>
    /// Message type classification for <see cref="Print"/>.
    /// </summary>
    public enum MsgType
    {
        /// <summary>Generic informational message.</summary>
        Info,
        /// <summary>Successful operation.</summary>
        Success,
        /// <summary>Error or failure.</summary>
        Error,
        /// <summary>Warning or caution.</summary>
        Warning,
        /// <summary>User input prompt.</summary>
        Input,
        /// <summary>Wait/progress indication.</summary>
        Wait
    }

    /// <summary>
    /// Draws the PULSAR logo and version banner.
    /// </summary>
    public static void DrawLogo()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine($"   {C_BOLD}{C_WHITE}P  U  L  S  A  R{C_RESET}  {C_GRAY}//  SYSTEM {Constants.VersionStr}{C_RESET}");
        Console.WriteLine($"   {C_GRAY}──────────────────────────────────────────────────{C_RESET}");
    }

    /// <summary>
    /// Draws the status bar showing proxy state and current time.
    /// </summary>
    public static void DrawStatusBar()
    {
        string proxyStatus = GlobalState.GlobalProxyAddress == "None"
            ? $"{C_GRAY}Direct{C_RESET}"
            : $"{C_GREEN}Proxied{C_RESET}";

        string date = DateTime.Now.ToString("HH:mm");
        Console.WriteLine($"   {C_GRAY}NET:{C_RESET} {proxyStatus}  {C_GRAY}│{C_RESET}  {C_GRAY}TIME:{C_RESET} {date}");
        Console.WriteLine($"   {C_GRAY}──────────────────────────────────────────────────{C_RESET}");
        Console.WriteLine();
    }

    /// <summary>
    /// Prints a formatted message with a colored prefix based on message type.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="type">The message type determining the prefix color/style.</param>
    public static void Print(string text, MsgType type = MsgType.Info)
    {
        string prefix = type switch
        {
            MsgType.Success => $"{C_GREEN}✔{C_RESET}",
            MsgType.Error => $"{C_RED}✖{C_RESET}",
            MsgType.Warning => $"{C_YELLOW}!{C_RESET}",
            MsgType.Wait => $"{C_BLUE}…{C_RESET}",
            MsgType.Input => $"{C_MAGENTA}?{C_RESET}",
            _ => $"{C_BLUE}•{C_RESET}"
        };
        Console.WriteLine($"   {prefix}  {text}");
    }

    /// <summary>
    /// Displays connection info banner showing whether the current module uses a proxy.
    /// </summary>
    /// <param name="usesProxy">True if the module is configured to use a proxy.</param>
    public static void ShowConnectionInfo(bool usesProxy)
    {
        Console.WriteLine();
        if (usesProxy)
        {
            if (GlobalState.GlobalProxy != null)
                Console.WriteLine($"   {C_YELLOW}[INFO] MODULE USES PROXY ({GlobalState.GlobalProxyAddress}){C_RESET}");
            else
                Console.WriteLine($"   {C_RED}[WARNING] MODULE USES PROXY but Proxy is DISABLED (Direct Connection){C_RESET}");
        }
        else
        {
            Console.WriteLine($"   {C_GRAY}[INFO] MODULE USES DIRECT CONNECTION (Local/Raw){C_RESET}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Prompts the user for text input with an optional default value.
    /// </summary>
    /// <param name="label">The prompt label.</param>
    /// <param name="defaultValue">Default value returned if input is empty.</param>
    /// <returns>The user's input or the default value.</returns>
    public static string Prompt(string label, string defaultValue = "")
    {
        Console.Write($"   {C_MAGENTA}➜{C_RESET}  {label} {(defaultValue != "" ? $"[{defaultValue}]" : "")}: {C_CYAN}");
        string input = Console.ReadLine();
        Console.Write(C_RESET);
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }

    /// <summary>
    /// Prompts the user for a password with masked input (asterisks).
    /// </summary>
    /// <param name="label">The prompt label.</param>
    /// <returns>The entered password string.</returns>
    public static string PromptPassword(string label)
    {
        Console.Write($"   {C_MAGENTA}➜{C_RESET}  {label}: {C_CYAN}");
        string pass = "";
        do
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                pass += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
            {
                pass = pass.Substring(0, pass.Length - 1);
                Console.Write("\b \b");
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
        } while (true);
        Console.Write(C_RESET);
        return pass;
    }

    /// <summary>
    /// Waits for the user to press any key.
    /// </summary>
    public static void Pause()
    {
        Console.WriteLine($"\n   {C_GRAY}Press any key to continue...{C_RESET}");
        Console.ReadKey(true);
    }

    /// <summary>
    /// Draws a bordered box with a title and renders the provided content action inside it.
    /// </summary>
    /// <param name="title">Box title.</param>
    /// <param name="content">Action that writes the box content.</param>
    public static void DrawBox(string title, Action content)
    {
        Console.WriteLine($"   {C_GRAY}┌──[ {C_WHITE}{title}{C_GRAY} ]─────────────────────────────────{C_RESET}");
        content();
        Console.WriteLine($"   {C_GRAY}└──────────────────────────────────────────────────{C_RESET}");
    }

    /// <summary>
    /// Draws a menu option with an optional second column.
    /// </summary>
    /// <param name="key">Left column key (e.g., "1").</param>
    /// <param name="description">Left column description.</param>
    /// <param name="key2">Right column key (optional).</param>
    /// <param name="description2">Right column description (optional).</param>
    public static void DrawMenuOption(string key, string description, string key2 = "", string description2 = "")
    {
        string left = $"[{C_CYAN}{key}{C_RESET}] {description}";
        if (string.IsNullOrEmpty(key2))
        {
            Console.WriteLine($"   {left}");
        }
        else
        {
            string right = $"[{C_CYAN}{key2}{C_RESET}] {description2}";
            Console.WriteLine($"   {left,-45} {right}");
        }
    }

    /// <summary>
    /// Displays a simple animated loading bar for a named task.
    /// </summary>
    /// <param name="taskName">Name of the task being loaded.</param>
    public static async Task LoadingBar(string taskName)
    {
        Console.Write($"   {C_BLUE}•{C_RESET}  {taskName,-30}");
        for (int i = 0; i < 10; i++)
        {
            Console.Write($"{C_CYAN}■{C_RESET}");
            await Task.Delay(20);
        }
        Console.WriteLine($" {C_GREEN}OK{C_RESET}");
    }
}