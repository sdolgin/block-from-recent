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

        SuspendLayout();

        Text = "Block From Recent — Settings";
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Shield;
        ClientSize = new Size(600, 440);

        int margin = 14;
        int btnWidth = 130;
        int btnHeight = 34;
        int btnLeft = 600 - margin - btnWidth;
        int listRight = btnLeft - 10;

        // Rules label
        var rulesLabel = new Label
        {
            Text = "Exclusion Rules:",
            Location = new Point(margin, margin),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold)
        };

        // Rules ListBox
        _rulesListBox = new ListBox
        {
            Location = new Point(margin, margin + 28),
            Size = new Size(listRight - margin, 230),
            Font = new Font("Consolas", 9.5f),
            IntegralHeight = false
        };

        // Side buttons
        int btnTop = margin + 28;
        _addPrefixBtn = MakeButton("+ Path Prefix", btnLeft, btnTop, btnWidth, btnHeight);
        _addGlobBtn = MakeButton("+ Glob Pattern", btnLeft, btnTop + 40, btnWidth, btnHeight);
        _editBtn = MakeButton("Edit", btnLeft, btnTop + 90, btnWidth, btnHeight);
        _removeBtn = MakeButton("Remove", btnLeft, btnTop + 130, btnWidth, btnHeight);
        _testBtn = MakeButton("Test Rules", btnLeft, btnTop + 190, btnWidth, btnHeight);

        // Checkboxes
        int checkTop = margin + 28 + 240;
        _autoStartCheckBox = new CheckBox
        {
            Text = "Start with Windows",
            Location = new Point(margin, checkTop),
            AutoSize = true,
            Checked = _config.AutoStart
        };

        _scanOnStartupCheckBox = new CheckBox
        {
            Text = "Scan existing files on startup",
            Location = new Point(margin, checkTop + 28),
            AutoSize = true,
            Checked = _config.ScanOnStartup
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "",
            Location = new Point(margin, checkTop + 64),
            Size = new Size(listRight - margin, 24),
            ForeColor = Color.DarkGreen
        };

        // Save button
        _saveBtn = new Button
        {
            Text = "Save",
            Location = new Point(btnLeft, checkTop + 56),
            Size = new Size(btnWidth, 40),
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold)
        };

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

        ResumeLayout(true);
        RefreshRulesList();
    }

    private static Button MakeButton(string text, int x, int y, int w, int h)
    {
        return new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h) };
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
        SetStatus("Rule added (not yet saved).", Color.DarkOrange);
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
        SetStatus("Rule updated (not yet saved).", Color.DarkOrange);
    }

    private void RemoveRule()
    {
        if (_rulesListBox.SelectedIndex < 0)
            return;

        _config.Rules.RemoveAt(_rulesListBox.SelectedIndex);
        RefreshRulesList();
        SetStatus("Rule removed (not yet saved).", Color.DarkOrange);
    }

    private void TestRules()
    {
        try
        {
            Log.Info("TestRules started");
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
                    {
                        matchCount++;
                        Log.Info($"  Match: {Path.GetFileName(lnkFile)} -> {target}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"  Skip {Path.GetFileName(lnkFile)}: {ex.Message}");
                }
            }

            Log.Info($"TestRules complete: {matchCount}/{total} matched");
            SetStatus($"Test: {matchCount} of {total} recent files would be removed.",
                matchCount > 0 ? Color.DarkOrange : Color.DarkGreen);
        }
        catch (Exception ex)
        {
            Log.Error("TestRules failed", ex);
            SetStatus($"Test error: {ex.Message}", Color.DarkRed);
        }
    }

    private void SaveConfig()
    {
        _config.AutoStart = _autoStartCheckBox.Checked;
        _config.ScanOnStartup = _scanOnStartupCheckBox.Checked;

        ConfigSaved?.Invoke(_config);
        SetStatus("Settings saved.", Color.DarkGreen);
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }

    private static string? PromptInput(string title, string prompt, string defaultValue = "")
    {
        using var form = new Form
        {
            Text = title,
            AutoScaleDimensions = new SizeF(96F, 96F),
            AutoScaleMode = AutoScaleMode.Dpi,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(520, 140)
        };

        int m = 14;
        var label = new Label { Text = prompt, Location = new Point(m, m), AutoSize = true };
        var textBox = new TextBox
        {
            Text = defaultValue,
            Location = new Point(m, m + 30),
            Size = new Size(520 - m * 2, 26)
        };

        var okBtn = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(90, 34),
            Location = new Point(520 - m - 90 - 100, m + 70)
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(90, 34),
            Location = new Point(520 - m - 90, m + 70)
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
