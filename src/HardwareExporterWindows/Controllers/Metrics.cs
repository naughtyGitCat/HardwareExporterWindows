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
    /// Sanitize a metric name to contain only Prometheus-allowed characters: [a-zA-Z_:][a-zA-Z0-9_:]*
    /// Replaces any disallowed character with underscore, then collapses consecutive underscores.
    /// </summary>
    private static string SanitizeMetricName(string name)
    {
        // Replace any character not in [a-zA-Z0-9_:] with underscore
        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_:]", "_");
        // Collapse consecutive underscores
        sanitized = Regex.Replace(sanitized, @"_{2,}", "_");
        // Trim leading/trailing underscores
        return sanitized.Trim('_');
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

        // Extract parenthesized suffixes as labels before further processing
        // e.g. "Core (SMU)" -> name="Core", extra_info label="SMU"
        // e.g. "Core (Tctl/Tdie)" -> name="Core", extra_info label="Tctl_Tdie"
        var sensorLabels = new Dictionary<string, string>();
        var parenMatch = Regex.Match(sensorName, @"\s*\(([^)]+)\)\s*$");
        if (parenMatch.Success)
        {
            var parenContent = parenMatch.Groups[1].Value;
            // Sanitize the parenthesized content for use as label value
            parenContent = parenContent.Replace("/", "_");
            sensorLabels["sensor_info"] = parenContent.Trim();
            sensorName = sensorName[..parenMatch.Index].Trim();
        }

        // Pattern to match numbered sensors like "Core #0" or "Fan 1"
        var pattern = @"\s#?\d";
        var matchCount = Regex.Count(sensorName, pattern);

        if (matchCount == 0)
        {
            // No numbered sensors, just replace spaces with underscores
            return (SanitizeMetricName(sensorName.Replace(" ", "_")), sensorLabels);
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

        var newMetricName = SanitizeMetricName(string.Join("_", newMetricElements));

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
                // Normalize line endings to Unix LF (prometheus-net may output CRLF on Windows)
                dotnetMetrics = dotnetMetrics.Replace("\r\n", "\n").Replace("\r", "\n");
                metricsBuilder.Append(dotnetMetrics);
                metricsBuilder.Append("\n\n"); // Add blank line separator (Unix LF)
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect .NET runtime metrics, continuing with hardware metrics only");
            }
            
            // Then, collect hardware metrics
            await _hardwareMonitor.UpdateAsync();

            var computer = _hardwareMonitor.GetComputer();
            var emittedMetrics = new HashSet<string>();

            foreach (var hardware in computer.Hardware)
            {
                ProcessHardware(hardware, metricsBuilder, emittedMetrics);
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

    private void ProcessHardware(IHardware hardware, System.Text.StringBuilder metricsBuilder, HashSet<string> emittedMetrics)
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

        // Sanitize prefix (e.g. HardwareType might contain invalid chars)
        prefix = SanitizeMetricName(prefix);

        // Process sub-hardware (e.g., CPU cores)
        foreach (var subhardware in hardware.SubHardware)
        {
            ProcessSubHardware(subhardware, prefix, hardwareLabels, metricsBuilder, emittedMetrics);
        }

        // Process hardware sensors
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value == null) continue;

            ProcessSensor(sensor, prefix, hardwareLabels, metricsBuilder, emittedMetrics);
        }
    }

    private void ProcessSubHardware(
        IHardware subhardware,
        string parentPrefix,
        Dictionary<string, string> parentLabels,
        System.Text.StringBuilder metricsBuilder,
        HashSet<string> emittedMetrics)
    {
        var subPrefix = $"{parentPrefix}_{SanitizeMetricName(subhardware.HardwareType.ToString().ToLower())}";
        var subHardwareLabels = new Dictionary<string, string>(parentLabels)
        {
            ["sub_name"] = subhardware.Name
        };

        foreach (var sensor in subhardware.Sensors)
        {
            if (sensor.Value == null) continue;

            ProcessSensor(sensor, subPrefix, subHardwareLabels, metricsBuilder, emittedMetrics);
        }
    }

    private void ProcessSensor(
        ISensor sensor,
        string prefix,
        Dictionary<string, string> baseLabels,
        System.Text.StringBuilder metricsBuilder,
        HashSet<string> emittedMetrics)
    {
        var (sensorName, sensorLabels) = ProcessSensorName(sensor.Name.ToLower());

        // Combine base labels with sensor-specific labels
        var allLabels = baseLabels
            .Concat(sensorLabels)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Build metric name and ensure it's fully sanitized
        var metricName = $"{prefix}_{sensor.SensorType.ToString().ToLower()}_{sensorName}";
        metricName = SanitizeMetricName(TrimDuplicateElements(metricName));

        // Render labels
        var labelsRendered = string.Empty;
        if (allLabels.Any())
        {
            labelsRendered = string.Join(", ",
                allLabels.Select(l => $"{l.Key}=\"{l.Value}\""));
            labelsRendered = $"{{{labelsRendered}}}";
        }

        // Only emit HELP and TYPE once per metric name (Prometheus requires no duplicates)
        if (emittedMetrics.Add(metricName))
        {
            metricsBuilder.Append($"# HELP {metricName} Sensor: {sensor.Name}, Type: {sensor.SensorType}\n");
            metricsBuilder.Append($"# TYPE {metricName} gauge\n");
        }
        // Output Prometheus format (use Unix LF instead of Windows CRLF)
        metricsBuilder.Append($"{metricName}{labelsRendered} {sensor.Value}\n");
    }
}
