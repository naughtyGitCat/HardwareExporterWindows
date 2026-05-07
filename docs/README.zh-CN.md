# HardwareExporterWindows

[![构建状态](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml/badge.svg)](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml)
[![发布版本](https://img.shields.io/github/v/release/naughtyGitCat/HardwareExporterWindows)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases/latest)
[![下载量](https://img.shields.io/github/downloads/naughtyGitCat/HardwareExporterWindows/total)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases)
[![许可证](https://img.shields.io/github/license/naughtyGitCat/HardwareExporterWindows)](../LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![平台](https://img.shields.io/badge/平台-Windows-0078D6)](https://www.microsoft.com/windows)
[![Stars](https://img.shields.io/github/stars/naughtyGitCat/HardwareExporterWindows?style=social)](https://github.com/naughtyGitCat/HardwareExporterWindows/stargazers)

[English](../README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

基于 LibreHardwareMonitor 的 Windows 硬件指标 Prometheus 导出器。

## Grafana 仪表板

![Grafana 仪表板](images/grafana-dashboard.png)

项目附带了开箱即用的 Grafana 仪表板 [`docs/grafana-dashboard.json`](grafana-dashboard.json)，通过 Grafana 界面导入即可使用（仪表板 > 导入 > 上传 JSON 文件）。

**仪表板包含：** CPU 概览、CPU 温度、CPU 功率与电压、风扇、内存、GPU（温度 / 负载 / 显存 / 功率 / 频率 / PCIe）、磁盘（温度 / 空间 / 健康度 / IO / 吞吐量）、网络（利用率 / 吞吐量 / 流量统计）。

## SMART 磁盘属性

LibreHardwareMonitor 无法读取挂在 SAS HBA（LSI/Avago、Adaptec 等）后面的磁盘的 SMART 数据，因为这些盘呈现为 SCSI 设备，需要 SAT（SCSI/ATA Translation）pass-through，而该库未实现这部分。为了补上这块缺口，本 exporter 内置 [`smartctl`](https://www.smartmontools.org/) 并以 `hardware_storage_smart_*` 前缀输出一组并行指标。

内置的二进制位于 `<安装目录>\smartctl\`（共约 1.4 MB）。它以独立子进程方式被调用；smartmontools 是 GPLv2+，作为独立、未修改的可执行文件包含在内。完整归属信息见 `smartctl\COPYING.txt` 和 `smartctl\README.txt`。

如果想用其他版本的 `smartctl.exe`（例如更新版），在 `appsettings.json` 里设置 `SmartMonitor:SmartctlPath`。要完全关闭 SMART 采集，把 `SmartMonitor:Enable` 设为 `false`。

### SMART 指标示例

```
# 磁盘温度，包括挂在 SAS HBA 后面的 SATA 盘
hardware_storage_smart_temperature_celsius{device="/dev/sda", model="WDC  WUH721816ALE6L4", serial="3HGHU16P", protocol="ata", firmware="PCGNW232"} 34

# 整体健康自检（1 = 通过，0 = 失败）
hardware_storage_smart_health_passed{device="/dev/sda", ...} 1

# ATA SMART 属性表（按 id）
hardware_storage_smart_ata_attribute_raw{device="/dev/sda", ..., id="5", name="Reallocated_Sector_Ct"} 0
hardware_storage_smart_ata_attribute_raw{device="/dev/sda", ..., id="9", name="Power_On_Hours"} 31120

# NVMe 专属
hardware_storage_smart_nvme_percentage_used{device="/dev/sdi", model="SAMSUNG MZWLL3T2HAJQ-00005", ...} 6
hardware_storage_smart_nvme_media_errors{device="/dev/sdi", ...} 0
```

### SMART 配置

| 选项 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `SmartMonitor:Enable` | bool | true | 启用 SMART 采集器 |
| `SmartMonitor:SmartctlPath` | string | `""` | 覆盖 smartctl.exe 路径（留空使用内置） |
| `SmartMonitor:RefreshIntervalSeconds` | int | 60 | 多久重新轮询 smartctl 一次。刷新是异步的；`/metrics` 始终返回最近一次缓存的快照，所以 Prometheus scrape 延迟不受影响。注意：读取 SMART 会唤醒 idle 的 HDD，不要设得太激进。 |
| `SmartMonitor:InvocationTimeoutSeconds` | int | 15 | 单次 smartctl 调用的每盘超时 |
| `SmartMonitor:DeviceExcludePatterns` | string[] | `[]` | 用于匹配 `/dev/sdN` 名称的 glob 模式，匹配到的设备会被跳过 |

## 缓存与刷新模型

两个采集器都跑各自的后台刷新循环，`/metrics` 从内存缓存中取数据。Prometheus scrape **从来不会**直接触发硬件读取。一共两个独立缓存：

| 缓存 | 配置项 | 默认值 | 下限 |
|---|---|---|---|
| LibreHardwareMonitor（CPU / GPU / 内存 / 主板 / 网卡 / 直连 SATA SMART / NVMe） | `HardwareMonitor:ScrapeIntervalSeconds` | **15s** | 1s |
| smartctl（每块盘的完整 SMART 表，含 SAS HBA 后面的盘） | `SmartMonitor:RefreshIntervalSeconds` | **60s** | 15s |

服务启动时两个都会立刻刷一次，所以服务起来后第一次 scrape 就有数据，没有冷启动空洞。

**为什么默认值不一样**

- `HardwareMonitor` 覆盖快变指标（CPU 温度、GPU 负载、风扇转速）。15s 与 Prometheus 默认 scrape 间隔同步，客户端看到的延迟可忽略。
- `SmartMonitor` 读慢变健康属性（温度按分钟级漂移，reallocated_sectors 按天级变）。更关键的是，**每次** smartctl 调到 idle 的 HDD 都会让它重新 spin up——把它放到 scrape 路径上等于让盘永远睡不着。60s 是真正能让盘休眠的实际下限。

**有效陈旧度**

Prometheus 客户端看到的某个指标最多旧 `cache_interval + scrape_interval`。默认值下：

- LibreHardwareMonitor 指标：≤ 30 秒
- SMART 指标：≤ 75 秒

**什么时候要调参**

- 想要 1Hz 实时仪表：`HardwareMonitor:ScrapeIntervalSeconds=1`。**别动** `SmartMonitor`——15s 已经是下限，再低就直接搅乱 idle 盘。
- 你的 AHCI 直连 SATA HDD 想真睡下来：把 `HardwareMonitor:ScrapeIntervalSeconds` 提到 ≥ 60s。LHM 的 `Update()` 会对这些盘下 `IOCTL_ATA_PASS_THROUGH SMART READ DATA`，对 ATA `standby_timer` 而言这就是活动。（SAS HBA 后面的盘不受影响——LHM 走不通 SAT 这条路，只读 perfmon，不会唤盘。）
- 全 NVMe 主机、不在意省电：默认值即可。

## 为什么需要这个项目

> windows_exporter 的温度区域数据不够准确

本导出器通过直接使用 LibreHardwareMonitor 库提供准确的硬件监控数据。

## 功能特性

- ✅ 准确的 CPU、GPU、内存、主板、网络和存储指标
- ✅ 作为 Windows 服务运行
- ✅ 可配置的硬件监控（启用/禁用特定组件）
- ✅ Prometheus 兼容的指标格式
- ✅ 单例模式低资源占用
- ✅ 结构化日志

## 安装

### 前置要求

- Windows 10/11 或 Windows Server 2016+
- .NET 10.0 运行时（开发需要 SDK）
- 管理员权限（硬件访问需要）

### 快速开始

**方式一：MSI 安装器（推荐）**

1. 从 [最新版本](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) 下载 `HardwareExporterWindows-win-x64.msi`
2. 双击 MSI 文件进行安装
3. 安装器将自动：
   - 复制文件到 `C:\Program Files\HardwareExporter`
   - 安装并启动 Windows 服务
   - 配置防火墙规则

**方式二：使用 PowerShell 脚本手动安装**

1. 从 [最新版本](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) 下载 `HardwareExporterWindows-win-x64.zip`
2. 解压到 `C:\Program Files\HardwareExporter`
3. 以管理员身份运行 PowerShell
4. 执行安装脚本：

```powershell
cd "C:\Program Files\HardwareExporter"
.\install.ps1
```

脚本将会：
- 复制文件到安装目录
- 创建 Windows 防火墙规则
- 注册并启动 Windows 服务

### 手动安装

```powershell
# 注册为 Windows 服务
New-Service -Name "HardwareExporter" `
    -BinaryPathName "C:\Program Files\HardwareExporter\HardwareExporterWindows.exe" `
    -StartupType "Automatic" `
    -Description "Hardware Exporter Service"

# 启动服务
Start-Service -Name "HardwareExporter"
```

## 配置

编辑 `appsettings.json` 来自定义导出器：

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

### 配置选项

| 选项 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `EnableCpu` | bool | true | 监控 CPU 指标 |
| `EnableGpu` | bool | true | 监控 GPU 指标 |
| `EnableMemory` | bool | true | 监控内存指标 |
| `EnableMotherboard` | bool | true | 监控主板指标 |
| `EnableController` | bool | true | 监控控制器指标 |
| `EnableNetwork` | bool | true | 监控网络指标 |
| `EnableStorage` | bool | true | 监控存储指标 |
| `ScrapeIntervalSeconds` | int | 15 | 后台循环调用硬件 `Update()` 的频率。`/metrics` 始终返回最近一次缓存的快照，所以这个参数控制硬件轮询频率而非 Prometheus scrape 延迟。值越小数据越新；值越大 idle 的 AHCI HDD 才有机会休眠（每次 `Update()` 都会对那些盘下 ATA SMART pass-through）。 |

## Prometheus 配置

在你的 `prometheus.yml` 中添加：

```yaml
scrape_configs:
  - job_name: 'windows-hardware'
    static_configs:
      - targets:
          - '192.168.1.100:9888'  # 你的 Windows 机器 IP
          - '192.168.1.101:9888'
```

## HardwareExporterWeb（可选的服务发现）

**HardwareExporterWeb** 是一个可选的配套服务，为 Prometheus 提供自动服务发现功能。它**与指标采集无关** - 只是帮助 Prometheus 自动发现网络中的监控目标。

📖 **[完整文档](../src/HardwareExporterWeb/README.md)**

### 功能说明

- 自动扫描本地网络，发现运行 HardwareExporter 的 Windows 机器
- 提供 Prometheus HTTP 服务发现端点
- 无需在 `prometheus.yml` 中手动配置每个目标

### 安装

从 [最新版本](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) 下载 `HardwareExporterWeb-win-x64.zip`，在网络中的任意一台机器上运行即可（不需要在每台被监控机器上安装）。

### 配置

编辑 `appsettings.json`：

```json
{
  "NetworkScan": {
    "SubnetFilter": "",           // 空 = 扫描所有本地子网
    "SubnetMask": "255.255.255.0" // 子网掩码
  }
}
```

### 使用服务发现的 Prometheus 配置

使用 HTTP 服务发现代替静态目标：

```yaml
scrape_configs:
  - job_name: 'windows-hardware-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/HardwareExporter'
        refresh_interval: 60s
```

### API 端点

- `/api/ServiceDiscovery/HardwareExporter` - 发现 HardwareExporter 实例
- `/api/ServiceDiscovery/WindowsExporter` - 发现 windows_exporter 实例

**注意：** 这是完全可选的功能。如果你更喜欢静态配置，可以直接在 `prometheus.yml` 中配置目标。

## 可用指标

导出器提供两种类型的指标：

1. **硬件指标** - 来自 LibreHardwareMonitor
2. **.NET 运行时指标** - 来自 prometheus-net（GC、线程、内存等）

所有指标都可以在 `/metrics` 端点获取。

### 硬件指标格式

```
hardware_{类型}_{传感器类型}_{传感器名称}{标签} 值
```

### 指标示例

```
# CPU 温度
hardware_cpu_temperature_core{name="AMD Ryzen 9 5900X", core="0"} 45.0

# GPU 使用率
hardware_gpu_load_core{name="NVIDIA GeForce RTX 3080", vendor="nvidia"} 75.5

# 内存使用率
hardware_memory_load_memory{name="Generic Memory"} 45.2

# 风扇转速
hardware_motherboard_fan_fan{name="ASUS ROG STRIX B550-F", fan="1"} 1200
```

### 指标类型

- `temperature` - 温度（摄氏度）
- `load` - 负载百分比（0-100）
- `clock` - 时钟频率（MHz）
- `power` - 功耗（瓦特）
- `fan` - 风扇转速（RPM）
- `voltage` - 电压（伏特）
- `data` - 数据速率（GB/s）
- `throughput` - 吞吐量（MB/s）

## 故障排查

### 服务无法启动

1. 检查 Windows 事件查看器中的错误
2. 确保你有管理员权限
3. 验证 .NET 10.0 运行时已安装
4. 检查端口 9888 是否已被占用

### 没有指标显示

1. 检查服务是否运行：`Get-Service HardwareExporter`
2. 测试端点：`curl http://localhost:9888/metrics`
3. 在事件查看器的"应用程序"下检查日志

### 防火墙问题

安装脚本会自动创建防火墙规则。如果需要，可以手动创建：

```powershell
New-NetFirewallRule -DisplayName "HardwareExporter" `
    -Direction Inbound `
    -Program "C:\Program Files\HardwareExporter\HardwareExporterWindows.exe" `
    -Action Allow
```

## 开发

### 从源码构建

```bash
git clone https://github.com/naughtyGitCat/HardwareExporterWindows.git
cd HardwareExporterWindows
dotnet build
```

### 开发模式运行

```bash
cd src/HardwareExporterWindows
dotnet run
```

访问指标：http://localhost:9888/metrics

### 运行测试

```bash
dotnet test
```

## 致谢

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - 硬件监控库
- [prometheus-net](https://github.com/prometheus-net/prometheus-net) - Prometheus 客户端库

## 许可证

MIT License - 详见 [LICENSE](LICENSE) 文件。

本项目使用了以下开源库：
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL 2.0)
- [prometheus-net](https://github.com/prometheus-net/prometheus-net) (MIT)

## 贡献

欢迎贡献！请随时提交 Pull Request。
