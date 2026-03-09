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

    public TrayApplicationContext(AppConfig config)
    {
        _config = config;
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

        _cleaner.Start();
    }

    private static Icon LoadAppIcon()
    {
        // Try to load custom icon, fall back to system default
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

        var settingsItem = new ToolStripMenuItem("⚙️ Settings", null, (_, _) => ShowSettings());
        settingsItem.Font = new Font(settingsItem.Font, FontStyle.Bold);
        menu.Items.Add(settingsItem);

        menu.Items.Add("🔄 Run Scan Now", null, (_, _) => RunScanNow());
        menu.Items.Add(new ToolStripSeparator());

        var rulesCount = new ToolStripMenuItem($"Rules: {_config.Rules.Count}")
        {
            Enabled = false
        };
        menu.Items.Add(rulesCount);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("❌ Exit", null, (_, _) => ExitApp());

        return menu;
    }

    private void RefreshContextMenu()
    {
        _trayIcon.ContextMenuStrip?.Dispose();
        _trayIcon.ContextMenuStrip = BuildContextMenu();
    }

    private void ShowSettings()
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

    private void OnConfigSaved(AppConfig newConfig)
    {
        _config = newConfig;
        _cleaner.UpdateConfig(newConfig);
        AutoStartManager.SetEnabled(newConfig.AutoStart);
        ConfigManager.Save(newConfig);
        RefreshContextMenu();
    }

    private void RunScanNow()
    {
        int removed = _cleaner.ScanExisting();
        _trayIcon.ShowBalloonTip(
            2000,
            "Block From Recent",
            removed > 0
                ? $"Scan complete. Removed {removed} file(s)."
                : "Scan complete. No matching files found.",
            removed > 0 ? ToolTipIcon.Info : ToolTipIcon.None);
    }

    private void OnFileRemoved(string lnkPath, string targetPath)
    {
        // Optionally show notification (keep it non-intrusive)
    }

    private void ExitApp()
    {
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
