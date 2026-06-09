// SPDX-License-Identifier: MIT
using System.Net;
using PULSAR.Core;

namespace PULSAR.Networking;

/// <summary>
/// Validates targets against the global blacklist to prevent accidental attacks on protected systems.
/// Blacklist includes localhost and any user-added IPs.
/// </summary>
public static class BlacklistManager
{
    /// <summary>
    /// Checks if the specified target is allowed for operations.
    /// Returns false if the target or any resolved IP is in the blacklist.
    /// </summary>
    /// <param name="target">The target hostname or IP address.</param>
    /// <returns>True if the target is allowed; false if blacklisted.</returns>
    public static bool IsTargetAllowed(string target)
    {
        if (GlobalState.TargetBlacklist.Contains(target))
            return false;

        try
        {
            var ips = Dns.GetHostAddressesAsync(target);
            ips.Wait(2000);
            if (ips.Result is not null)
            {
                foreach (var ip in ips.Result)
                {
                    if (GlobalState.TargetBlacklist.Contains(ip.ToString()))
                        return false;
                }
            }
        }
        catch
        {
            // DNS resolution failed - allow raw target string through
        }
        return true;
    }
}