using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

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
            return key?.GetValue(ValueName) is string value
                && TryGetExecutablePath(value, out string executablePath)
                && File.Exists(executablePath);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            string executablePath = Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName
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

    private static bool TryGetExecutablePath(string command, out string executablePath)
    {
        executablePath = string.Empty;
        if (!command.StartsWith('"'))
            return false;

        int closingQuote = command.IndexOf('"', 1);
        if (closingQuote <= 1)
            return false;

        executablePath = command[1..closingQuote];
        return true;
    }
}