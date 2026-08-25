using Microsoft.Win32;
using System.Diagnostics;

namespace WatercoolerTemp.Core;

public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WatercoolerTemp";
    private const string StartupArgument = "--startup";

    public static bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            string executablePath = Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("Não foi possível localizar o executável da aplicação.");
            key.SetValue(ValueName, $"\"{executablePath}\" {StartupArgument}");
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }

    public static bool IsStartupLaunch(string[] args) =>
        args.Any(argument => string.Equals(argument, StartupArgument, StringComparison.OrdinalIgnoreCase));
}