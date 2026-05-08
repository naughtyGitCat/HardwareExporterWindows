# HardwareExporterWindows

[![Build Status](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml/badge.svg)](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml)
[![Release](https://img.shields.io/github/v/release/naughtyGitCat/HardwareExporterWindows)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/naughtyGitCat/HardwareExporterWindows/total)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases)
[![License](https://img.shields.io/github/license/naughtyGitCat/HardwareExporterWindows)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D6)](https://www.microsoft.com/windows)
[![Stars](https://img.shields.io/github/stars/naughtyGitCat/HardwareExporterWindows?style=social)](https://github.com/naughtyGitCat/HardwareExporterWindows/stargazers)

[English](README.md) | [简体中文](docs/README.zh-CN.md) | [日本語](docs/README.ja.md)

A Prometheus exporter for Windows hardware metrics using LibreHardwareMonitor.

## Grafana Dashboard

![Grafana Dashboard](docs/images/grafana-dashboard.png)

A ready-to-use Grafana dashboard is included at [`docs/grafana-dashboard.json`](docs/grafana-dashboard.json). Import it via Grafana UI (Dashboards > Import > Upload JSON file).

**Dashboard sections:** CPU Overview, CPU Temperature, CPU Power & Voltage, Fans, Memory, GPU (Temperature / Load / VRAM / Power / Clock / PCIe), Disk (Temperature / Space / Health / IO / Throughput), Network (Utilization / Throughput / Data Transferred).

## SMART (S.M.A.R.T. disk attributes)

LibreHardwareMonitor cannot read SMART data from disks attached behind SAS HBAs (LSI/Avago, Adaptec, etc.) because those disks appear as SCSI devices and require SAT (SCSI/ATA Translation) pass-through that the library does not implement. To cover that gap, this exporter bundles [`smartctl`](https://www.smartmontools.org/) and emits a parallel set of metrics under the `hardware_storage_smart_*` prefix.

Bundled binaries live in `<install-dir>\smartctl\` (≈1.4 MB total). They are invoked as a separate process; smartmontools is GPLv2+ and is included as a separate, unmodified executable. See `smartctl\COPYING.txt` and `smartctl\README.txt` for full attribution.

To use a different `smartctl.exe` (e.g. a newer version), set `SmartMonitor:SmartctlPath` in `appsettings.json`. To disable the SMART collector entirely, set `SmartMonitor:Enable` to `false`.

### SMART metric examples

```
# Drive temperature, including SAS HBA-attached SATA disks
hardware_storage_smart_temperature_celsius{device="/dev/sda", model="WDC  WUH721816ALE6L4", serial="3HGHU16P", protocol="ata", firmware="PCGNW232"} 34

# Overall self-assessment (1 = passed, 0 = failed)
hardware_storage_smart_health_passed{device="/dev/sda", ...} 1

# ATA SMART attribute table (per-id)
hardware_storage_smart_ata_attribute_raw{device="/dev/sda", ..., id="5", name="Reallocated_Sector_Ct"} 0
hardware_storage_smart_ata_attribute_raw{device="/dev/sda", ..., id="9", name="Power_On_Hours"} 31120

# NVMe-specific
hardware_storage_smart_nvme_percentage_used{device="/dev/sdi", model="SAMSUNG MZWLL3T2HAJQ-00005", ...} 6
hardware_storage_smart_nvme_media_errors{device="/dev/sdi", ...} 0
```

### SMART configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SmartMonitor:Enable` | bool | true | Enable the SMART collector |
| `SmartMonitor:SmartctlPath` | string | `""` | Override smartctl.exe path (empty = bundled) |
| `SmartMonitor:RefreshIntervalSeconds` | int | 60 | How often to re-poll smartctl. Refresh is asynchronous; `/metrics` always serves the most recent cached snapshot, so Prometheus scrape latency is unaffected. Note: reading SMART can wake idle HDDs, so don't set this too aggressively. |
| `SmartMonitor:InvocationTimeoutSeconds` | int | 15 | Per-disk timeout for a single smartctl call |
| `SmartMonitor:DeviceExcludePatterns` | string[] | `[]` | Glob patterns matched against `/dev/sdN` names; matching devices are skipped |

## Caching and refresh model

Both collectors run their own background refresh loop and serve `/metrics` from an in-memory cache. The Prometheus scrape never triggers a hardware read directly. There are two independent caches:

| Cache | Config option | Default | Lower bound |
|---|---|---|---|
| LibreHardwareMonitor (CPU / GPU / memory / motherboard / network / native SATA SMART / NVMe) | `HardwareMonitor:ScrapeIntervalSeconds` | **15s** | 1s |
| smartctl (full SMART table for every disk, incl. those behind SAS HBA) | `SmartMonitor:RefreshIntervalSeconds` | **60s** | 15s |

Both fire one eager refresh at service start, so the first scrape after launch already has data — no warmup gap.

**Why two different defaults**

- `HardwareMonitor` covers fast-changing telemetry (CPU temperature, GPU load, fan RPM). 15s matches the default Prometheus scrape interval, so client-visible staleness stays imperceptible.
- `SmartMonitor` reads slow-changing health attributes (temperature drifts in minutes, reallocated-sectors in days). More importantly, every smartctl invocation against an idle HDD spins it back up — running it on the scrape path would prevent disks from ever sleeping. 60s is the floor where that's actually tolerable.

**Effective staleness**

What a Prometheus client sees is bounded by `cache_interval + scrape_interval`. With defaults:

- LibreHardwareMonitor metrics: ≤ 30 s old
- SMART metrics: ≤ 75 s old

**When to retune**

- Want a 1Hz dashboard: set `HardwareMonitor:ScrapeIntervalSeconds=1`. Don't touch `SmartMonitor` — 15s is already the lower bound and it'd thrash idle drives.
- Have AHCI-attached SATA HDDs you want to actually spin down: bump `HardwareMonitor:ScrapeIntervalSeconds` to ≥ 60s. LHM's `Update()` issues `IOCTL_ATA_PASS_THROUGH SMART READ DATA` to those disks, which counts as activity for ATA `standby_timer`. (SAS-HBA-attached disks are unaffected — LHM can't read SMART through SAT, only perfmon, which doesn't wake the disk.)
- NVMe-only host with no power concerns: defaults are fine.

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
| `ScrapeIntervalSeconds` | int | 15 | How often the background loop calls `Update()` on the hardware monitor. `/metrics` always serves the most recent cached snapshot, so this controls hardware-poll frequency, not Prometheus scrape latency. Lower = fresher data; higher = idle AHCI HDDs can actually spin down (each `Update()` issues ATA SMART pass-through to those disks). |

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

## HardwareExporterWeb (Optional Service Discovery)

**HardwareExporterWeb** is an optional companion service that provides automatic service discovery for Prometheus. It is **not related to metrics collection** - it only helps Prometheus automatically discover monitoring targets on your network.

📖 **[Full Documentation](src/HardwareExporterWeb/README.md)**

### What It Does

- Automatically scans your local network to discover Windows machines running HardwareExporter
- Provides Prometheus HTTP Service Discovery endpoints
- Eliminates the need to manually configure each target in `prometheus.yml`

### Installation

Download `HardwareExporterWeb-win-x64.zip` from the [latest release](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) and run it on any machine in your network (doesn't need to be on every monitored machine).

### Configuration

Edit `appsettings.json`:

```json
{
  "NetworkScan": {
    "SubnetFilter": "",           // Empty = scan all local subnets
    "SubnetMask": "255.255.255.0" // Subnet mask
  }
}
```

### Prometheus Configuration with Service Discovery

Instead of static targets, use HTTP service discovery:

```yaml
scrape_configs:
  - job_name: 'windows-hardware-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/HardwareExporter'
        refresh_interval: 60s
```

### API Endpoints

- `/api/ServiceDiscovery/HardwareExporter` - Discover HardwareExporter instances
- `/api/ServiceDiscovery/WindowsExporter` - Discover windows_exporter instances

**Note:** This is completely optional. You can use static configuration in `prometheus.yml` if you prefer.

## Available Metrics

The exporter provides two types of metrics:

1. **Hardware Metrics** - From LibreHardwareMonitor
2. **.NET Runtime Metrics** - From prometheus-net (GC, threads, memory, etc.)

All metrics are available at the `/metrics` endpoint.

### Hardware Metrics Format

LHM-derived metrics carry the LHM SensorType verbatim and use LHM's native
units (see the "Metric Types" table below):

```
hardware_{type}_{sensor_type}_{sensor_name}{labels} value
```

In addition, for sensor types that carry a unit (`Throughput`, `Data`,
`SmallData`), the exporter also emits a Prometheus-conventional alias with
values normalized to base SI units. Prefer these for new dashboards — they
need no per-panel unit fiddling and sidestep the GB-quantization that makes
`rate(hardware_storage_data_*[5m])` unreliable.

```
hardware_{type}_{sensor_name}_bytes_per_second{labels} value   # Throughput
hardware_{type}_{sensor_name}_bytes{labels} value              # Data, SmallData
```

### Example Metrics

```
# CPU Temperature
hardware_cpu_temperature_core{name="AMD Ryzen 9 5900X", core="0"} 45.0

# GPU Usage
hardware_gpu_load_core{name="NVIDIA GeForce RTX 3080", vendor="nvidia"} 75.5

# Memory Usage (legacy, GB)
hardware_memory_data_memory_used{name="Generic Memory"} 18.655
# Same value, conventional alias (bytes)
hardware_memory_used_bytes{name="Generic Memory"} 18655895233

# Fan Speed
hardware_motherboard_fan_fan{name="ASUS ROG STRIX B550-F", fan="1"} 1200

# Disk throughput (legacy, B/s — note: NOT MB/s, was a doc bug)
hardware_storage_throughput_write_rate{name="KIOXIA-EXCERIA SSD"} 153991
# Same value, conventional alias
hardware_storage_write_rate_bytes_per_second{name="KIOXIA-EXCERIA SSD"} 153991

# Lifetime data written (legacy, GB cumulative — NOT a rate)
hardware_storage_data_written{name="SAMSUNG MZWLL3T2HAJQ"} 4608505
# Same value, conventional alias (bytes cumulative; safe to use rate())
hardware_storage_data_written_bytes{name="SAMSUNG MZWLL3T2HAJQ"} 4608505000000000
```

### Metric Types

| LHM SensorType | Legacy unit (in `hardware_*_<sensor_type>_*`) | Conventional alias suffix | Alias value |
|---|---|---|---|
| `Temperature` | °C | _ | _ |
| `Load` | percent (0-100) | _ | _ |
| `Clock` | MHz | _ | _ |
| `Power` | W | _ | _ |
| `Fan` | RPM | _ | _ |
| `Voltage` | V | _ | _ |
| `Throughput` | **B/s** (not MB/s — earlier docs were wrong) | `_bytes_per_second` | same value (already B/s) |
| `Data` | **GB cumulative** (not GB/s) | `_bytes` | legacy × 1e9 |
| `SmallData` | MB cumulative | `_bytes` | legacy × 1e6 |
| `Energy` | mWh | _ | _ |

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
