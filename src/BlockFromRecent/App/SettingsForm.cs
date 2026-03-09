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

        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Block From Recent — Settings";
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Shield;

        // Use TableLayoutPanel for DPI-aware layout
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // row 0: label
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220)); // row 1: listbox + buttons
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // row 2: checkboxes
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // row 3: status
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // row 4: save

        // Row 0: Label
        var rulesLabel = new Label
        {
            Text = "Exclusion Rules:",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 4)
        };
        layout.Controls.Add(rulesLabel, 0, 0);
        layout.SetColumnSpan(rulesLabel, 2);

        // Row 1 left: ListBox
        _rulesListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5f),
            IntegralHeight = false
        };
        layout.Controls.Add(_rulesListBox, 0, 1);

        // Row 1 right: Button panel
        var btnPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(4, 0, 0, 0)
        };

        _addPrefixBtn = new Button { Text = "+ Path", AutoSize = true, MinimumSize = new Size(120, 32) };
        _addGlobBtn = new Button { Text = "+ Glob", AutoSize = true, MinimumSize = new Size(120, 32) };
        _editBtn = new Button { Text = "Edit", AutoSize = true, MinimumSize = new Size(120, 32) };
        _removeBtn = new Button { Text = "Remove", AutoSize = true, MinimumSize = new Size(120, 32) };
        _testBtn = new Button { Text = "Test Rules", AutoSize = true, MinimumSize = new Size(120, 32), Margin = new Padding(3, 12, 3, 3) };

        btnPanel.Controls.AddRange(new Control[] { _addPrefixBtn, _addGlobBtn, _editBtn, _removeBtn, _testBtn });
        layout.Controls.Add(btnPanel, 1, 1);

        // Row 2: Checkboxes
        var checkPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        _autoStartCheckBox = new CheckBox { Text = "Start with Windows", AutoSize = true, Checked = _config.AutoStart };
        _scanOnStartupCheckBox = new CheckBox { Text = "Scan existing files on startup", AutoSize = true, Checked = _config.ScanOnStartup };
        checkPanel.Controls.AddRange(new Control[] { _autoStartCheckBox, _scanOnStartupCheckBox });
        layout.Controls.Add(checkPanel, 0, 2);
        layout.SetColumnSpan(checkPanel, 2);

        // Row 3: Status label
        _statusLabel = new Label
        {
            Text = "",
            AutoSize = true,
            ForeColor = Color.DarkGreen,
            Padding = new Padding(0, 4, 0, 4)
        };
        layout.Controls.Add(_statusLabel, 0, 3);

        // Row 4: Save button
        _saveBtn = new Button
        {
            Text = "Save",
            AutoSize = true,
            MinimumSize = new Size(120, 36),
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
            Anchor = AnchorStyles.Right
        };
        layout.Controls.Add(_saveBtn, 1, 3);

        Controls.Add(layout);

        // Set form size after layout is built
        ClientSize = new Size(580, 400);

        // Wire events
        _addPrefixBtn.Click += (_, _) => AddRule(RuleType.PathPrefix);
        _addGlobBtn.Click += (_, _) => AddRule(RuleType.GlobPattern);
        _editBtn.Click += (_, _) => EditRule();
        _removeBtn.Click += (_, _) => RemoveRule();
        _testBtn.Click += (_, _) => TestRules();
        _saveBtn.Click += (_, _) => SaveConfig();

        RefreshRulesList();
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
        try
        {
            var engine = new ExclusionEngine();
            engine.UpdateRules(_config.Rules);

            string recentPath = RecentFileWatcher.RecentFolderPath;
            int matchCount = 0;
            int total = 0;

            foreach (var lnkFile in Directory.GetFiles(recentPath, "*.lnk"))
            {
                total++;
                try
                {
                    string? target = ShortcutResolver.ResolveTarget(lnkFile);
                    if (target != null && engine.IsExcluded(target))
                        matchCount++;
                }
                catch
                {
                    // Skip individual files that can't be resolved
                }
            }

            _statusLabel.Text = $"Test: {matchCount} of {total} recent files would be removed.";
            _statusLabel.ForeColor = matchCount > 0 ? Color.DarkOrange : Color.DarkGreen;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Test error: {ex.Message}";
            _statusLabel.ForeColor = Color.DarkRed;
        }
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
            AutoScaleMode = AutoScaleMode.Dpi,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(500, 120)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 2,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label { Text = prompt, AutoSize = true, Padding = new Padding(0, 0, 0, 6) };
        layout.Controls.Add(label, 0, 0);
        layout.SetColumnSpan(label, 2);

        var textBox = new TextBox { Text = defaultValue, Dock = DockStyle.Fill };
        layout.Controls.Add(textBox, 0, 1);
        layout.SetColumnSpan(textBox, 2);

        var btnPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
            Padding = new Padding(0, 8, 0, 0)
        };

        var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(80, 30) };
        var okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(80, 30) };
        btnPanel.Controls.AddRange(new Control[] { cancelBtn, okBtn });

        layout.Controls.Add(btnPanel, 0, 2);
        layout.SetColumnSpan(btnPanel, 2);

        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;
        form.Controls.Add(layout);

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
