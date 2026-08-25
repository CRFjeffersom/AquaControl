using System.IO.Ports;

namespace WatercoolerTemp.Core;

public sealed class Aqua240XSerialClient : IDisposable
{
    private readonly SerialPort porta;

    public Aqua240XSerialClient(string portaCom = "COM3", int baudRate = 9600)
    {
        porta = new SerialPort(portaCom, baudRate)
        {
            WriteTimeout = 1000
        };
    }

    public string PortName => porta.PortName;

    public void Open()
    {
        porta.Open();
    }

    public void Send(byte[] pacote)
    {
        porta.Write(pacote, 0, pacote.Length);
    }

    public void Dispose()
    {
        if (porta.IsOpen)
            porta.Close();

        porta.Dispose();
    }
}