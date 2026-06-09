// SPDX-License-Identifier: MIT
namespace PULSAR.Core;

/// <summary>
/// Application-wide constants including version info and proxy source URLs.
/// </summary>
public static class Constants
{
    /// <summary>Application version number.</summary>
    public const double VersionNum = 3.5;

    /// <summary>Application version string.</summary>
    public const string VersionStr = "3.5";

    /// <summary>GitHub repository owner for auto-update/path references.</summary>
    public const string GitHubRepoOwner = "PULSAR-Project";

    /// <summary>GitHub repository name.</summary>
    public const string GitHubRepoName = "PULSAR";

    /// <summary>RockYou wordlist archive URL (GitHub mirror).</summary>
    public const string RockYouGzUrl = "https://github.com/zacheller/rockyou/raw/master/rockyou.txt.tar.gz";

    /// <summary>HTTP proxy list source URLs.</summary>
    public static readonly string[] ProxySources = {
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all",
        "https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/http.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/http.txt",
        "https://raw.githubusercontent.com/prxchk/proxy-list/main/http.txt",
        "https://raw.githubusercontent.com/sunny9577/proxy-scraper/master/proxies.txt",
        "https://raw.githubusercontent.com/roosterkid/openproxylist/main/HTTPS_RAW.txt"
    };

    /// <summary>SOCKS5 proxy list source URLs.</summary>
    public static readonly string[] Socks5Sources = {
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=10000&country=all",
        "https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/socks5.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/hookzof/socks5_list/master/proxy.txt"
    };

    /// <summary>HTTPS proxy list source URLs.</summary>
    public static readonly string[] HttpsSources = {
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=https&timeout=10000&country=all&ssl=all&anonymity=all",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/https.txt",
        "https://raw.githubusercontent.com/Zaeem20/FREE_PROXIES_LIST/master/https.txt"
    };

    /// <summary>Default list of directories for web directory enumeration.</summary>
    public static readonly List<string> DefaultDirectories = new()
    {
        "admin", "login", "wp-admin", "dashboard", "config", "backup", "db",
        "shell", "user", "upload", "ftp", "mail", "webmail", "phpmyadmin",
        "robots.txt", "sitemap.xml", ".env", ".git"
    };

    /// <summary>Default list of subdomains for subdomain enumeration.</summary>
    public static readonly List<string> DefaultSubdomains = new()
    {
        "www", "mail", "ftp", "cpanel", "test"
    };
}