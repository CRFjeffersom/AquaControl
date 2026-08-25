using System.Windows;
using System.Windows.Input;
using WatercoolerTemp.ViewModels;

namespace WatercoolerTemp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
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

    private void WindowClosed(object? sender, EventArgs eventArgs)
    {
        (DataContext as MainViewModel)?.Dispose();
    }
}