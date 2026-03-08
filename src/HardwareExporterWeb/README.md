# HardwareExporterWeb

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

**HardwareExporterWeb** is an optional companion service for [HardwareExporterWindows](../README.md) that provides automatic service discovery for Prometheus.

## What It Does

HardwareExporterWeb is **not a metrics collector** - it's a service discovery tool that:

- 🔍 Automatically scans your local network to discover Windows machines
- 📡 Provides Prometheus HTTP Service Discovery endpoints
- 🌐 Offers a web interface to manage discovered hosts
- 🌍 Supports multiple languages (English, Chinese, Japanese)

## Why Use It?

Instead of manually configuring each monitoring target in `prometheus.yml`:

```yaml
# Manual configuration (tedious)
scrape_configs:
  - job_name: 'windows-hardware'
    static_configs:
      - targets:
          - '192.168.1.100:9888'
          - '192.168.1.101:9888'
          - '192.168.1.102:9888'
          # ... add more manually
```

Use automatic service discovery:

```yaml
# Automatic discovery (dynamic)
scrape_configs:
  - job_name: 'windows-hardware-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/HardwareExporter'
        refresh_interval: 60s
```

## Installation

### Prerequisites

- .NET 10.0 Runtime
- Network access to scan subnet

### Quick Start

1. Download `HardwareExporterWeb-win-x64.zip` from the [latest release](https://github.com/naughtyGitCat/HardwareExporterWindows/releases)
2. Extract to your desired location
3. Edit `appsettings.json` (optional)
4. Run `HardwareExporterWeb.exe`

The web interface will be available at `http://localhost:80` by default.

## Configuration

Edit `appsettings.json`:

```json
{
  "NetworkScan": {
    "SubnetFilter": "",           // Empty = scan all local subnets
    "SubnetMask": "255.255.255.0" // Subnet mask for scanning
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      }
    }
  }
}
```

### Network Scan Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SubnetFilter` | string | "" | Optional subnet filter (e.g., "10.100.100"). Empty = scan all local subnets |
| `SubnetMask` | string | "255.255.255.0" | Subnet mask for network scanning |

### Examples

**Scan all local subnets:**
```json
"SubnetFilter": ""
```

**Scan specific subnet only:**
```json
"SubnetFilter": "10.100.100"
```

**Use different subnet mask:**
```json
"SubnetMask": "255.255.0.0"
```

## API Endpoints

### Service Discovery

- **GET** `/api/ServiceDiscovery/HardwareExporter`
  - Returns list of discovered HardwareExporter instances
  - Format: Prometheus HTTP SD compatible JSON

- **GET** `/api/ServiceDiscovery/WindowsExporter`
  - Returns list of discovered windows_exporter instances
  - Format: Prometheus HTTP SD compatible JSON

### Response Format

```json
[
  {
    "targets": ["192.168.1.100:9888"],
    "labels": {
      "ip": "192.168.1.100",
      "hostname": "DESKTOP-ABC123"
    }
  }
]
```

## Prometheus Configuration

### HTTP Service Discovery

```yaml
scrape_configs:
  - job_name: 'hardware-exporter-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/HardwareExporter'
        refresh_interval: 60s
    
  - job_name: 'windows-exporter-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/WindowsExporter'
        refresh_interval: 60s
```

## Web Interface

The web interface provides:

- 📊 View discovered hosts
- ➕ Manually add hosts
- ✏️ Edit host information
- 🗑️ Remove hosts
- 🌐 Multi-language support (EN/CN/JP)

Access the web interface at `http://localhost:80` (or your configured port).

### Language Support

The web interface supports three languages:
- 🇺🇸 English
- 🇨🇳 简体中文 (Simplified Chinese)
- 🇯🇵 日本語 (Japanese)

Use the language selector in the top-right corner to switch languages.

## Architecture

```
┌─────────────────────────────────────┐
│   Prometheus Server                 │
│   (pulls from HTTP SD endpoints)    │
└──────────────┬──────────────────────┘
               │
               │ HTTP SD API
               │
┌──────────────▼──────────────────────┐
│   HardwareExporterWeb               │
│   - Network Scanner                 │
│   - Host Manager                    │
│   - Web UI                          │
└──────────────┬──────────────────────┘
               │
               │ ARP Scan
               │
┌──────────────▼──────────────────────┐
│   Local Network                     │
│   - Windows machines with           │
│     HardwareExporter                │
│   - Windows machines with           │
│     windows_exporter                │
└─────────────────────────────────────┘
```

## Development

### Building from Source

```bash
cd src/HardwareExporterWeb
dotnet build
```

### Running in Development

```bash
cd src/HardwareExporterWeb
dotnet run
```

### Running Tests

```bash
cd test/HardwareExporterWeb.Tests
dotnet test
```

## Troubleshooting

### Service not discovering hosts

1. Check network connectivity
2. Verify subnet configuration in `appsettings.json`
3. Ensure target machines are running HardwareExporter or windows_exporter
4. Check firewall rules allow network scanning

### Web interface not accessible

1. Check if port 80 is already in use
2. Try changing the port in `appsettings.json`
3. Verify firewall allows incoming connections
4. Check application logs for errors

### Prometheus not picking up targets

1. Verify the HTTP SD URL is correct
2. Check Prometheus logs for SD errors
3. Test the API endpoint manually: `curl http://your-server/api/ServiceDiscovery/HardwareExporter`
4. Ensure `refresh_interval` is set appropriately

## License

MIT License - see [LICENSE](../LICENSE) file for details.

## Related Projects

- [HardwareExporterWindows](../README.md) - The main hardware metrics exporter
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - Hardware monitoring library
- [Prometheus](https://prometheus.io/) - Monitoring system and time series database

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
