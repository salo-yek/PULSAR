# PULSAR v3.5 — Open-Source Network Security Toolkit

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-lightgrey)]()

> **For educational and authorized security testing purposes only.**  
> Unauthorized use against targets you do not own or have explicit written permission to test is illegal.

## Overview

PULSAR is a modular, cross-platform network security toolkit written in C#. It provides a comprehensive set of capabilities for:

- **Network Reconnaissance** — Port scanning, traceroute, WHOIS, GeoIP lookup, WiFi scanning, subnet calculation
- **Web Application Testing** — Directory enumeration, subdomain discovery, header analysis, SSL inspection, technology detection, web crawling
- **Performance Testing / Stress Testing** — Multi-vector network stress testing (Layer 4/7) with configurable parameters
- **Proxy Management** — Automatic proxy discovery, testing, rotation, and deep scanning from multiple public sources
- **Distributed Operations** — TCP-based cluster mode for coordinated multi-node testing
- **Post-Exploitation (Windows)** — Privilege escalation auditing, credential dump simulation, system cleanup
- **Cryptographic Utilities** — Hash generation, dictionary-based hash cracking (MD5/SHA1/SHA256/SHA512)
- **WiFi Auditing (Linux)** — Network scanning and deauthentication attack testing (requires aircrack-ng)

## Legal Notice

This software is provided for **educational purposes and authorized security testing only**. Users must:

1. Only test systems they own or have explicit written permission to test
2. Comply with all applicable local, state, and federal laws
3. Not use PULSAR for any illegal or unauthorized purpose

The developers assume **no liability** for misuse of this software.

## Requirements

### Runtime
- [.NET 8.0 SDK or later](https://dotnet.microsoft.com/download) (target: `net10.0`, compatible with .NET 6+)
- Windows 10/11, Windows Server, or Linux (Ubuntu/Debian/Fedora recommended)

### Optional Dependencies
- **WiFi Features (Linux):** `aircrack-ng` suite (`sudo apt install aircrack-ng`)
- **Packet Capture (Windows):** Administrator privileges
- **Packet Capture (Linux):** `tcpdump` (`sudo apt install tcpdump`) + root privileges

## Quick Start

```bash
# Clone the repository
git clone https://github.com/PULSAR-Project/PULSAR.git
cd PULSAR

# Build and run
dotnet restore
dotnet build
dotnet run --project src/PULSAR
```

### One-liner (after clone)

```bash
dotnet run --project src/PULSAR
```

## Features

### Main Menu
| Option | Module | Description |
|--------|--------|-------------|
| 1 | DoS / Stresser | Multi-vector network stress testing (L4/L7) |
| 2 | Traffic Monitor | Packet sniffer (requires admin/root) |
| 3 | Local Network Scanner | ARP/ICMP subnet discovery |
| 4 | GeoIP Lookup | IP geolocation via ip-api.com |
| 5 | Traceroute | ICMP-based route tracing |
| 6 | WHOIS Lookup | Domain WHOIS via hackertarget.com |
| 7 | WiFi Scanner | Network enumeration (netsh/nmcli) |
| 8 | Subnet Calculator | CIDR/IP calculations |
| 9 | Port Scanner | Quick TCP port scan |
| 10 | Directory Scanner | Web path enumeration |
| 11 | Subdomain Enumerator | DNS/HTTP subdomain discovery |
| 12 | Header Analyzer | HTTP response header inspection |
| 13 | Hash Gen / Crypto | MD5/SHA256 hash generation |
| 14 | SSL Inspector | Certificate chain inspection |
| 15 | Tech Detector | CMS/framework fingerprinting |
| 16 | Web Crawler | Link extraction |
| 17 | Web Brute-Force | Form-based credential testing |
| 18 | URL Traffic Gen | HTTP request generator |
| 19 | Hash Cracker | Dictionary-based hash cracking |
| 20 | WiFi Deauth (Linux) | Deauthentication attack testing |
| 21 | PrivEsc Scan (Win) | Privilege escalation vector scan |
| 22 | Dump Creds (Win) | SAM/SYSTEM hive export |
| 23 | System Cleanup | Log/track removal |
| S | System Settings | Proxy, PATH, startup configuration |
| C | Cluster Mode | Distributed testing coordination |
| P | Proxy Database Scan | Deep proxy discovery and benchmarking |

## Command-Line Arguments

```bash
pulsar --startup-mode=menu          # Start in normal menu mode
pulsar --startup-mode=cluster_master # Start as cluster master
pulsar --startup-mode=cluster_slave  # Start as cluster slave
```

## Build Scripts

### Windows
```powershell
.\build.ps1
```

### Linux
```bash
chmod +x build.sh
./build.sh
```

## Project Structure

```
PULSAR/
├── src/PULSAR/          # Source code
│   ├── Core/            # Constants, global state, models, P/Invoke
│   ├── UI/              # Console UI framework
│   ├── Configuration/   # Settings persistence, PATH, startup
│   ├── Networking/      # Proxy management, network utilities
│   ├── Attacks/         # Stress testing vectors
│   ├── Cluster/         # Distributed coordination
│   ├── PostExploitation/# Windows post-exploitation modules
│   ├── Recon/           # Reconnaissance and web modules
│   └── Settings/        # Configuration menus
├── docs/                # Documentation (future)
├── tests/               # Unit tests (future)
├── LICENSE              # MIT License
├── README.md            # This file
└── build.ps1 / build.sh # Build scripts
```

## Configuration

PULSAR stores configuration in JSON files alongside the executable:
- `pulsar_proxy_cache.dat` — Cached proxy node information
- `pulsar_path_settings.json` — PATH integration settings
- `pulsar_startup_settings.json` — Windows startup configuration

## Proxy Sources

PULSAR aggregates proxies from publicly available lists for educational testing purposes. All requests can be routed through SOCKS5, HTTP, or HTTPS proxies automatically.

## Contributing

Contributions are welcome! Please read the following guidelines:

1. **No malicious features** — Modules that enable unauthorized access or harm will not be accepted
2. **Code style** — Follow .NET conventions (PascalCase for public members, camelCase for locals)
3. **Documentation** — Add XML documentation for all public APIs
4. **Testing** — Ensure existing functionality is preserved

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## Disclaimer

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND. THE AUTHORS SHALL NOT BE HELD LIABLE FOR ANY DAMAGES ARISING FROM THE USE OF THIS SOFTWARE. USE AT YOUR OWN RISK.

---

*PULSAR — Network Security Toolkit | For Educational Use Only*