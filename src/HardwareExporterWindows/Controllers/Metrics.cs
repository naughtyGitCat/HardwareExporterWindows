using LibreHardwareMonitor.Hardware;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using HardwareExporterWindows.Services;

namespace HardwareExporterWindows.Controllers;

[Route("[controller]")]
[ApiController]
public class MetricsController : ControllerBase
{
    private readonly ILogger<MetricsController> _logger;
    private readonly HardwareMonitorService _hardwareMonitor;

    public MetricsController(
        ILogger<MetricsController> logger,
        HardwareMonitorService hardwareMonitor)
    {
        _logger = logger;
        _hardwareMonitor = hardwareMonitor;
    }

    /// <summary>
    /// Remove duplicate consecutive elements from metric name
    /// Example: "cpu_cpu_temperature" -> "cpu_temperature"
    /// </summary>
    private string TrimDuplicateElements(string metricName)
    {
        var rawElements = metricName.Split("_");
        var newElements = new List<string>();
        
        for (int i = 0; i < rawElements.Length; i++)
        {
            if (i == 0 || rawElements[i] != rawElements[i - 1])
            {
                newElements.Add(rawElements[i]);
            }
        }
        
        return string.Join("_", newElements);
    }

    /// <summary>
    /// Process sensor name to extract metric name and labels
    /// Handles special characters and numbered sensors
    /// </summary>
    /// <param name="sensorName">Raw sensor name from hardware</param>
    /// <returns>Tuple of (metric_name, labels)</returns>
    private (string MetricName, IDictionary<string, string> Labels) ProcessSensorName(string sensorName)
    {
        // Handle voltage signs: +3.3V -> positive_3_3v
        sensorName = sensorName
            .Replace("+", "positive_")
            .Replace("-", "negative_");

        var sensorLabels = new Dictionary<string, string>();
        
        // Pattern to match numbered sensors like "Core #0" or "Fan 1"
        var pattern = @"\s#?\d";
        var matchCount = Regex.Count(sensorName, pattern);
        
        if (matchCount == 0)
        {
            // No numbered sensors, just replace spaces with underscores
            return (sensorName.Replace(" ", "_"), sensorLabels);
        }

        // Extract numbers and create labels
        var elements = sensorName.Split(" ");
        var newMetricElements = new List<string>();
        
        for (int i = 0; i < elements.Length; i++)
        {
            var numberMatch = Regex.Match(elements[i], @"\d+");
            
            // If this element ends with a number and we have a previous element
            if (Regex.Match(elements[i], @"#?\d+$").Success && numberMatch.Success && i > 0)
            {
                // Use previous element as label key, number as value
                // Example: "Core #0" -> label: core="0"
                sensorLabels[elements[i - 1].ToLower()] = numberMatch.Value;
            }
            else
            {
                newMetricElements.Add(elements[i]);
            }
        }
        
        var newMetricName = string.Join("_", newMetricElements);
        
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Processed sensor name: {Original} -> {Metric}, Labels: {Labels}",
                sensorName, newMetricName, JsonConvert.SerializeObject(sensorLabels));
        }
        
        return (newMetricName, sensorLabels);
    }

    /// <summary>
    /// Generate Prometheus metrics from hardware sensors and .NET runtime
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> GetMetrics()
    {
        try
        {
            _logger.LogDebug("Starting metrics collection");
            
            var metricsBuilder = new System.Text.StringBuilder();
            
            // First, collect .NET runtime metrics from prometheus-net
            try
            {
                var registry = Prometheus.Metrics.DefaultRegistry;
                using var stream = new System.IO.MemoryStream();
                await registry.CollectAndExportAsTextAsync(stream);
                stream.Position = 0;
                using var reader = new System.IO.StreamReader(stream);
                var dotnetMetrics = await reader.ReadToEndAsync();
                metricsBuilder.AppendLine(dotnetMetrics);
                metricsBuilder.AppendLine(); // Add blank line separator
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect .NET runtime metrics, continuing with hardware metrics only");
            }
            
            // Then, collect hardware metrics
            await _hardwareMonitor.UpdateAsync();
            
            var computer = _hardwareMonitor.GetComputer();
            
            foreach (var hardware in computer.Hardware)
            {
                ProcessHardware(hardware, metricsBuilder);
            }
            
            var result = metricsBuilder.ToString();
            _logger.LogDebug("Metrics collection completed, {Size} bytes", result.Length);
            
            return Content(result, "text/plain; version=0.0.4");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect hardware metrics");
            return StatusCode(500, "# ERROR: Failed to collect metrics\n");
        }
    }

    private void ProcessHardware(IHardware hardware, System.Text.StringBuilder metricsBuilder)
    {
        var prefix = $"hardware_{hardware.HardwareType.ToString().ToLower()}";
        var hardwareLabels = new Dictionary<string, string> { { "name", hardware.Name } };
        
        // Handle GPU vendor labels
        if (prefix.StartsWith("hardware_gpu"))
        {
            var vendor = prefix.Split("hardware_gpu")[1];
            if (!string.IsNullOrEmpty(vendor))
            {
                hardwareLabels["vendor"] = vendor;
            }
            prefix = "hardware_gpu";
        }

        // Process sub-hardware (e.g., CPU cores)
        foreach (var subhardware in hardware.SubHardware)
        {
            ProcessSubHardware(subhardware, prefix, hardwareLabels, metricsBuilder);
        }

        // Process hardware sensors
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value == null) continue;
            
            ProcessSensor(sensor, prefix, hardwareLabels, metricsBuilder);
        }
    }

    private void ProcessSubHardware(
        IHardware subhardware,
        string parentPrefix,
        Dictionary<string, string> parentLabels,
        System.Text.StringBuilder metricsBuilder)
    {
        var subPrefix = $"{parentPrefix}_{subhardware.HardwareType.ToString().ToLower()}";
        var subHardwareLabels = new Dictionary<string, string>(parentLabels)
        {
            ["sub_name"] = subhardware.Name
        };

        foreach (var sensor in subhardware.Sensors)
        {
            if (sensor.Value == null) continue;
            
            ProcessSensor(sensor, subPrefix, subHardwareLabels, metricsBuilder);
        }
    }

    private void ProcessSensor(
        ISensor sensor,
        string prefix,
        Dictionary<string, string> baseLabels,
        System.Text.StringBuilder metricsBuilder)
    {
        var (sensorName, sensorLabels) = ProcessSensorName(sensor.Name.ToLower());
        
        // Combine base labels with sensor-specific labels
        var allLabels = baseLabels
            .Concat(sensorLabels)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // Build metric name
        var metricName = $"{prefix}_{sensor.SensorType.ToString().ToLower()}_{sensorName}";
        metricName = TrimDuplicateElements(metricName);
        
        // Render labels
        var labelsRendered = string.Empty;
        if (allLabels.Any())
        {
            labelsRendered = string.Join(", ", 
                allLabels.Select(l => $"{l.Key}=\"{l.Value}\""));
            labelsRendered = $"{{{labelsRendered}}}";
        }
        
        // Output Prometheus format
        metricsBuilder.AppendLine($"# HELP {metricName} Sensor: {sensor.Name}, Type: {sensor.SensorType}");
        metricsBuilder.AppendLine($"# TYPE {metricName} gauge");
        metricsBuilder.AppendLine($"{metricName}{labelsRendered} {sensor.Value}");
    }
}
