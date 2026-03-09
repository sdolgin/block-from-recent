using Microsoft.Win32;

namespace BlockFromRecent.Startup;

public static class AutoStartManager
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "BlockFromRecent";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
    }

    public static void Enable()
    {
        string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.SetValue(AppName, $"\"{exePath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(AppName, false);
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
            Enable();
        else
            Disable();
    }
}
