using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json;
using System.Collections.Immutable;
using Prometheus;

namespace HardwareExporterWindows
{
    public class Y : BackgroundService
    {
        public void Monitor()
        {
            Computer computer = new Computer
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
            var g1 = Metrics.CreateGauge("g1", "this is test gauge", "foo", "foo1");
            g1.WithLabels(new string[] { "bar","bar1" }).Set(114514);
            foreach (var hardware in computer.Hardware)
            {
                var prefix = $"{hardware.HardwareType.ToString().ToLower()}";
                var labels = new Dictionary<string, string>() { { "name", $"{hardware.Name}" } };
                Console.WriteLine("prefix: {0}, label: {1}", prefix, JsonConvert.SerializeObject(labels));

                if (prefix.StartsWith("hardware_gpu"))
                {
                    labels["vendor"] = prefix.Split("hardware_gpu")[1];
                    prefix = "hardware_gpu";
                }

                foreach (var subhardware in hardware.SubHardware)
                {
                    var subPrefix = $"{prefix}_{subhardware.HardwareType}";
                    var subLabels = labels.Append(new KeyValuePair<string, string>("sub_name", subhardware.Name)).ToImmutableDictionary();
                    Console.WriteLine("\tprefix: {0}, label: {1}", subPrefix, JsonConvert.SerializeObject(subLabels));

                    foreach (var sensor in subhardware.Sensors)
                    {
                        Console.WriteLine("\t\tSensor: {0}, value: {1}, type: {2}", sensor.Name, sensor.Value, sensor.SensorType);
                    }
                }

                foreach (ISensor sensor in hardware.Sensors)
                {

                    // var gauge = Metrics
                    var sensorLabels = labels.Append(new("type", sensor.SensorType.ToString())).ToImmutableDictionary();
                    //Console.WriteLine("\tmetric: {0}_{1}_{2}, value: {3}", prefix, sensor.SensorType.ToString().ToLower(), sensor.Name.ToLower(), sensor.Value);
                    Console.WriteLine("\tmetric: {0}_{1}_{2}, value: {3}, label: {4}", prefix,sensor.SensorType, sensor.Name.Replace(" ", "_").ToLower(), sensor.Value, JsonConvert.SerializeObject(sensorLabels));
                }

                if (hardware.HardwareType == HardwareType.Motherboard) break;
            }

            computer.Close();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested) 
            {
                Monitor();
                await Task.Delay(new TimeSpan(0, 0, 58), stoppingToken);
            }
        }
    }
}
