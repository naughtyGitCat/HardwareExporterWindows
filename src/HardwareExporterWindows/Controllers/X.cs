// // 张锐志 2023-08-18
using LibreHardwareMonitor.Hardware;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using System.Text.RegularExpressions;

namespace HardwareExporterWindows.Controllers;


public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }
    public void VisitHardware(IHardware hardware)
    {
        hardware.Traverse(this);
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
    }
    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}


[Route("api/[controller]")]
public class XController : ControllerBase
{
    private string TrimDuplicateElments(string metricName)
    {
        var rawElements = metricName.Split("_");
        var newElements = new List<string>();
        for (int i = 0; i < rawElements.Length; i++)
        {
            if (i == 0)
            {
                newElements.Add(rawElements[i]);
            }
            else
            {
                if (rawElements[i] != rawElements[i - 1])
                {
                    newElements.Add(rawElements[i]);
                }
            }
        }
        return string.Join("_", newElements);
    }
    private (string, IDictionary<string, string>) Process(string metricName)
    {
        // +3.3v
        metricName = metricName.Replace("+", "positive_").Replace("-", "negative_");

        var sensorLabels = new Dictionary<string, string>();
        var pattern = @"\s#?\d";
        var c = Regex.Count(metricName, pattern);
        if (c == 0)
        {
            return (metricName.Replace(" ", "_"), sensorLabels);
        }


        var elements = metricName.Split(" ");
        var newMetricElements = new List<string> { };
        for (int i = 0; i < elements.Length; i++)
        {
            // Console.WriteLine($"element: {elements[i]}");
            var re = Regex.Match(elements[i], @"\d+");
            if (Regex.Match(elements[i], @"#?\d+$").Success && re.Success)
            {
                // Console.WriteLine("number triggered");
                sensorLabels[elements[i - 1]] = re.Value;
            }
            else
            {
                newMetricElements.Add(elements[i]);
            }
        }
        var newMetricName = string.Join("_", newMetricElements);
        Console.WriteLine($"in Process, metric: {newMetricName}, labels: {JsonConvert.SerializeObject(sensorLabels)}");
        return (newMetricName, sensorLabels);
    }

    [HttpGet]
    public string Monitor()
    {
        var ret = string.Empty;
        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true
        };

        computer.Open();
        computer.Accept(new UpdateVisitor());
        foreach (var hardware in computer.Hardware)
        {
            var prefix = $"hardware_{hardware.HardwareType.ToString().ToLower()}";
            var hardwareLabels = new Dictionary<string, string>() { { "name", $"{hardware.Name}" } };
            if (prefix.StartsWith("hardware_gpu"))
            {
                hardwareLabels["vendor"] = prefix.Split("hardware_gpu")[1];
                prefix = "hardware_gpu";
            }

            foreach (var subhardware in hardware.SubHardware)
            {
                var subPrefix = $"{prefix}_{subhardware.HardwareType.ToString().ToLower()}";
                var subHardwareLabels = new Dictionary<string, string>(hardwareLabels.Append(new("sub_name", subhardware.Name)));

                foreach (var sensor in subhardware.Sensors)
                {
                    if (sensor.Value is null)
                    {
                        continue;
                    }
                    Console.WriteLine($"hardwareLabels: {JsonConvert.SerializeObject(hardwareLabels)}");
                    Console.WriteLine($"subHardwareLabels: {JsonConvert.SerializeObject(subHardwareLabels)}");
                    var (sensorName, pureSensorLabels) = Process(sensor.Name.ToLower());
                    var sensorLabels = subHardwareLabels.ToList().Concat(pureSensorLabels.ToList());
                    Console.WriteLine($"sensorLabels: {JsonConvert.SerializeObject(sensorLabels)}");
                    var subMetricName = $"{subPrefix}_{sensor.SensorType.ToString().ToLower()}_{sensorName}";
                    subMetricName = TrimDuplicateElments(subMetricName);
                    ret += $"# HELP {subMetricName} sensor identifier: {sensor.Identifier}\n";
                    ret += $"# TYPE {subMetricName} gauge\n";
                    var sensorLabelsRendered = string.Empty;
                    if (sensorLabels.Any())
                    {
                        sensorLabelsRendered = string.Join(", ", sensorLabels.Select(l => $"{l.Key}=\"{l.Value}\""));
                        sensorLabelsRendered = $"{{{sensorLabelsRendered}}}";
                    }
                    ret += $"{subMetricName}{sensorLabelsRendered} {sensor.Value}\n";

                }
            }

            foreach (ISensor sensor in hardware.Sensors)
            {
                if (sensor.Value is null)
                {
                    continue;
                }
                var (sensorName, pureSensorLabels) = Process(sensor.Name.ToLower());
                var sensorLabels = hardwareLabels.ToList().Concat(pureSensorLabels.ToList());
                var metricName = $"{prefix}_{sensor.SensorType.ToString().ToLower()}_{sensorName}";
                metricName = TrimDuplicateElments(metricName);
                ret += $"# HELP {metricName} sensor type: {sensor.SensorType}\n";
                ret += $"# TYPE {metricName} gauge\n";
                var sensorLabelsRendered = string.Empty;
                if (sensorLabels.Any())
                {
                    sensorLabelsRendered = string.Join(", ", sensorLabels.Select(l => $"{l.Key}=\"{l.Value}\""));
                    sensorLabelsRendered = $"{{{sensorLabelsRendered}}}";
                }
                ret += $"{metricName}{sensorLabelsRendered} {sensor.Value}\n";

            }
        }

        computer.Close();
        return ret;
    }
}