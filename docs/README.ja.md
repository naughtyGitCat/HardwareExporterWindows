# HardwareExporterWindows

[![ビルドステータス](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml/badge.svg)](https://github.com/naughtyGitCat/HardwareExporterWindows/actions/workflows/dotnet.yml)
[![リリース](https://img.shields.io/github/v/release/naughtyGitCat/HardwareExporterWindows)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases/latest)
[![ダウンロード](https://img.shields.io/github/downloads/naughtyGitCat/HardwareExporterWindows/total)](https://github.com/naughtyGitCat/HardwareExporterWindows/releases)
[![ライセンス](https://img.shields.io/github/license/naughtyGitCat/HardwareExporterWindows)](../LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![プラットフォーム](https://img.shields.io/badge/プラットフォーム-Windows-0078D6)](https://www.microsoft.com/windows)
[![Stars](https://img.shields.io/github/stars/naughtyGitCat/HardwareExporterWindows?style=social)](https://github.com/naughtyGitCat/HardwareExporterWindows/stargazers)

[English](../README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

LibreHardwareMonitor を使用した Windows ハードウェアメトリクス用 Prometheus エクスポーター。

## Grafana ダッシュボード

![Grafana ダッシュボード](images/grafana-dashboard.png)

すぐに使える Grafana ダッシュボードが [`docs/grafana-dashboard.json`](grafana-dashboard.json) に含まれています。Grafana UI からインポートできます（ダッシュボード > インポート > JSON ファイルをアップロード）。

**ダッシュボードのセクション：** CPU 概要、CPU 温度、CPU 電力と電圧、ファン、メモリ、GPU（温度 / 負荷 / VRAM / 電力 / クロック / PCIe）、ディスク（温度 / 容量 / 健康状態 / IO / スループット）、ネットワーク（使用率 / スループット / データ転送量）。

## 今後の破壊的変更：LHM レガシーメトリクス名は 2026-08-15 以降に削除されます

ee6b1c2（2026-05-08）まで、LHM 由来のメトリクスは `hardware_<type>_<sensor_type>_<name>` として LHM ネイティブ単位（`Data` は GB、`SmallData` は MB、`Throughput` は B/s）で出力されていました。この命名は単位が暗黙で、さらに Data は GB 量子化された整数のため `rate(hardware_storage_data_*[5m])` が短いウィンドウで 0 を返してしまっていました。

exporter は現在、Prometheus 規約に沿った別名を**並行で**出力します：Throughput は `*_bytes_per_second`、Data / SmallData は `*_bytes`、値は SI 基本単位に正規化されています。**レガシー名は 2026-08-15 以降にタグ付けされたリリースで削除されます**。完全な対応表は [CHANGELOG.md](../CHANGELOG.md) を参照してください。

移行手順：

1. [`docs/grafana-dashboard.json`](grafana-dashboard.json) を再 import してください。このダッシュボードは 2026-05-08 に別名へ移行済みです。
2. カスタムダッシュボード / アラートルールを監査：
   ```promql
   {__name__=~"hardware_(storage|memory|network|gpu)_(throughput|data)_.*"}
   ```
   返ってくるすべての series は、消える予定の名前を使っています。
3. `appsettings.json` で `HardwareMonitor:EmitLegacyMetricNames` を `false` に設定すると、「削除後の /metrics がどう見えるか」をプレビューできます。気になるメトリクスがそこで消えるなら、移行のどこかを見落としています。

単位を持たないセンサー（温度、ファン、電圧、電力、クロック、load、level、factor）は影響を受けません——元々この命名しかなく、`EmitLegacyMetricNames=false` で一緒にミュートされることはありません。

## SMART ディスク属性

LibreHardwareMonitor は SAS HBA（LSI/Avago、Adaptec など）の背後にあるディスクの SMART データを読み取れません。これらのディスクは SCSI デバイスとして見え、SAT（SCSI/ATA Translation）パススルーが必要ですが、ライブラリはこれを実装していないためです。このギャップを埋めるため、本 exporter は [`smartctl`](https://www.smartmontools.org/) を同梱し、`hardware_storage_smart_*` プレフィックスで並列のメトリクス群を出力します。

同梱バイナリは `<インストール先>\smartctl\` に配置されます（合計約 1.4 MB）。これらは別プロセスとして呼び出されます。smartmontools は GPLv2+ で、未改変の独立した実行ファイルとして同梱されています。完全なクレジットは `smartctl\COPYING.txt` および `smartctl\README.txt` を参照してください。

別のバージョンの `smartctl.exe`（より新しいものなど）を使いたい場合は、`appsettings.json` で `SmartMonitor:SmartctlPath` を設定します。SMART 採集を完全に無効化するには `SmartMonitor:Enable` を `false` にします。

### SMART メトリクスの例

```
# ディスク温度（SAS HBA 接続の SATA ディスクを含む）
hardware_storage_smart_temperature_celsius{device="/dev/sda", model="WDC  WUH721816ALE6L4", serial="3HGHU16P", protocol="ata", firmware="PCGNW232"} 34

# 全体ヘルス自己診断（1 = 合格、0 = 不合格）
hardware_storage_smart_health_passed{device="/dev/sda", ...} 1

# ATA SMART 属性表（id 別）
hardware_storage_smart_ata_attribute_raw{device="/dev/sda", ..., id="5", name="Reallocated_Sector_Ct"} 0
hardware_storage_smart_ata_attribute_raw{device="/dev/sda", ..., id="9", name="Power_On_Hours"} 31120

# NVMe 固有
hardware_storage_smart_nvme_percentage_used{device="/dev/sdi", model="SAMSUNG MZWLL3T2HAJQ-00005", ...} 6
hardware_storage_smart_nvme_media_errors{device="/dev/sdi", ...} 0
```

### SMART 設定

| オプション | 型 | デフォルト | 説明 |
|-----------|------|-----------|------|
| `SmartMonitor:Enable` | bool | true | SMART コレクターを有効化 |
| `SmartMonitor:SmartctlPath` | string | `""` | smartctl.exe のパスをオーバーライド（空なら同梱版を使用） |
| `SmartMonitor:RefreshIntervalSeconds` | int | 60 | smartctl を再ポーリングする間隔。リフレッシュは非同期で、`/metrics` は常にキャッシュされた最新スナップショットを返すため、Prometheus の scrape レイテンシには影響しません。注意：SMART を読み取るとアイドル中の HDD が起き上がるので、過度に短くしないでください。 |
| `SmartMonitor:InvocationTimeoutSeconds` | int | 15 | ディスク 1 台あたりの smartctl 呼び出しタイムアウト |
| `SmartMonitor:DeviceExcludePatterns` | string[] | `[]` | `/dev/sdN` 名にマッチさせる glob パターン。マッチしたデバイスはスキップされる |

## キャッシュとリフレッシュモデル

両方のコレクターは独立したバックグラウンドリフレッシュループを持ち、`/metrics` はメモリ内キャッシュから応答します。Prometheus の scrape は**ハードウェア読み取りを直接トリガーしません**。独立した 2 つのキャッシュがあります：

| キャッシュ | 設定項目 | デフォルト | 下限 |
|---|---|---|---|
| LibreHardwareMonitor（CPU / GPU / メモリ / マザーボード / ネットワーク / ネイティブ SATA SMART / NVMe） | `HardwareMonitor:ScrapeIntervalSeconds` | **15s** | 1s |
| smartctl（全ディスクの完全な SMART 表、SAS HBA 背後のディスクを含む） | `SmartMonitor:RefreshIntervalSeconds` | **60s** | 15s |

サービス起動時に両方とも即座に 1 回リフレッシュするため、起動後初回の scrape からデータが揃っています。ウォームアップギャップはありません。

**デフォルト値が異なる理由**

- `HardwareMonitor` は変化の速いテレメトリ（CPU 温度、GPU 負荷、ファン回転数）をカバーします。15s は Prometheus のデフォルト scrape 間隔と同期しているため、クライアント側の遅延は知覚できないレベルです。
- `SmartMonitor` は変化の遅いヘルス属性（温度は分単位で変動、reallocated_sectors は日単位で変動）を読み取ります。さらに重要なのは、アイドル中の HDD に対する smartctl 呼び出しは毎回その HDD を起き上がらせることです——scrape 経路で実行するとディスクが永久に休止できなくなります。60s は実際に許容できる下限です。

**実効的な古さ**

Prometheus クライアントが見るメトリクスは最大で `cache_interval + scrape_interval` 古くなります。デフォルト設定では：

- LibreHardwareMonitor メトリクス：≤ 30 秒
- SMART メトリクス：≤ 75 秒

**チューニングのタイミング**

- 1Hz のリアルタイムダッシュボードが欲しい場合：`HardwareMonitor:ScrapeIntervalSeconds=1`。`SmartMonitor` は触らないでください——15s が既に下限で、それ以下にするとアイドルドライブを酷使します。
- AHCI 接続の SATA HDD を実際にスピンダウンさせたい場合：`HardwareMonitor:ScrapeIntervalSeconds` を 60s 以上に上げます。LHM の `Update()` はそれらのディスクに対して `IOCTL_ATA_PASS_THROUGH SMART READ DATA` を発行し、ATA `standby_timer` 上はアクティビティとしてカウントされます。（SAS HBA 接続のディスクは影響なし——LHM は SAT 経路を通せず perfmon のみを読むため、ディスクを起こしません。）
- NVMe のみで省電力を気にしないホスト：デフォルトのままでよいです。

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
| `ScrapeIntervalSeconds` | int | 15 | バックグラウンドループがハードウェアモニタの `Update()` を呼び出す頻度。`/metrics` は常にキャッシュされた最新スナップショットを返すため、これはハードウェアのポーリング頻度を制御するもので、Prometheus の scrape レイテンシではありません。低い値ほどデータが新鮮、高い値ほどアイドル中の AHCI HDD が実際にスピンダウンできます（`Update()` は対象ディスクに ATA SMART パススルーを発行するため）。 |

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

📖 **[完全なドキュメント](../src/HardwareExporterWeb/README.md)**

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

LHM 由来のメトリクスは LHM の SensorType をそのまま保ち、LHM ネイティブの単位を使用します（下の「メトリクスタイプ」表を参照）：

```
hardware_{タイプ}_{センサータイプ}_{センサー名}{ラベル} 値
```

加えて、単位を持つセンサータイプ（`Throughput`、`Data`、`SmallData`）については、Prometheus 規約に沿った別名を**並行で**出力し、値は SI 基本単位に正規化されます。新しいダッシュボードではこちらを優先してください——パネルごとの単位調整が不要で、`rate(hardware_storage_data_*[5m])` を不安定にする GB 量子化問題も回避できます。

```
hardware_{タイプ}_{センサー名}_bytes_per_second{ラベル} 値   # Throughput
hardware_{タイプ}_{センサー名}_bytes{ラベル} 値              # Data, SmallData
```

### メトリクス例

```
# CPU 温度
hardware_cpu_temperature_core{name="AMD Ryzen 9 5900X", core="0"} 45.0

# GPU 使用率
hardware_gpu_load_core{name="NVIDIA GeForce RTX 3080", vendor="nvidia"} 75.5

# メモリ使用量（レガシー、GB）
hardware_memory_data_memory_used{name="Generic Memory"} 18.655
# 同じ値、規約準拠の別名（bytes）
hardware_memory_used_bytes{name="Generic Memory"} 18655895233

# ファン速度
hardware_motherboard_fan_fan{name="ASUS ROG STRIX B550-F", fan="1"} 1200

# ディスクスループット（レガシー、B/s——注：MB/s ではない、過去のドキュメントに誤りあり）
hardware_storage_throughput_write_rate{name="KIOXIA-EXCERIA SSD"} 153991
# 同じ値、規約準拠の別名
hardware_storage_write_rate_bytes_per_second{name="KIOXIA-EXCERIA SSD"} 153991

# 累計書き込み（レガシー、GB 累計値——レートではない）
hardware_storage_data_written{name="SAMSUNG MZWLL3T2HAJQ"} 4608505
# 同じ値、規約準拠の別名（bytes 累計値；rate() を安全に使用可能）
hardware_storage_data_written_bytes{name="SAMSUNG MZWLL3T2HAJQ"} 4608505000000000
```

### メトリクスタイプ

| LHM SensorType | レガシー単位（`hardware_*_<sensor_type>_*` 内） | 規約準拠の別名サフィックス | 別名の値 |
|---|---|---|---|
| `Temperature` | °C | _ | _ |
| `Load` | percent (0-100) | _ | _ |
| `Clock` | MHz | _ | _ |
| `Power` | W | _ | _ |
| `Fan` | RPM | _ | _ |
| `Voltage` | V | _ | _ |
| `Throughput` | **B/s**（以前のドキュメントの MB/s は誤り） | `_bytes_per_second` | 値はそのまま（既に B/s） |
| `Data` | **GB 累計値**（GB/s ではない） | `_bytes` | レガシー値 × 1e9 |
| `SmallData` | MB 累計値 | `_bytes` | レガシー値 × 1e6 |
| `Energy` | mWh | _ | _ |

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
