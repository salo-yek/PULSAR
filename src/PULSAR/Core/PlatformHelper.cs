// SPDX-License-Identifier: MIT
using System.Runtime.InteropServices;
using System.Security.Principal;
using PULSAR.UI;

namespace PULSAR.Core;

/// <summary>
/// Platform detection and helper utilities for cross-platform compatibility.
/// Handles ANSI console support, administrator checks, and cancellable task patterns.
/// </summary>
public static class PlatformHelper
{
    /// <summary>
    /// Enables ANSI virtual terminal processing on Windows consoles.
    /// This allows colored output via escape sequences.
    /// Non-Windows platforms typically support ANSI natively.
    /// </summary>
    public static void EnableAnsi()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var handle = Win32Native.GetStdHandle(Win32Native.StdOutputHandle);
                Win32Native.GetConsoleMode(handle, out uint mode);
                Win32Native.SetConsoleMode(handle, mode | Win32Native.EnableVirtualTerminalProcessing);
            }
            catch
            {
                // Non-critical: ANSI escape support is a convenience enhancement
            }
        }
    }

    /// <summary>
    /// Checks whether the current process is running with elevated privileges
    /// (Administrator on Windows, root on Linux).
    /// </summary>
    public static bool IsAdministrator()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        return Environment.UserName == "root";
    }

    /// <summary>
    /// Wraps an async operation with Ctrl+C cancellation support.
    /// Handles <see cref="OperationCanceledException"/> gracefully.
    /// </summary>
    /// <param name="action">The async action to execute, receiving a cancellation token.</param>
    public static async Task RunCancellable(Func<CancellationToken, Task> action)
    {
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (sender, args) =>
        {
            args.Cancel = true;
            if (!cts.IsCancellationRequested)
                cts.Cancel();
        };
        Console.CancelKeyPress += handler;
        try
        {
            await action(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected: user cancelled via Ctrl+C
        }
        catch (Exception ex)
        {
            ModernUI.Print(ex.Message, ModernUI.MsgType.Error);
            ModernUI.Pause();
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }
}