using System;
using System.Windows;
using WatercoolerTemp.Views;

namespace WatercoolerTemp;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs eventArgs)
	{
		base.OnStartup(eventArgs);

		try
		{
			MainWindow = new MainWindow();
			MainWindow.Show();
		}
		catch (Exception exception)
		{
			MessageBox.Show(
				exception.ToString(),
				"Falha ao iniciar o WatercoolerTemp",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			Shutdown(1);
		}
	}
}