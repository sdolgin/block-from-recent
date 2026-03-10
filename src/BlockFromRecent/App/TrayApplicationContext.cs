using BlockFromRecent.Config;
using BlockFromRecent.Core;
using BlockFromRecent.Startup;

namespace BlockFromRecent.App;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly RecentFileCleaner _cleaner;
    private AppConfig _config;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext(AppConfig config, bool wasCorrupted = false)
    {
        _config = config;
        Log.Verbose = config.VerboseLogging;
        _cleaner = new RecentFileCleaner(config);
        _cleaner.OnFileRemoved += OnFileRemoved;

        _trayIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "Block From Recent",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        Log.Info("Tray icon created, starting cleaner");
        _cleaner.Start();

        if (wasCorrupted)
        {
            _trayIcon.ShowBalloonTip(
                5000,
                "Block From Recent",
                "Config file was corrupted and has been reset to defaults.\nA backup was saved as config.json.corrupt.",
                ToolTipIcon.Warning);
        }
    }

    private static Icon LoadAppIcon()
    {
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
        if (File.Exists(iconPath))
        {
            try { return new Icon(iconPath); }
            catch { }
        }

        return SystemIcons.Shield;
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("Settings", null, (_, _) => ShowSettings());
        settingsItem.Font = new Font(settingsItem.Font, FontStyle.Bold);
        menu.Items.Add(settingsItem);

        menu.Items.Add("Run Scan Now", null, (_, _) => RunScanNow());
        menu.Items.Add("Open Log File", null, (_, _) => OpenLogFile());
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(new ToolStripMenuItem($"Rules: {_config.Rules.Count}") { Enabled = false });
        var loggingItem = new ToolStripMenuItem($"Verbose: {(_config.VerboseLogging ? "ON" : "OFF")}") { Enabled = false };
        menu.Items.Add(loggingItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        return menu;
    }

    private void RefreshContextMenu()
    {
        _trayIcon.ContextMenuStrip?.Dispose();
        _trayIcon.ContextMenuStrip = BuildContextMenu();
    }

    private void ShowSettings()
    {
        try
        {
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.Activate();
                return;
            }

            _settingsForm = new SettingsForm(_config);
            _settingsForm.ConfigSaved += OnConfigSaved;
            _settingsForm.Show();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to show settings", ex);
        }
    }

    private void OnConfigSaved(AppConfig newConfig)
    {
        try
        {
            _config = newConfig;
            Log.Verbose = newConfig.VerboseLogging;
            _cleaner.UpdateConfig(newConfig);
            AutoStartManager.SetEnabled(newConfig.AutoStart);
            ConfigManager.Save(newConfig);
            RefreshContextMenu();
            Log.Info($"Config saved: {newConfig.Rules.Count} rules, AutoStart={newConfig.AutoStart}, VerboseLogging={newConfig.VerboseLogging}");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to save config", ex);
        }
    }

    private void RunScanNow()
    {
        try
        {
            Log.Info("Manual scan triggered");
            int removed = _cleaner.ScanExisting();
            Log.Info($"Manual scan complete: removed {removed} file(s)");
            _trayIcon.ShowBalloonTip(
                2000,
                "Block From Recent",
                removed > 0
                    ? $"Scan complete. Removed {removed} file(s)."
                    : "Scan complete. No matching files found.",
                removed > 0 ? ToolTipIcon.Info : ToolTipIcon.None);
        }
        catch (Exception ex)
        {
            Log.Error("RunScanNow failed", ex);
            _trayIcon.ShowBalloonTip(2000, "Block From Recent",
                $"Scan error: {ex.Message}", ToolTipIcon.Error);
        }
    }

    private void OnFileRemoved(string lnkPath, string targetPath)
    {
        Log.Info($"Removed: {Path.GetFileName(lnkPath)} -> {targetPath}");
    }

    private static void OpenLogFile()
    {
        try
        {
            string logPath = AppPaths.LogFile;
            if (File.Exists(logPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("Log file not found yet.\nIt will be created when the app logs its first entry.",
                    "Block From Recent", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to open log file", ex);
        }
    }

    private void ExitApp()
    {
        Log.Info("User requested exit");
        _cleaner.Stop();
        _cleaner.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cleaner.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
