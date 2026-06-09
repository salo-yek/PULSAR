// SPDX-License-Identifier: MIT
using System.Runtime.InteropServices;

namespace PULSAR.Core;

/// <summary>
/// Platform Invoke (P/Invoke) declarations for Windows native APIs.
/// Used for console mode, ARP scanning, and input simulation.
/// All calls are guarded by runtime platform checks at the call site.
/// </summary>
public static class Win32Native
{
    // --- Console ---
    public const int StdOutputHandle = -11;
    public const uint EnableVirtualTerminalProcessing = 4;

    /// <summary>Gets the standard output handle.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>Gets the current console mode flags.</summary>
    [DllImport("kernel32.dll")]
    public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    /// <summary>Sets console mode flags.</summary>
    [DllImport("kernel32.dll")]
    public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    // --- Networking ---
    /// <summary>Sends an ARP request to resolve an IP address to a MAC address.</summary>
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    public static extern int SendARP(uint destinationIp, uint sourceIp, byte[] macAddress, ref uint macAddressLength);

    // --- Input Simulation ---
    /// <summary>Simulates keyboard/mouse input.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>Sets the cursor position.</summary>
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    /// <summary>Gets the cursor position.</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    // --- Structures ---

    /// <summary>Windows POINT structure.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        /// <summary>X coordinate.</summary>
        public int X;
        /// <summary>Y coordinate.</summary>
        public int Y;
    }

    /// <summary>Windows INPUT structure for SendInput.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        /// <summary>Input type.</summary>
        public uint type;
        /// <summary>Union of input event data.</summary>
        public InputUnion u;
    }

    /// <summary>Union of mouse, keyboard, and hardware input structures.</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        /// <summary>Mouse input data.</summary>
        [FieldOffset(0)] public MOUSEINPUT mi;
        /// <summary>Keyboard input data.</summary>
        [FieldOffset(0)] public KEYBDINPUT ki;
        /// <summary>Hardware input data.</summary>
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    /// <summary>Keyboard input structure.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        /// <summary>Virtual key code.</summary>
        public ushort wVk;
        /// <summary>Hardware scan code.</summary>
        public ushort wScan;
        /// <summary>Flags.</summary>
        public uint dwFlags;
        /// <summary>Time stamp.</summary>
        public uint time;
        /// <summary>Extra info.</summary>
        public UIntPtr dwExtraInfo;
    }

    /// <summary>Mouse input structure.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        /// <summary>X delta or position.</summary>
        public int dx;
        /// <summary>Y delta or position.</summary>
        public int dy;
        /// <summary>Mouse data (e.g., wheel delta).</summary>
        public uint mouseData;
        /// <summary>Flags.</summary>
        public uint dwFlags;
        /// <summary>Time stamp.</summary>
        public uint time;
        /// <summary>Extra info.</summary>
        public UIntPtr dwExtraInfo;
    }

    /// <summary>Hardware input structure.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        /// <summary>Message.</summary>
        public uint uMsg;
        /// <summary>Low-order parameter.</summary>
        public ushort wParamL;
        /// <summary>High-order parameter.</summary>
        public ushort wParamH;
    }
}