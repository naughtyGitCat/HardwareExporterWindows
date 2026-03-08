using LibreHardwareMonitor.Hardware;
using HardwareExporterWindows.Configuration;
using Microsoft.Extensions.Options;

namespace HardwareExporterWindows.Services;

/// <summary>
/// Singleton service for hardware monitoring
/// Initializes Computer once and reuses it for all metric collections
/// </summary>
public class HardwareMonitorService : IHostedService, IDisposable
{
    private readonly ILogger<HardwareMonitorService> _logger;
    private readonly HardwareMonitorOptions _options;
    private Computer? _computer;
    private readonly SemaphoreSlim _updateLock = new(1, 1);

    public HardwareMonitorService(
        ILogger<HardwareMonitorService> logger,
        IOptions<HardwareMonitorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing hardware monitor...");
            
            _computer = new Computer
            {
                IsCpuEnabled = _options.EnableCpu,
                IsGpuEnabled = _options.EnableGpu,
                IsMemoryEnabled = _options.EnableMemory,
                IsMotherboardEnabled = _options.EnableMotherboard,
                IsControllerEnabled = _options.EnableController,
                IsNetworkEnabled = _options.EnableNetwork,
                IsStorageEnabled = _options.EnableStorage
            };

            _computer.Open();
            
            _logger.LogInformation(
                "Hardware monitor initialized. Enabled: CPU={Cpu}, GPU={Gpu}, Memory={Memory}, " +
                "Motherboard={Motherboard}, Controller={Controller}, Network={Network}, Storage={Storage}",
                _options.EnableCpu, _options.EnableGpu, _options.EnableMemory,
                _options.EnableMotherboard, _options.EnableController, 
                _options.EnableNetwork, _options.EnableStorage);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize hardware monitor");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping hardware monitor...");
            _computer?.Close();
            _logger.LogInformation("Hardware monitor stopped");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping hardware monitor");
            throw;
        }
    }

    /// <summary>
    /// Update all hardware sensors
    /// Thread-safe operation
    /// </summary>
    public async Task UpdateAsync()
    {
        if (_computer == null)
        {
            throw new InvalidOperationException("Hardware monitor not initialized");
        }

        await _updateLock.WaitAsync();
        try
        {
            _computer.Accept(new UpdateVisitor());
        }
        finally
        {
            _updateLock.Release();
        }
    }

    /// <summary>
    /// Get the Computer instance
    /// </summary>
    public Computer GetComputer()
    {
        if (_computer == null)
        {
            throw new InvalidOperationException("Hardware monitor not initialized");
        }
        return _computer;
    }

    public void Dispose()
    {
        _computer?.Close();
        _updateLock.Dispose();
    }
}

/// <summary>
/// Visitor pattern implementation for updating hardware sensors
/// </summary>
public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor) { }
    
    public void VisitParameter(IParameter parameter) { }
}
