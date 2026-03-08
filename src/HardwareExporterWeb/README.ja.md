# HardwareExporterWeb

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

**HardwareExporterWeb** は、[HardwareExporterWindows](../README.md) のオプションサービスで、Prometheus の自動サービスディスカバリー機能を提供します。

## 機能

HardwareExporterWeb は**メトリクスコレクターではありません** - サービスディスカバリーツールです：

- 🔍 ローカルネットワークを自動スキャンして Windows マシンを発見
- 📡 Prometheus HTTP サービスディスカバリーエンドポイントを提供
- 🌐 発見されたホストを管理する Web インターフェース
- 🌍 多言語対応（英語、中国語、日本語）

## なぜ使うのか？

`prometheus.yml` で各監視ターゲットを手動設定する代わりに：

```yaml
# 手動設定（面倒）
scrape_configs:
  - job_name: 'windows-hardware'
    static_configs:
      - targets:
          - '192.168.1.100:9888'
          - '192.168.1.101:9888'
          - '192.168.1.102:9888'
          # ... 手動で追加
```

自動サービスディスカバリーを使用：

```yaml
# 自動発見（動的）
scrape_configs:
  - job_name: 'windows-hardware-auto'
    http_sd_configs:
      - url: 'http://your-web-server/api/ServiceDiscovery/HardwareExporter'
        refresh_interval: 60s
```

## インストール

### 前提条件

- .NET 10.0 ランタイム
- サブネットをスキャンするためのネットワークアクセス

### クイックスタート

1. [最新リリース](https://github.com/naughtyGitCat/HardwareExporterWindows/releases) から `HardwareExporterWeb-win-x64.zip` をダウンロード
2. 任意の場所に解凍
3. `appsettings.json` を編集（オプション）
4. `HardwareExporterWeb.exe` を実行

Web インターフェースはデフォルトで `http://localhost:80` で利用可能です。

## 設定

`appsettings.json` を編集：

```json
{
  "NetworkScan": {
    "SubnetFilter": "",           // 空 = すべてのローカルサブネットをスキャン
    "SubnetMask": "255.255.255.0" // スキャン用のサブネットマスク
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

### ネットワークスキャンオプション

| オプション | 型 | デフォルト | 説明 |
|-----------|------|-----------|------|
| `SubnetFilter` | string | "" | オプションのサブネットフィルター（例："10.100.100"）。空 = すべてのローカルサブネットをスキャン |
| `SubnetMask` | string | "255.255.255.0" | ネットワークスキャン用のサブネットマスク |

### 設定例

**すべてのローカルサブネットをスキャン：**
```json
"SubnetFilter": ""
```

**特定のサブネットのみスキャン：**
```json
"SubnetFilter": "10.100.100"
```

**異なるサブネットマスクを使用：**
```json
"SubnetMask": "255.255.0.0"
```

## API エンドポイント

### サービスディスカバリー

- **GET** `/api/ServiceDiscovery/HardwareExporter`
  - 発見された HardwareExporter インスタンスのリストを返す
  - 形式：Prometheus HTTP SD 互換 JSON

- **GET** `/api/ServiceDiscovery/WindowsExporter`
  - 発見された windows_exporter インスタンスのリストを返す
  - 形式：Prometheus HTTP SD 互換 JSON

### レスポンス形式

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

## Prometheus 設定

### HTTP サービスディスカバリー

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

## Web インターフェース

Web インターフェースの機能：

- 📊 発見されたホストを表示
- ➕ ホストを手動追加
- ✏️ ホスト情報を編集
- 🗑️ ホストを削除
- 🌐 多言語対応（英語/中国語/日本語）

Web インターフェースにアクセス：`http://localhost:80`（または設定したポート）。

### 言語サポート

Web インターフェースは3つの言語をサポート：
- 🇺🇸 English（英語）
- 🇨🇳 简体中文（簡体字中国語）
- 🇯🇵 日本語

右上の言語セレクターで言語を切り替えます。

## アーキテクチャ

```
┌─────────────────────────────────────┐
│   Prometheus サーバー                │
│   (HTTP SD エンドポイントから取得)   │
└──────────────┬──────────────────────┘
               │
               │ HTTP SD API
               │
┌──────────────▼──────────────────────┐
│   HardwareExporterWeb               │
│   - ネットワークスキャナー            │
│   - ホストマネージャー                │
│   - Web UI                          │
└──────────────┬──────────────────────┘
               │
               │ ARP スキャン
               │
┌──────────────▼──────────────────────┐
│   ローカルネットワーク                │
│   - HardwareExporter を実行する      │
│     Windows マシン                   │
│   - windows_exporter を実行する      │
│     Windows マシン                   │
└─────────────────────────────────────┘
```

## 開発

### ソースからビルド

```bash
cd src/HardwareExporterWeb
dotnet build
```

### 開発モードで実行

```bash
cd src/HardwareExporterWeb
dotnet run
```

### テストを実行

```bash
cd test/HardwareExporterWeb.Tests
dotnet test
```

## トラブルシューティング

### サービスがホストを発見しない

1. ネットワーク接続を確認
2. `appsettings.json` のサブネット設定を確認
3. ターゲットマシンで HardwareExporter または windows_exporter が実行されていることを確認
4. ファイアウォールルールがネットワークスキャンを許可していることを確認

### Web インターフェースにアクセスできない

1. ポート 80 が既に使用されていないか確認
2. `appsettings.json` でポートを変更してみる
3. ファイアウォールが着信接続を許可していることを確認
4. アプリケーションログでエラーを確認

### Prometheus がターゲットを取得しない

1. HTTP SD URL が正しいことを確認
2. Prometheus ログで SD エラーを確認
3. API エンドポイントを手動でテスト：`curl http://your-server/api/ServiceDiscovery/HardwareExporter`
4. `refresh_interval` が適切に設定されていることを確認

## ライセンス

MIT License - 詳細は [LICENSE](../LICENSE) ファイルを参照してください。

## 関連プロジェクト

- [HardwareExporterWindows](../README.md) - メインのハードウェアメトリクスエクスポーター
- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) - ハードウェア監視ライブラリ
- [Prometheus](https://prometheus.io/) - 監視システムと時系列データベース

## 貢献

貢献を歓迎します！お気軽にプルリクエストを送信してください。
