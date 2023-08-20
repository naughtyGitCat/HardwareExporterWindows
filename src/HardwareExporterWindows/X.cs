// // 张锐志 2023-08-18
using LibreHardwareMonitor.Hardware;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using Prometheus;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace HardwareExporterWindows;


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
    private Tuple<string,IDictionary<string, string>> Process(string metricName, IDictionary<string, string> labels) 
    {
        var pattern = @"\s#?\d";
        var c = Regex.Count(metricName, pattern);
        if (c == 0)
        {
            return Tuple.Create(metricName, labels);
        }
        Console.WriteLine($"metric: {metricName}, c: {c}");

        var elements = metricName.Split(" ");
        var numPattern = @"\d+";
        for (int i = 0;i< elements.Length; i++)
        {
            var re = Regex.Match(elements[i], numPattern);
            if (re.Success)
            {
                elements.
                labels[elements[i - 1]] = re.Value;
            }                         
        }
        return new Tuple<string, IDictionary<string, string>>(metricName, labels);
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
            var labels = new Dictionary<string, string>() { { "name", $"{hardware.Name}" } };
            if (prefix.StartsWith("hardware_gpu"))
            {
                labels["vendor"] = prefix.Split("hardware_gpu")[1];
                prefix = "hardware_gpu";
            }

            foreach (var subhardware in hardware.SubHardware)
            {
                var subPrefix = $"{prefix}_{subhardware.HardwareType.ToString().ToLower()}";
                var subLabels = labels.Append(new KeyValuePair<string, string>("sub_name", subhardware.Name)).ToImmutableDictionary();
                //  Console.WriteLine("\tprefix: {0}, label: {1}", subPrefix, subLabels);
                foreach (var sensor in subhardware.Sensors)
                {
                    var subMetricName = $"{subPrefix}_{sensor.SensorType.ToString().ToLower()}_{sensor.Name.ToLower()}";
                    ret += $"# HELP {subMetricName} sensor identifier: {sensor.Identifier}\n";
                    ret += $"# TYPE {subMetricName} gauge\n";
                    var sensorLabelsRendered = string.Empty;
                    if (subLabels.Any())
                    {
                        sensorLabelsRendered = string.Join(", ", subLabels.Select(l => $"{l.Key}=\"{l.Value}\""));
                        sensorLabelsRendered = $"{{{sensorLabelsRendered}}}";
                    }
                    ret += $"{subMetricName}{sensorLabelsRendered} {sensor.Value}\n";
                    // Console.WriteLine("\t\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
                }
            }

            foreach (ISensor sensor in hardware.Sensors)
            {
                
                // var gauge = Metrics
                var sensorLabels = labels.Append(new("type", sensor.SensorType.ToString())).ToImmutableDictionary();
                //Console.WriteLine("\tmetric: {0}_{1}_{2}, value: {3}", prefix, sensor.SensorType.ToString().ToLower(), sensor.Name.ToLower(), sensor.Value);
                // Console.WriteLine("\tmetric: {0}_{1}, value: {2}, label: {3}", prefix, sensor.Name.Replace(" ", "_").ToLower(), sensor.Value, JsonConvert.SerializeObject(sensorLabels));
                var metricName = $"{prefix}_{sensor.Name.ToLower()}";
                ret += $"# HELP {metricName} sensor type: {sensor.SensorType}\n";
                ret += $"# TYPE {metricName} gauge\n";
                var sensorLabelsRendered = string.Empty ;
                if (sensorLabels.Any()) 
                {
                    sensorLabelsRendered = string.Join(", ", sensorLabels.Select(l=>$"{l.Key}=\"{l.Value}\""));
                    sensorLabelsRendered = $"{{{sensorLabelsRendered}}}";
                }
                ret += $"{metricName}{sensorLabelsRendered} {sensor.Value}\n";
                Process(metricName, sensorLabels);
            }
        }
    
        computer.Close();
        return ret;
    }
}