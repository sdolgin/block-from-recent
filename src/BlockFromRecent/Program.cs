using BlockFromRecent.App;
using BlockFromRecent.Config;

namespace BlockFromRecent;

static class Program
{
    private const string MutexName = "Global\\BlockFromRecent_SingleInstance";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running
            MessageBox.Show(
                "Block From Recent is already running.\nCheck the system tray.",
                "Block From Recent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var config = ConfigManager.Load();
        Application.Run(new TrayApplicationContext(config));
    }
}