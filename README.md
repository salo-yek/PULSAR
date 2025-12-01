# PULSAR Network Toolkit (v3.0)

![Status](https://img.shields.io/badge/Status-Freeware-green)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Security](https://img.shields.io/badge/Purpose-Educational-red)

**PULSAR** is an advanced console-based network toolkit and pentesting suite for Windows. The application integrates 17 diagnostic modules, OSINT tools, and security auditing functions (Red Teaming) into a single interface.

> [!WARNING]
> **DISCLAIMER / LEGAL NOTICE**
> 
> This program was created **solely for educational purposes** and for **authorized security testing** of your own infrastructure.
> 
> The author assumes no liability for any damage caused by the misuse of this software. Using offensive modules (e.g., Network Stresser) on servers without the owner's explicit permission is illegal. By downloading this software, you agree to the terms of the EULA.

## 🚀 Features (v2.9)

The toolkit is divided into three main categories:

### 🛡️ Network & Diagnostics
*   **Network Stresser:** Network load testing (HTTP/HTTPS/UDP) with multi-threading support.
*   **Advanced Network Scanner:** LAN scanning (ARP), MAC Vendor detection, and OS Fingerprinting based on TTL.
*   **Passive Traffic Monitor:** Real-time packet sniffer (detects DNS queries and HTTP traffic).
*   **TCP Listener:** *[NEW]* Simple port listener (Netcat alternative) for testing connectivity.
*   **WiFi Scanner:** Scans wireless networks and evaluates signal strength/security.
*   **Traceroute:** Packet route tracing with latency visualization.

### 🌐 Web & OSINT
*   **Web Crawler:** *[NEW]* Extracts internal and external links from target websites.
*   **Subdomain Enumerator:** Brute-force discovery of subdomains (e.g., admin.site.com, dev.site.com).
*   **Web Directory Scanner:** Scans for hidden resources (admin panels, backups, config files).
*   **Tech & CMS Detector:** Identifies server technologies and CMS (WordPress, Joomla, Laravel, etc.).
*   **SSL Inspector:** Analyzes SSL/TLS certificates and the chain of trust.
*   **HTTP Header Analyzer:** Audits security headers (XSS-Protection, CSP, HSTS).
*   **Whois & GeoIP:** Domain registration data and physical IP geolocation.

### 🔧 Utilities
*   **Target Port Scanner:** Deep TCP port scanning for a specific host.
*   **IP Calculator:** Subnet calculator (CIDR, Broadcast, Host Range).
*   **Password & Hash Tool:** Hash generation (MD5, SHA256) and password strength analysis.

---

## 📥 Download & Installation

The software is distributed as a portable executable (`.exe`) and does not require installation.

1.  Go to the **[Releases](../../releases)** tab on the right side of this page.
2.  Download the latest `.zip` archive or `.exe` file.
3.  Run `PULSAR.exe`.

> **Requirements:** Windows 10/11/Linux.

### 🛡️ Important Note Regarding Antivirus
Antivirus software (e.g., Windows Defender) may flag this application as a threat (e.g., *HackTool:Win32/Pulsar*). This is a **False Positive**—standard behavior for tools that contain network scanners and stress testing modules. You may need to add an exception to run it.

### 🔑 Administrator Privileges
To use advanced features such as:
*   Sniffer (Raw Sockets),
*   Network Stresser (UDP) (Recommended),
*   OS Detection (Ping/TTL),

...you must run the program as **Administrator** (Right-click -> *Run as administrator*).

---

## 📜 License

**PULSAR** is **Freeware (Closed Source)**.
You may use it for free for personal, educational, and professional purposes.

❌ **RESTRICTIONS:**
*   Reverse Engineering, decompilation, and modification of the code are strictly prohibited.
*   Selling or sub-licensing the software is prohibited.
*   Using this software for illegal activities is prohibited.

See the [LICENSE](LICENSE) file for the full End User License Agreement (EULA).
