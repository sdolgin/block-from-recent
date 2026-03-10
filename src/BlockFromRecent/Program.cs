using BlockFromRecent.App;
using BlockFromRecent.Config;
using BlockFromRecent.Core;

namespace BlockFromRecent;

static class Program
{
    private const string MutexName = "Global\\BlockFromRecent_SingleInstance";

    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            Log.Error("Unhandled UI thread exception", e.Exception);
            MessageBox.Show(
                $"An error occurred:\n{e.Exception.Message}\n\nSee block-from-recent.log for details.",
                "Block From Recent — Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Log.Error("Unhandled domain exception", ex);
        };

        Log.TrimIfNeeded();
        Log.Info("Application starting");

        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Log.Info("Another instance is already running — exiting");
            MessageBox.Show(
                "Block From Recent is already running.\nCheck the system tray.",
                "Block From Recent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var (config, wasCorrupted) = ConfigManager.Load();
        Log.Info($"Config loaded: {config.Rules.Count} rules, AutoStart={config.AutoStart}, ScanOnStartup={config.ScanOnStartup}");

        Application.Run(new TrayApplicationContext(config, wasCorrupted));
        Log.Info("Application exiting");
    }
}