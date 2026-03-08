# HardwareExporterWindows

[English](README.md) | [简体中文](docs/README.zh-CN.md) | [日本語](docs/README.ja.md)

A Prometheus exporter for Windows hardware metrics using LibreHardwareMonitor.

## Why

> windows_exporter's thermal zone data is not accurate

This exporter provides accurate hardware monitoring data by using LibreHardwareMonitor library directly.

## Features

- ✅ Accurate CPU, GPU, Memory, Motherboard, Network, and Storage metrics
- ✅ Runs as Windows Service
- ✅ Configurable hardware monitoring (enable/disable specific components)
- ✅ Prometheus-compatible metrics format
- ✅ Low resource usage with singleton pattern
- ✅ Structured logging

## Installation

### Prerequisites

- Windows 10/11 or Windows Server 2016+
- .NET 10.0 Runtime (or SDK for development)
- Administrator privileges (required for hardware access)

### Quick Start

**Option 1: MSI Installer (Recommended)**

1. Download `HardwareExporterWindows-win-x64.msi` from the [latest release](https://github.com/naughtyGitCat/HardwareExporterWindows/releases)
2. Double-click the MSI file to install
3. The installer will automatically:
   - Copy files to `C:\Program Files\HardwareExporter`
   - Install and start the Windows Service
   - Configure firewall rules

**Option 2: Manual Installation with PowerShell Script**

1. Download `HardwareExporterWindows-win-x64.zip` from the [latest release](https://github.com/naughtyGitCat/HardwareExporterWindows/releases)
2. Extract to `C:\Program Files\HardwareExporter`
3. Run PowerShell as Administrator
4. Execute the installation script:

```powershell
cd "C:\Program Files\HardwareExporter"
.\install.ps1
```

The script will:
- Copy files to the installation directory
- Create a Windows Firewall rule
- Register and start the Windows Service

### Manual Installation

```powershell
# Register as Windows Service
New-Service -Name "HardwareExporter" `
    -BinaryPathName "C:\Program Files\HardwareExporter\HardwareExporterWindows.exe" `
    -StartupType "Automatic" `
    -Description "Hardware Exporter Service"

# Start the service
Start-Service -Name "HardwareExporter"
```

## Configuration

Edit `appsettings.json` to customize the exporter:

```json
{
  "HardwareMonitor": {
    "EnableCpu": true,
    "EnableGpu": true,
    "EnableMemory": true,
    "EnableMotherboard": true,
    "EnableController": true,
    "EnableNetwork": true,
    "EnableStorage": true,
    "ScrapeIntervalSeconds": 15
  },
  "Urls": "http://0.0.0.0:9888"
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableCpu` | bool | true | Monitor CPU metrics |
| `EnableGpu` | bool | true | Monitor GPU metrics |
| `EnableMemory` | bool | true | Monitor Memory metrics |
| `EnableMotherboard` | bool | true | Monitor Motherboard metrics |
| `EnableController` | bool | true | Monitor Controller metrics |
| `EnableNetwork` | bool | true | Monitor Network metrics |
| `EnableStorage` | bool | true | Monitor Storage metrics |
| `ScrapeIntervalSeconds` | int | 15 | Update interval (not used yet) |

## Prometheus Configuration

Add this to your `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'windows-hardware'
    static_configs:
      - targets:
          - '192.168.1.100:9888'  # Your Windows machine IP
          - '192.168.1.101:9888'
```

## Available Metrics

The exporter provides two types of metrics:

1. **Hardware Metrics** - From LibreHardwareMonitor
2. **.NET Runtime Metrics** - From prometheus-net (GC, threads, memory, etc.)

All metrics are available at the `/metrics` endpoint.

### Hardware Metrics Format

```
hardware_{type}_{sensor_type}_{sensor_name}{labels} value
```

### Example Metrics

```
# CPU Temperature
hardware_cpu_temperature_core{name="AMD Ryzen 9 5900X", core="0"} 45.0

# GPU Usage
hardware_gpu_load_core{name="NVIDIA GeForce RTX 3080", vendor="nvidia"} 75.5

# Memory Usage
hardware_memory_load_memory{name="Generic Memory"} 45.2

# Fan Speed
hardware_motherboard_fan_fan{name="ASUS ROG STRIX B550-F", fan="1"} 1200
```

### Metric Types

- `temperature` - Temperature in Celsius
- `load` - Load percentage (0-100)
- `clock` - Clock speed in MHz
- `power` - Power consumption in Watts
- `fan` - Fan speed in RPM
- `voltage` - Voltage in Volts
- `data` - Data rate in GB/s
- `throughput` - Throughput in MB/s

## Troubleshooting

### Service won't start

1. Check Windows Event Viewer for errors
2. Ensure you have Administrator privileges
3. Verify .NET 10.0 Runtime is installed
4. Check if port 9888 is already in use

### No metrics appearing

1. Check if the service is running: `Get-Service HardwareExporter`
2. Test the endpoint: `curl http://localhost:9888/metrics`
3. Check logs in Event Viewer under "Application"

### Firewall issues

The installation script creates a firewall rule automatically. If needed, create it manually:

```powershell
New-NetFirewallRule -DisplayName "HardwareExporter" `
    -Direction Inbound `
    -Program "C:\Program Files\HardwareExporter\HardwareExporterWindows.exe" `
    -Action Allow
```

## Development

### Building from Source

```bash
git clone https://github.com/naughtyGitCat/HardwareExporterWindows.git
cd HardwareExporterWindows
dotnet build
```

### Running in Development

```bash
cd src/HardwareExporterWindows
dotnet run
```

Access metrics at: http://localhost:9888/metrics

### Running Tests

```bash
dotnet test
```

## Credits

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - Hardware monitoring library
- [prometheus-net](https://github.com/prometheus-net/prometheus-net) - Prometheus client library

## License

MIT License - see [LICENSE](LICENSE) file for details.

This project uses the following open source libraries:
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL 2.0)
- [prometheus-net](https://github.com/prometheus-net/prometheus-net) (MIT)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
