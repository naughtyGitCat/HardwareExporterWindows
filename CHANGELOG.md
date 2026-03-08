# Changelog

All notable changes to this project will be documented in this file.

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
2. ✅ Metrics endpoint returns data: `http://localhost:9888/api/metrics`
3. ✅ Configuration changes are respected (try disabling GPU monitoring)
4. ✅ Service runs stably for extended period
5. ✅ Memory usage remains stable (no leaks)
6. ✅ CPU usage is low when idle
7. ✅ Prometheus can scrape the metrics successfully

## [Previous Versions]

See git history for previous changes.
