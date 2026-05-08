# Changelog

All notable changes to this project will be documented in this file.

## Upcoming Breaking Change — scheduled ≥ 2026-08-15

Legacy LHM metric names of the form `hardware_<type>_<sensor_type>_<name>`
will be **removed** in a release tagged on or after **2026-08-15**. After
removal, only the Prometheus-conventional aliases will be emitted:

| Old (removed)                                 | New (kept)                                                  |
|-----------------------------------------------|-------------------------------------------------------------|
| `hardware_storage_throughput_read_rate`       | `hardware_storage_read_rate_bytes_per_second`               |
| `hardware_storage_throughput_write_rate`      | `hardware_storage_write_rate_bytes_per_second`              |
| `hardware_storage_data_read`         (in GB)  | `hardware_storage_data_read_bytes`         (in bytes)       |
| `hardware_storage_data_written`      (in GB)  | `hardware_storage_data_written_bytes`      (in bytes)       |
| `hardware_memory_data_memory_used`   (in GB)  | `hardware_memory_used_bytes`               (in bytes)       |
| `hardware_memory_data_memory_available`       | `hardware_memory_available_bytes`                           |
| `hardware_memory_data_virtual_memory_used`    | `hardware_memory_virtual_memory_used_bytes`                 |
| `hardware_memory_data_virtual_memory_available` | `hardware_memory_virtual_memory_available_bytes`          |
| `hardware_network_throughput_download_speed`  | `hardware_network_download_speed_bytes_per_second`          |
| `hardware_network_throughput_upload_speed`    | `hardware_network_upload_speed_bytes_per_second`            |
| `hardware_network_data_downloaded`   (in GB)  | `hardware_network_downloaded_bytes`        (in bytes)       |
| `hardware_network_data_uploaded`     (in GB)  | `hardware_network_uploaded_bytes`          (in bytes)       |
| `hardware_gpu_throughput_gpu_pcie_rx`         | `hardware_gpu_pcie_rx_bytes_per_second`                     |
| `hardware_gpu_throughput_gpu_pcie_tx`         | `hardware_gpu_pcie_tx_bytes_per_second`                     |

Sensors that do not carry a unit (`temperature`, `voltage`, `power`, `clock`,
`fan`, `load`, `level`, `factor`) are **unaffected** — their legacy form is
the only form and will continue to be emitted unchanged.

### How to prepare

1. Update Prometheus / Grafana to use the new alias names. The bundled
   `docs/grafana-dashboard.json` was migrated to aliases on 2026-05-08;
   re-import it to refresh existing deploys.
2. Audit any custom dashboards or alert rules with this Prometheus query:
   ```
   {__name__=~"hardware_(storage|memory|network|gpu)_(throughput|data)_.*"}
   ```
   Any series this returns is on a name that will go away.
3. Once everything looks good, set `HardwareMonitor:EmitLegacyMetricNames=false`
   in `appsettings.json` to preview the post-removal /metrics. If something
   you care about disappears, you missed a migration step somewhere.

## [Unreleased]

### Added
- Configuration support via `appsettings.json`
- Ability to enable/disable specific hardware monitoring components
- Structured logging with ILogger
- Comprehensive error handling
- Detailed README with installation and configuration instructions
- XML documentation comments for public APIs

### Changed
- **BREAKING**: Refactored to use singleton pattern for Computer instance (major performance improvement)
- Renamed `Y.cs` to `HardwareMonitorBackgroundService.cs` for clarity
- Refactored `MetricsController` to use dependency injection
- Improved metric name processing with better documentation
- Made HTTPS redirection optional (disabled by default for internal networks)
- Updated default port to 9888 (avoids conflict with windows_exporter on 9182)

### Fixed
- Performance issue: Computer instance is now initialized once instead of on every request
- Thread safety: Added semaphore lock for hardware updates
- Memory leaks: Proper disposal of Computer instance

### Technical Improvements
- Added `HardwareMonitorService` as a hosted service
- Introduced `HardwareMonitorOptions` configuration class
- Better separation of concerns
- Improved code readability with XML comments
- Added async/await pattern where appropriate

## Testing Required

Before merging, please test on Windows:

1. ✅ Service starts successfully
2. ✅ Metrics endpoint returns data: `http://localhost:9888/metrics`
3. ✅ Configuration changes are respected (try disabling GPU monitoring)
4. ✅ Service runs stably for extended period
5. ✅ Memory usage remains stable (no leaks)
6. ✅ CPU usage is low when idle
7. ✅ Prometheus can scrape the metrics successfully

## [Previous Versions]

See git history for previous changes.
