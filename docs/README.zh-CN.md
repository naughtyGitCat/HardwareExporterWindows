# HardwareExporterWindows

[![构建状态](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml/badge.svg)](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml)
[![发布版本](https://img.shields.io/github/v/release/naughtyGitCat/HardwareExporterWindows)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases/latest)
[![下载量](https://img.shields.io/github/downloads/naughtyGitCat/HardwareExporterWindows/total)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases)
[![许可证](https://img.shields.io/github/license/naughtyGitCat/HardwareExporterWindows)](../LICENSE)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![平台](https://img.shields.io/badge/平台-Windows-0078D6)](https://www.microsoft.com/windows)
[![Stars](https://img.shields.io/github/stars/naughtyGitCat/HardwareExporterWindows?style=social)](https://github.com/naughtyGitCat/HardwareExporterWindows/stargazers)

[English](../README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

基于 LibreHardwareMonitor 的 Windows 硬件指标 Prometheus 导出器。

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
| `ScrapeIntervalSeconds` | int | 15 | 更新间隔（暂未使用） |

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
