# 🚀 PULSAR Network Toolkit (v3.4)

![Status](https://img.shields.io/badge/Status-Freeware-green)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Platform](https://img.shields.io/badge/Platform-Linux-orange)
![Version](https://img.shields.io/badge/Version-3.4%20(Deep%20Scan%20%26%20Post--Exp)-purple)
![Security](https://img.shields.io/badge/Purpose-Red%20Teaming-red)
![Anonymity](https://img.shields.io/badge/Feature-Proxy%20Deep%20Scan-cyan)

**PULSAR v3.4** represents a massive leap forward in anonymity and offensive capabilities. This release introduces the **Proxy Database Deep Scan** engine for finding high-speed tunneling nodes and a brand new **Post-Exploitation Framework** for Windows environments.

> [!WARNING]
> **DISCLAIMER / LEGAL NOTICE**
> 
> This program was created **solely for educational purposes** and for **authorized security testing** of your own infrastructure.
> 
> The author assumes no liability for any damage caused by the misuse of this software. Using offensive modules (e.g., Network Stresser, Credential Dumping) on systems without the owner's explicit permission is illegal. By downloading this software, you agree to the terms of the EULA.

## 🚀 New Features in v3.4

### 🕵️‍♂️ Proxy Database Deep Scan (Module P)
A completely rewritten networking core designed for maximum anonymity and speed.
*   **Global Node Search:** Scans PULSAR's extensive database to identify the fastest available **HTTP, HTTPS, and SOCKS5** tunneling nodes.
*   **Latency-Based Selection:** Automatically tests thousands of endpoints and selects the one with the lowest ping for your session.
*   **Smart Caching:** Saves the best performing nodes to an encrypted local cache for instant connection on the next startup.
*   **Auto-Rotation:** Configurable options to rotate IP addresses on every request or at set intervals.

### 🏴‍☠️ Post-Exploitation Framework (Windows)
A new dedicated menu for advanced operations on compromised systems (Requires Admin privileges).
*   **Privilege Escalation Scan (Mod 21):** Audits the system for common misconfigurations (Unquoted Service Paths, AlwaysInstallElevated registry keys) that allow standard users to gain System access.
*   **Credential Dumping (Mod 22):** Exports **SAM** and **SYSTEM** registry hives for offline NTLM hash extraction.
*   **System Cleanup (Mod 23):** "Anti-Forensics" module that wipes PULSAR configuration traces and clears Windows Event Logs (Application, System, Security) to cover tracks.

### ☁️ Cloud-Synced Intelligence
*   **Live Definitions:** The Directory Scanner and Subdomain Enumerator now sync with the cloud database at startup. This ensures your wordlists are always up-to-date with the latest known vulnerabilities and admin panel paths without needing to update the software manually.

### 🔐 Enhanced Security
*   **AES-256 Encryption:** All local configuration data (session tokens, proxy cache) is now encrypted using a unique local key for improved security.
*   **Secure Connection:** Updated TLS protocols for all remote communications.

---

## 🛠️ Full Feature List

### 🛡️ Offensive Operations & Post-Exploitation
*   **Network Stresser (Layer 4/7):** Advanced load testing with support for UDP, TCP, HTTP, HTTP HEAD, Slowloris, and Amplification vectors (NTP/DNS).
*   **Privilege Escalation Scanner:** Detecting system vulnerabilities on local Windows machines.
*   **Credential Dumper:** extracting SAM/SYSTEM hives.
*   **Log Wiper:** Clearing Windows Event Logs.
*   **WiFi Deauth Attack:** Disconnecting devices from Wi-Fi networks (Linux Root required).

### 🌐 Reconnaissance & OSINT
*   **Proxy Deep Scan:** Finding and validating high-speed anonymous proxies.
*   **Subdomain Enumerator:** Cloud-synced discovery of subdomains (e.g., dev.target.com).
*   **Directory Scanner:** Cloud-synced scanning for hidden web resources.
*   **Tech Detector:** Identifying CMS (WordPress, Joomla) and server frameworks.
*   **Passive Traffic Monitor:** Real-time packet sniffing (Windows Admin/Linux Root).
*   **Whois & GeoIP:** Domain ownership and physical location data.

### 🔑 Authentication & Cracking
*   **Web Brute-Force:** Automated login testing via HTTP POST.
*   **Hash Cracker:** Dictionary attack tool for MD5, SHA1, SHA256, SHA512.
*   **Hash Generator:** Creating hashes for text verification.

### 🔧 System Integration
*   **Cluster Mode (v2):** Coordinate attacks using multiple devices (Master/Slave).
*   **Startup Manager:** Configure PULSAR to launch on system boot.
*   **PATH Integration:** Run the tool from any command line window.

---

## 📥 Download & Installation

The software is distributed as a **portable** executable (no installation required).

1.  Go to the **[Releases](../../releases)** tab.
2.  Download the file matching your OS.
3.  Run the executable (`PULSARx64.exe` or `PULSARlinux-x64`).

### 🛡️ Antivirus Note
Security software (Windows Defender, etc.) may flag this tool as *HackTool* or *PUP* due to the inclusion of stress testing, credential dumping, and proxy scanning modules. This is a **False Positive** typical for security auditing software. You may need to whitelist the application folder.

### 🔑 Privileges
To use the **Post-Exploitation** modules and **Raw Socket** features (Sniffer, Deauth, UDP Flood), you must run the application with **Administrator** (Windows) or **Root** (Linux) privileges.

---

## 📜 License

**PULSAR** is **Freeware**.
You may use it for personal education and authorized testing.

❌ **RESTRICTIONS:**
*   Reverse Engineering or decompiling the code is prohibited.
*   Use for illegal activities (DDoS, unauthorized hacking) is strictly prohibited.
*   Redistribution of modified binaries is not allowed.

See the [LICENSE](LICENSE) file for details.
