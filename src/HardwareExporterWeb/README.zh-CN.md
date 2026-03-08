# HardwareExporterWeb

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

**HardwareExporterWeb** 是 [HardwareExporterWindows](../README.md) 的可选配套服务，为 Prometheus 提供自动服务发现功能。

## 功能说明

HardwareExporterWeb **不是指标采集器** - 它是一个服务发现工具，可以：

- 🔍 自动扫描本地网络以发现 Windows 机器
- 📡 提供 Prometheus HTTP 服务发现端点
- 🌐 提供 Web 界面管理已发现的主机
- 🌍 支持多语言（英文、中文、日文）

## 为什么使用它？

无需在 `prometheus.yml` 中手动配置每个监控目标：

```yaml
# 手动配置（繁琐）
scrape_configs:
  - job_name: 'windows-hardware'
    static_configs:
      - targets:
          - '192.168.1.100:9888'
          - '192.168.1.101:9888'
          - '192.168.1.102:9888'
          # ... 手动添加更多
```

使用自动服务发现：

```yaml
# 自动发现（动态）
scrape_configs:
  - job_name: 'windows-hardware-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/HardwareExporter'
        refresh_interval: 60s
```

## 安装

### 前置要求

- .NET 10.0 运行时
- 网络访问权限以扫描子网

### 快速开始

1. 从 [最新版本](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) 下载 `HardwareExporterWeb-win-x64.zip`
2. 解压到你想要的位置
3. 编辑 `appsettings.json`（可选）
4. 运行 `HardwareExporterWeb.exe`

Web 界面默认在 `http://localhost:80` 可用。

## 配置

编辑 `appsettings.json`：

```json
{
  "NetworkScan": {
    "SubnetFilter": "",           // 空 = 扫描所有本地子网
    "SubnetMask": "255.255.255.0" // 扫描用的子网掩码
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

### 网络扫描选项

| 选项 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `SubnetFilter` | string | "" | 可选的子网过滤器（如 "10.100.100"）。空 = 扫描所有本地子网 |
| `SubnetMask` | string | "255.255.255.0" | 网络扫描用的子网掩码 |

### 配置示例

**扫描所有本地子网：**
```json
"SubnetFilter": ""
```

**仅扫描特定子网：**
```json
"SubnetFilter": "10.100.100"
```

**使用不同的子网掩码：**
```json
"SubnetMask": "255.255.0.0"
```

## API 端点

### 服务发现

- **GET** `/api/ServiceDiscovery/HardwareExporter`
  - 返回已发现的 HardwareExporter 实例列表
  - 格式：Prometheus HTTP SD 兼容 JSON

- **GET** `/api/ServiceDiscovery/WindowsExporter`
  - 返回已发现的 windows_exporter 实例列表
  - 格式：Prometheus HTTP SD 兼容 JSON

### 响应格式

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

## Prometheus 配置

### HTTP 服务发现

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

## Web 界面

Web 界面提供：

- 📊 查看已发现的主机
- ➕ 手动添加主机
- ✏️ 编辑主机信息
- 🗑️ 删除主机
- 🌐 多语言支持（英文/中文/日文）

访问 Web 界面：`http://localhost:80`（或你配置的端口）。

### 语言支持

Web 界面支持三种语言：
- 🇺🇸 English（英文）
- 🇨🇳 简体中文
- 🇯🇵 日本語（日文）

使用右上角的语言选择器切换语言。

## 架构

```
┌─────────────────────────────────────┐
│   Prometheus 服务器                  │
│   (从 HTTP SD 端点拉取)              │
└──────────────┬──────────────────────┘
               │
               │ HTTP SD API
               │
┌──────────────▼──────────────────────┐
│   HardwareExporterWeb               │
│   - 网络扫描器                       │
│   - 主机管理器                       │
│   - Web UI                          │
└──────────────┬──────────────────────┘
               │
               │ ARP 扫描
               │
┌──────────────▼──────────────────────┐
│   本地网络                           │
│   - 运行 HardwareExporter 的         │
│     Windows 机器                     │
│   - 运行 windows_exporter 的         │
│     Windows 机器                     │
└─────────────────────────────────────┘
```

## 开发

### 从源码构建

```bash
cd src/HardwareExporterWeb
dotnet build
```

### 开发模式运行

```bash
cd src/HardwareExporterWeb
dotnet run
```

### 运行测试

```bash
cd test/HardwareExporterWeb.Tests
dotnet test
```

## 故障排查

### 服务未发现主机

1. 检查网络连接
2. 验证 `appsettings.json` 中的子网配置
3. 确保目标机器正在运行 HardwareExporter 或 windows_exporter
4. 检查防火墙规则是否允许网络扫描

### Web 界面无法访问

1. 检查端口 80 是否已被占用
2. 尝试在 `appsettings.json` 中更改端口
3. 验证防火墙允许入站连接
4. 检查应用程序日志中的错误

### Prometheus 未获取到目标

1. 验证 HTTP SD URL 是否正确
2. 检查 Prometheus 日志中的 SD 错误
3. 手动测试 API 端点：`curl http://your-server/api/ServiceDiscovery/HardwareExporter`
4. 确保 `refresh_interval` 设置适当

## 许可证

MIT License - 详见 [LICENSE](../LICENSE) 文件。

## 相关项目

- [HardwareExporterWindows](../README.md) - 主要的硬件指标导出器
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - 硬件监控库
- [Prometheus](https://prometheus.io/) - 监控系统和时间序列数据库

## 贡献

欢迎贡献！请随时提交 Pull Request。
