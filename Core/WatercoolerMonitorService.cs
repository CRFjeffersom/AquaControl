namespace WatercoolerTemp.Core;

public sealed class WatercoolerMonitorService : IDisposable
{
    private readonly CpuTemperatureReader temperatureReader;
    private readonly Aqua240XSerialClient serialClient;
    private readonly TimeSpan interval;

    public WatercoolerMonitorService(
        CpuTemperatureReader temperatureReader,
        Aqua240XSerialClient serialClient,
        TimeSpan interval)
    {
        this.temperatureReader = temperatureReader;
        this.serialClient = serialClient;
        this.interval = interval;
    }

    public event Action<int, byte[]>? TemperatureSent;
    public event Action? TemperatureUnavailable;
    public event Action<Exception>? Error;

    public void Open()
    {
        temperatureReader.Open();
        serialClient.Open();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int? temperature = temperatureReader.ReadTemperature();

                if (temperature is null)
                {
                    TemperatureUnavailable?.Invoke();
                }
                else
                {
                    byte[] pacote = Aqua240XProtocol.MontarPacote(temperature.Value);
                    serialClient.Send(pacote);
                    TemperatureSent?.Invoke(temperature.Value, pacote);
                }

                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
            throw;
        }
    }

    public void Dispose()
    {
        serialClient.Dispose();
        temperatureReader.Dispose();
    }
}