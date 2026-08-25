using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WatercoolerTemp.Core;

namespace WatercoolerTemp.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const int IntervalMs = 1000;
    private const string PortName = "COM3";
    private readonly DispatcherTimer displayTimer;
    private WatercoolerMonitorService? service;
    private CancellationTokenSource? monitorCancellation;
    private Task? monitorTask;
    private string status = "DISCONNECTED";
    private string message = "READY";
    private double targetTemperature;
    private double displayedTemperature;
    private bool isConnected;

    public MainViewModel()
    {
        displayTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        displayTimer.Tick += AnimateDisplay;

        ConnectionCommand = new RelayCommand(ToggleConnection);
    }

    public RelayCommand ConnectionCommand { get; }

    public string Status
    {
        get => status;
        private set => SetField(ref status, value);
    }

    public string Message
    {
        get => message;
        private set => SetField(ref message, value);
    }

    public double DisplayedTemperature
    {
        get => displayedTemperature;
        private set
        {
            if (SetField(ref displayedTemperature, value))
            {
                OnPropertyChanged(nameof(TemperatureText));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(IndicatorBrush));
            }
        }
    }

    public string TemperatureText => $"{Math.Round(DisplayedTemperature):0}";
    public double Progress => Math.Clamp(DisplayedTemperature, 0, 100) / 100;
    public Brush IndicatorBrush => new SolidColorBrush(GetTemperatureColor(DisplayedTemperature));
    public Brush StatusBrush => IsConnected ? new SolidColorBrush(Color.FromRgb(83, 235, 151)) : new SolidColorBrush(Color.FromRgb(104, 116, 113));
    public bool IsConnected
    {
        get => isConnected;
        private set
        {
            if (SetField(ref isConnected, value))
            {
                OnPropertyChanged(nameof(StatusBrush));
                RefreshCommands();
            }
        }
    }

    public string ConnectionActionText => IsConnected ? "DISCONNECT" : "CONNECT";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ToggleConnection()
    {
        if (IsConnected)
            _ = DisconnectAsync();
        else
            Connect();
    }

    private void Connect()
    {
        try
        {
            var reader = new CpuTemperatureReader();
            var client = new Aqua240XSerialClient(PortName, 9600);
            service = new WatercoolerMonitorService(reader, client, TimeSpan.FromMilliseconds(IntervalMs));
            service.TemperatureSent += OnTemperatureSent;
            service.TemperatureUnavailable += OnTemperatureUnavailable;
            service.Error += OnServiceError;
            service.Open();
            IsConnected = true;
            Status = "CONNECTED";
            StartMonitoring();
            Message = $"{PortName} MONITORING";
        }
        catch (Exception exception)
        {
            service?.Dispose();
            service = null;
            Status = "DISCONNECTED";
            Message = exception.Message;
        }
    }

    private void StartMonitoring()
    {
        if (service is null)
            return;

        monitorCancellation = new CancellationTokenSource();
        monitorTask = Task.Run(() => service.StartAsync(monitorCancellation.Token), monitorCancellation.Token);
    }

    private async Task StopAsync()
    {
        if (monitorCancellation is null)
            return;

        monitorCancellation.Cancel();
        if (monitorTask is not null)
            await monitorTask;

        monitorCancellation.Dispose();
        monitorCancellation = null;
        monitorTask = null;
    }

    private async Task DisconnectAsync()
    {
        if (monitorCancellation is not null)
            await StopAsync();

        service?.Dispose();
        service = null;
        IsConnected = false;
        Status = "DISCONNECTED";
        Message = "READY";
    }

    private void OnTemperatureSent(int temperature, byte[] pacote)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            targetTemperature = Math.Clamp(temperature, 0, 100);
            displayTimer.Start();
            Message = $"SENT  {temperature:00} C";
        });
    }

    private void OnTemperatureUnavailable()
    {
        Application.Current.Dispatcher.Invoke(() => Message = "CPU SENSOR UNAVAILABLE");
    }

    private void OnServiceError(Exception exception)
    {
        Application.Current.Dispatcher.Invoke(() => Message = exception.Message);
    }

    private void AnimateDisplay(object? sender, EventArgs eventArgs)
    {
        double difference = targetTemperature - DisplayedTemperature;
        if (Math.Abs(difference) < 0.05)
        {
            DisplayedTemperature = targetTemperature;
            displayTimer.Stop();
            return;
        }

        DisplayedTemperature += difference * 0.14;
    }

    private static Color GetTemperatureColor(double temperature)
    {
        if (temperature <= 70)
            return Color.FromRgb(83, 235, 151);
        if (temperature <= 80)
            return Interpolate(Color.FromRgb(83, 235, 151), Color.FromRgb(245, 213, 73), (temperature - 70) / 10);
        if (temperature <= 90)
            return Interpolate(Color.FromRgb(245, 213, 73), Color.FromRgb(245, 137, 54), (temperature - 80) / 10);

        return Interpolate(Color.FromRgb(245, 137, 54), Color.FromRgb(255, 65, 75), (temperature - 90) / 10);
    }

    private static Color Interpolate(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));
    }

    public async void Dispose()
    {
        if (monitorCancellation is not null)
            await StopAsync();

        service?.Dispose();
        displayTimer.Stop();
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(ConnectionActionText));
        ConnectionCommand.Refresh();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}