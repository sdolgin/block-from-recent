using BlockFromRecent.Config;
using BlockFromRecent.Core;
using BlockFromRecent.Startup;

namespace BlockFromRecent.App;

public class SettingsForm : Form
{
    private readonly ListBox _rulesListBox;
    private readonly Button _addPrefixBtn;
    private readonly Button _addGlobBtn;
    private readonly Button _editBtn;
    private readonly Button _removeBtn;
    private readonly CheckBox _autoStartCheckBox;
    private readonly CheckBox _scanOnStartupCheckBox;
    private readonly Button _testBtn;
    private readonly Button _saveBtn;
    private readonly Label _statusLabel;

    private AppConfig _config;

    public event Action<AppConfig>? ConfigSaved;

    public SettingsForm(AppConfig config)
    {
        _config = CloneConfig(config);
        InitializeFormProperties();

        // Rules list
        var rulesLabel = new Label
        {
            Text = "Exclusion Rules:",
            Location = new Point(12, 12),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold)
        };

        _rulesListBox = new ListBox
        {
            Location = new Point(12, 35),
            Size = new Size(430, 200),
            Font = new Font("Consolas", 9.5f)
        };

        // Buttons panel (right side)
        _addPrefixBtn = new Button
        {
            Text = "+ Path Prefix",
            Location = new Point(455, 35),
            Size = new Size(115, 30)
        };

        _addGlobBtn = new Button
        {
            Text = "+ Glob Pattern",
            Location = new Point(455, 70),
            Size = new Size(115, 30)
        };

        _editBtn = new Button
        {
            Text = "Edit",
            Location = new Point(455, 110),
            Size = new Size(115, 30)
        };

        _removeBtn = new Button
        {
            Text = "Remove",
            Location = new Point(455, 145),
            Size = new Size(115, 30)
        };

        _testBtn = new Button
        {
            Text = "🔍 Test Rules",
            Location = new Point(455, 195),
            Size = new Size(115, 30)
        };

        // Options
        _autoStartCheckBox = new CheckBox
        {
            Text = "Start with Windows",
            Location = new Point(12, 250),
            AutoSize = true,
            Checked = _config.AutoStart
        };

        _scanOnStartupCheckBox = new CheckBox
        {
            Text = "Scan existing files on startup",
            Location = new Point(12, 275),
            AutoSize = true,
            Checked = _config.ScanOnStartup
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "",
            Location = new Point(12, 310),
            Size = new Size(430, 20),
            ForeColor = Color.DarkGreen
        };

        // Save button
        _saveBtn = new Button
        {
            Text = "💾 Save",
            Location = new Point(455, 305),
            Size = new Size(115, 35),
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold)
        };

        // Add controls
        Controls.AddRange(new Control[]
        {
            rulesLabel, _rulesListBox,
            _addPrefixBtn, _addGlobBtn, _editBtn, _removeBtn, _testBtn,
            _autoStartCheckBox, _scanOnStartupCheckBox,
            _statusLabel, _saveBtn
        });

        // Wire events
        _addPrefixBtn.Click += (_, _) => AddRule(RuleType.PathPrefix);
        _addGlobBtn.Click += (_, _) => AddRule(RuleType.GlobPattern);
        _editBtn.Click += (_, _) => EditRule();
        _removeBtn.Click += (_, _) => RemoveRule();
        _testBtn.Click += (_, _) => TestRules();
        _saveBtn.Click += (_, _) => SaveConfig();

        RefreshRulesList();
    }

    private void InitializeFormProperties()
    {
        Text = "Block From Recent — Settings";
        Size = new Size(595, 385);
        MinimumSize = new Size(595, 385);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Shield;
    }

    private void RefreshRulesList()
    {
        _rulesListBox.Items.Clear();
        foreach (var rule in _config.Rules)
            _rulesListBox.Items.Add(rule);
    }

    private void AddRule(RuleType type)
    {
        string title = type == RuleType.PathPrefix
            ? "Add Path Prefix"
            : "Add Glob Pattern";
        string hint = type == RuleType.PathPrefix
            ? "Enter a path prefix (e.g., D:\\Private\\ or \\\\server\\share\\):"
            : "Enter a glob pattern (e.g., *.mp4, **\\temp\\*):";

        string? input = PromptInput(title, hint);
        if (string.IsNullOrWhiteSpace(input))
            return;

        _config.Rules.Add(new ExclusionRule { Pattern = input, Type = type });
        RefreshRulesList();
        _statusLabel.Text = "Rule added (not yet saved).";
    }

    private void EditRule()
    {
        if (_rulesListBox.SelectedIndex < 0)
            return;

        var rule = _config.Rules[_rulesListBox.SelectedIndex];
        string title = rule.Type == RuleType.PathPrefix ? "Edit Path Prefix" : "Edit Glob Pattern";
        string? input = PromptInput(title, "Edit the pattern:", rule.Pattern);
        if (input == null)
            return;

        rule.Pattern = input;
        RefreshRulesList();
        _statusLabel.Text = "Rule updated (not yet saved).";
    }

    private void RemoveRule()
    {
        if (_rulesListBox.SelectedIndex < 0)
            return;

        _config.Rules.RemoveAt(_rulesListBox.SelectedIndex);
        RefreshRulesList();
        _statusLabel.Text = "Rule removed (not yet saved).";
    }

    private void TestRules()
    {
        var engine = new ExclusionEngine();
        engine.UpdateRules(_config.Rules);

        string recentPath = RecentFileWatcher.RecentFolderPath;
        int matchCount = 0;
        int total = 0;

        try
        {
            foreach (var lnkFile in Directory.GetFiles(recentPath, "*.lnk"))
            {
                total++;
                string? target = ShortcutResolver.ResolveTarget(lnkFile);
                if (target != null && engine.IsExcluded(target))
                    matchCount++;
            }
        }
        catch { }

        _statusLabel.Text = $"Test: {matchCount} of {total} recent files would be removed.";
        _statusLabel.ForeColor = matchCount > 0 ? Color.DarkOrange : Color.DarkGreen;
    }

    private void SaveConfig()
    {
        _config.AutoStart = _autoStartCheckBox.Checked;
        _config.ScanOnStartup = _scanOnStartupCheckBox.Checked;

        ConfigSaved?.Invoke(_config);
        _statusLabel.Text = "Settings saved.";
        _statusLabel.ForeColor = Color.DarkGreen;
    }

    private static string? PromptInput(string title, string prompt, string defaultValue = "")
    {
        using var form = new Form
        {
            Text = title,
            Size = new Size(500, 170),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
        var textBox = new TextBox
        {
            Text = defaultValue,
            Location = new Point(12, 40),
            Size = new Size(460, 25)
        };

        var okBtn = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(316, 80),
            Size = new Size(75, 30)
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(397, 80),
            Size = new Size(75, 30)
        };

        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;
        form.Controls.AddRange(new Control[] { label, textBox, okBtn, cancelBtn });

        return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
    }

    private static AppConfig CloneConfig(AppConfig source)
    {
        return new AppConfig
        {
            AutoStart = source.AutoStart,
            ScanOnStartup = source.ScanOnStartup,
            Rules = source.Rules.Select(r => new ExclusionRule
            {
                Pattern = r.Pattern,
                Type = r.Type
            }).ToList()
        };
    }
}
