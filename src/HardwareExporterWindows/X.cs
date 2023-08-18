// // 张锐志 2023-08-18
using LibreHardwareMonitor.Hardware;
namespace HardwareExporterWindows;

public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }
    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
    }
    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}


public class X
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

        foreach (var hardware in computer.Hardware)
        {
            Console.WriteLine("Hardware: {0}", hardware.Name);
        
            foreach (var subhardware in hardware.SubHardware)
            {
                Console.WriteLine("\tSubhardware: {0}", subhardware.Name);
            
                foreach (var sensor in subhardware.Sensors)
                {
                    Console.WriteLine("\t\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
                }
            }

            foreach (ISensor sensor in hardware.Sensors)
            {
                Console.WriteLine("\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
            }
        }
    
        computer.Close();
    }
}