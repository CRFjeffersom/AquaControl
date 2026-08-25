using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace WatercoolerTemp.Core;

public sealed class CpuTemperatureReader : IDisposable
{
    private readonly Computer computer = new()
    {
        IsCpuEnabled = true
    };

    public void Open()
    {
        computer.Open();
    }

    public int? ReadTemperature()
    {
        foreach (IHardware hardware in computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu) continue;

            hardware.Update();

            ISensor? sensorPackage = hardware.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature && s.Name.Contains("Package"));

            if (sensorPackage?.Value is not null)
                return (int)Math.Round(sensorPackage.Value.Value);

            ISensor? qualquerSensor = hardware.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature && s.Value is not null);

            if (qualquerSensor?.Value is not null)
                return (int)Math.Round(qualquerSensor.Value.Value);
        }

        return null;
    }

    public void Dispose()
    {
        computer.Close();
    }
}