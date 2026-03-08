# HardwareExporterWindows

[English](../README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

LibreHardwareMonitor を使用した Windows ハードウェアメトリクス用 Prometheus エクスポーター。

## なぜこのプロジェクトが必要か

> windows_exporter の温度ゾーンデータは正確ではありません

このエクスポーターは、LibreHardwareMonitor ライブラリを直接使用することで、正確なハードウェア監視データを提供します。

## 機能

- ✅ 正確な CPU、GPU、メモリ、マザーボード、ネットワーク、ストレージメトリクス
- ✅ Windows サービスとして実行
- ✅ 設定可能なハードウェア監視（特定コンポーネントの有効/無効）
- ✅ Prometheus 互換のメトリクス形式
- ✅ シングルトンパターンによる低リソース使用
- ✅ 構造化ログ

## インストール

### 前提条件

- Windows 10/11 または Windows Server 2016+
- .NET 10.0 ランタイム（開発には SDK が必要）
- 管理者権限（ハードウェアアクセスに必要）

### クイックスタート

**方法1：MSI インストーラー（推奨）**

1. [最新リリース](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) から `HardwareExporterWindows-win-x64.msi` をダウンロード
2. MSI ファイルをダブルクリックしてインストール
3. インストーラーは自動的に：
   - `C:\Program Files\HardwareExporter` にファイルをコピー
   - Windows サービスをインストールして起動
   - ファイアウォールルールを設定

**方法2：PowerShell スクリプトによる手動インストール**

1. [最新リリース](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) から `HardwareExporterWindows-win-x64.zip` をダウンロード
2. `C:\Program Files\HardwareExporter` に解凍
3. 管理者として PowerShell を実行
4. インストールスクリプトを実行：

```powershell
cd "C:\Program Files\HardwareExporter"
.\install.ps1
```

スクリプトは以下を実行します：
- インストールディレクトリにファイルをコピー
- Windows ファイアウォールルールを作成
- Windows サービスを登録して起動

### 手動インストール

```powershell
# Windows サービスとして登録
New-Service -Name "HardwareExporter" `
    -BinaryPathName "C:\Program Files\HardwareExporter\HardwareExporterWindows.exe" `
    -StartupType "Automatic" `
    -Description "Hardware Exporter Service"

# サービスを起動
Start-Service -Name "HardwareExporter"
```

## 設定

`appsettings.json` を編集してエクスポーターをカスタマイズ：

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

### 設定オプション

| オプション | 型 | デフォルト | 説明 |
|-----------|------|-----------|------|
| `EnableCpu` | bool | true | CPU メトリクスを監視 |
| `EnableGpu` | bool | true | GPU メトリクスを監視 |
| `EnableMemory` | bool | true | メモリメトリクスを監視 |
| `EnableMotherboard` | bool | true | マザーボードメトリクスを監視 |
| `EnableController` | bool | true | コントローラーメトリクスを監視 |
| `EnableNetwork` | bool | true | ネットワークメトリクスを監視 |
| `EnableStorage` | bool | true | ストレージメトリクスを監視 |
| `ScrapeIntervalSeconds` | int | 15 | 更新間隔（未使用） |

## Prometheus 設定

`prometheus.yml` に以下を追加：

```yaml
scrape_configs:
  - job_name: 'windows-hardware'
    static_configs:
      - targets:
          - '192.168.1.100:9888'  # Windows マシンの IP
          - '192.168.1.101:9888'
```

## HardwareExporterWeb（オプションのサービスディスカバリー）

**HardwareExporterWeb** は、Prometheus の自動サービスディスカバリー機能を提供するオプションのサービスです。**メトリクス収集とは無関係** で、Prometheus がネットワーク上の監視対象を自動的に発見するのを支援します。

### 機能

- ローカルネットワークを自動スキャンし、HardwareExporter を実行している Windows マシンを発見
- Prometheus HTTP サービスディスカバリーエンドポイントを提供
- `prometheus.yml` で各ターゲットを手動設定する必要がなくなります

### インストール

[最新リリース](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) から `HardwareExporterWeb-win-x64.zip` をダウンロードし、ネットワーク内の任意のマシンで実行します（監視対象の各マシンにインストールする必要はありません）。

### 設定

`appsettings.json` を編集：

```json
{
  "NetworkScan": {
    "SubnetFilter": "",           // 空 = すべてのローカルサブネットをスキャン
    "SubnetMask": "255.255.255.0" // サブネットマスク
  }
}
```

### サービスディスカバリーを使用した Prometheus 設定

静的ターゲットの代わりに HTTP サービスディスカバリーを使用：

```yaml
scrape_configs:
  - job_name: 'windows-hardware-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/HardwareExporter'
        refresh_interval: 60s
```

### API エンドポイント

- `/api/ServiceDiscovery/HardwareExporter` - HardwareExporter インスタンスを発見
- `/api/ServiceDiscovery/WindowsExporter` - windows_exporter インスタンスを発見

**注意：** これは完全にオプションです。静的設定を好む場合は、`prometheus.yml` で直接ターゲットを設定できます。

## 利用可能なメトリクス

エクスポーターは2種類のメトリクスを提供します：

1. **ハードウェアメトリクス** - LibreHardwareMonitor から
2. **.NET ランタイムメトリクス** - prometheus-net から（GC、スレッド、メモリなど）

すべてのメトリクスは `/metrics` エンドポイントで取得できます。

### ハードウェアメトリクス形式

```
hardware_{タイプ}_{センサータイプ}_{センサー名}{ラベル} 値
```

### メトリクス例

```
# CPU 温度
hardware_cpu_temperature_core{name="AMD Ryzen 9 5900X", core="0"} 45.0

# GPU 使用率
hardware_gpu_load_core{name="NVIDIA GeForce RTX 3080", vendor="nvidia"} 75.5

# メモリ使用率
hardware_memory_load_memory{name="Generic Memory"} 45.2

# ファン速度
hardware_motherboard_fan_fan{name="ASUS ROG STRIX B550-F", fan="1"} 1200
```

### メトリクスタイプ

- `temperature` - 温度（摂氏）
- `load` - 負荷率（0-100）
- `clock` - クロック速度（MHz）
- `power` - 消費電力（ワット）
- `fan` - ファン速度（RPM）
- `voltage` - 電圧（ボルト）
- `data` - データレート（GB/s）
- `throughput` - スループット（MB/s）

## トラブルシューティング

### サービスが起動しない

1. Windows イベントビューアでエラーを確認
2. 管理者権限があることを確認
3. .NET 10.0 ランタイムがインストールされていることを確認
4. ポート 9888 が使用されていないか確認

### メトリクスが表示されない

1. サービスが実行中か確認：`Get-Service HardwareExporter`
2. エンドポイントをテスト：`curl http://localhost:9888/metrics`
3. イベントビューアの「アプリケーション」でログを確認

### ファイアウォールの問題

インストールスクリプトは自動的にファイアウォールルールを作成します。必要に応じて手動で作成：

```powershell
New-NetFirewallRule -DisplayName "HardwareExporter" `
    -Direction Inbound `
    -Program "C:\Program Files\HardwareExporter\HardwareExporterWindows.exe" `
    -Action Allow
```

## 開発

### ソースからビルド

```bash
git clone https://github.com/naughtyGitCat/HardwareExporterWindows.git
cd HardwareExporterWindows
dotnet build
```

### 開発モードで実行

```bash
cd src/HardwareExporterWindows
dotnet run
```

メトリクスにアクセス：http://localhost:9888/metrics

### テストを実行

```bash
dotnet test
```

## クレジット

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - ハードウェア監視ライブラリ
- [prometheus-net](https://github.com/prometheus-net/prometheus-net) - Prometheus クライアントライブラリ

## ライセンス

MIT License - 詳細は [LICENSE](LICENSE) ファイルを参照してください。

このプロジェクトは以下のオープンソースライブラリを使用しています：
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL 2.0)
- [prometheus-net](https://github.com/prometheus-net/prometheus-net) (MIT)

## 貢献

貢献を歓迎します！お気軽にプルリクエストを送信してください。
