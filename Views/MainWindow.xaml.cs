using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using WatercoolerTemp.Core;
using WatercoolerTemp.ViewModels;

namespace WatercoolerTemp.Views;

public partial class MainWindow : Window
{
    private readonly Forms.NotifyIcon trayIcon;
    private readonly Forms.ToolStripMenuItem showMenuItem;
    private readonly Forms.ToolStripMenuItem startMenuItem;
    private readonly Forms.ToolStripMenuItem stopMenuItem;
    private readonly MainViewModel viewModel;
    private bool isClosing;

    public MainViewModel ViewModel => viewModel;

    public MainWindow()
    {
        InitializeComponent();
        viewModel = new MainViewModel();
        DataContext = viewModel;

        showMenuItem = new Forms.ToolStripMenuItem("Mostrar");
        startMenuItem = new Forms.ToolStripMenuItem("Iniciar monitoramento");
        stopMenuItem = new Forms.ToolStripMenuItem("Parar monitoramento");
        showMenuItem.Click += (_, _) => ShowWindow();
        startMenuItem.Click += (_, _) => viewModel.StartMonitoring();
        stopMenuItem.Click += async (_, _) => await viewModel.StopMonitoringAsync();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(showMenuItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(startMenuItem);
        menu.Items.Add(stopMenuItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        var exitMenuItem = new Forms.ToolStripMenuItem("Sair");
        exitMenuItem.Click += (_, _) => Close();
        menu.Items.Add(exitMenuItem);

        trayIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "Aqua Control",
            ContextMenuStrip = menu,
            Visible = true
        };
        trayIcon.DoubleClick += (_, _) =>
        {
            if (IsVisible)
                HideToTray();
            else
                ShowWindowFromTray();
        };
        viewModel.PropertyChanged += ViewModelPropertyChanged;
        viewModel.HighTemperatureAlert += ShowHighTemperatureAlert;
        UpdateTrayState();
    }

    private void DragWindow(object sender, MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeWindow(object sender, RoutedEventArgs eventArgs)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseWindow(object sender, RoutedEventArgs eventArgs)
    {
        Close();
    }

    public void HideToTray()
    {
        WindowState = WindowState.Normal;
        Hide();
    }

    private void WindowStateChanged(object? sender, EventArgs eventArgs)
    {
        if (WindowState == WindowState.Minimized && !isClosing)
            HideToTray();
    }

    public void ShowWindowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void ShowWindow() => ShowWindowFromTray();

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(MainViewModel.DisplayedTemperature)
            or nameof(MainViewModel.IsConnected)
            or nameof(MainViewModel.IsMonitoring))
        {
            UpdateTrayState();
        }
    }

    private void UpdateTrayState()
    {
        trayIcon.Text = $"Aqua Control — {viewModel.TemperatureText} °C";
        startMenuItem.Enabled = viewModel.IsConnected && !viewModel.IsMonitoring;
        stopMenuItem.Enabled = viewModel.IsMonitoring;
    }

    private void ShowHighTemperatureAlert(int temperature)
    {
        trayIcon.ShowBalloonTip(
            5000,
            "Aqua Control - Temperatura alta",
            $"A temperatura do processador atingiu {temperature} °C.",
            Forms.ToolTipIcon.Warning);
    }

    private async void WindowClosing(object? sender, CancelEventArgs eventArgs)
    {
        if (isClosing)
            return;

        MessageBoxResult result = System.Windows.MessageBox.Show(
            this,
            "Deseja realmente fechar o AquaControl?",
            "Fechar programa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            eventArgs.Cancel = true;
            return;
        }

        eventArgs.Cancel = true;
        isClosing = true;
        await viewModel.DisposeAsync();
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
        viewModel.HighTemperatureAlert -= ShowHighTemperatureAlert;
        trayIcon.Visible = false;
        trayIcon.Dispose();
        Close();
    }
}