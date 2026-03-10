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
    private readonly CheckBox _verboseLoggingCheckBox;
    private readonly Button _testBtn;
    private readonly Button _exportBtn;
    private readonly Button _importBtn;
    private readonly Button _saveBtn;
    private readonly Button _openLogBtn;
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
        ClientSize = new Size(600, 480);

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

        _verboseLoggingCheckBox = new CheckBox
        {
            Text = "Enable verbose logging",
            Location = new Point(margin, checkTop + 56),
            AutoSize = true,
            Checked = _config.VerboseLogging
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "",
            Location = new Point(margin, checkTop + 90),
            Size = new Size(listRight - margin, 24),
            ForeColor = Color.DarkGreen
        };

        // Open Log button
        _openLogBtn = new Button
        {
            Text = "Open Log",
            Location = new Point(btnLeft, checkTop + 14),
            Size = new Size(btnWidth, 34)
        };

        // Export Rules button
        _exportBtn = new Button
        {
            Text = "Export Rules",
            Location = new Point(btnLeft, checkTop + 54),
            Size = new Size(btnWidth, 34)
        };

        // Import Rules button
        _importBtn = new Button
        {
            Text = "Import Rules",
            Location = new Point(btnLeft, checkTop + 94),
            Size = new Size(btnWidth, 34)
        };

        // Save button
        _saveBtn = new Button
        {
            Text = "Save",
            Location = new Point(btnLeft, checkTop + 140),
            Size = new Size(btnWidth, 40),
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold)
        };

        Controls.AddRange(new Control[]
        {
            rulesLabel, _rulesListBox,
            _addPrefixBtn, _addGlobBtn, _editBtn, _removeBtn, _testBtn,
            _autoStartCheckBox, _scanOnStartupCheckBox, _verboseLoggingCheckBox,
            _statusLabel, _openLogBtn, _exportBtn, _importBtn, _saveBtn
        });

        // Wire events
        _addPrefixBtn.Click += (_, _) => AddRule(RuleType.PathPrefix);
        _addGlobBtn.Click += (_, _) => AddRule(RuleType.GlobPattern);
        _editBtn.Click += (_, _) => EditRule();
        _removeBtn.Click += (_, _) => RemoveRule();
        _testBtn.Click += (_, _) => TestRules();
        _openLogBtn.Click += (_, _) => OpenLogFile();
        _exportBtn.Click += (_, _) => ExportRules();
        _importBtn.Click += (_, _) => ImportRules();
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
            int lnkMatchCount = 0;
            int total = 0;

            foreach (var lnkFile in Directory.GetFiles(recentPath, "*.lnk"))
            {
                total++;
                try
                {
                    string? target = ShortcutResolver.ResolveTarget(lnkFile);
                    if (target != null && engine.IsExcluded(target))
                    {
                        lnkMatchCount++;
                        Log.Info($"  Match: {Path.GetFileName(lnkFile)} -> {target}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"  Skip {Path.GetFileName(lnkFile)}: {ex.Message}");
                }
            }

            // Also count jump list matches
            int jumpListCount = JumpListCleaner.CountMatches(engine);

            Log.Info($"TestRules complete: {lnkMatchCount}/{total} .lnk matched, {jumpListCount} jump list entries matched");
            int totalMatches = lnkMatchCount + jumpListCount;
            string msg = $"Test: {lnkMatchCount} of {total} .lnk files";
            if (jumpListCount > 0)
                msg += $" + {jumpListCount} jump list entries";
            msg += " would be removed.";

            SetStatus(msg, totalMatches > 0 ? Color.DarkOrange : Color.DarkGreen);
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
        _config.VerboseLogging = _verboseLoggingCheckBox.Checked;

        ConfigSaved?.Invoke(_config);
        SetStatus("Settings saved.", Color.DarkGreen);
    }

    private void ExportRules()
    {
        if (_config.Rules.Count == 0)
        {
            SetStatus("No rules to export.", Color.DarkRed);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export Exclusion Rules",
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = "json",
            FileName = "exclusion-rules.json"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            ConfigManager.ExportRules(_config.Rules, dialog.FileName);
            SetStatus($"Exported {_config.Rules.Count} rule(s).", Color.DarkGreen);
            Log.Info($"Exported {_config.Rules.Count} rule(s) to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to export rules", ex);
            SetStatus($"Export failed: {ex.Message}", Color.DarkRed);
        }
    }

    private void ImportRules()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import Exclusion Rules",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        RulesExport import;
        try
        {
            import = ConfigManager.ImportRules(dialog.FileName);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to import rules from {dialog.FileName}: {ex.Message}");
            MessageBox.Show(
                $"The selected file is not a valid rules file.\n\n{ex.Message}",
                "Import Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (import.Rules.Count == 0)
        {
            MessageBox.Show(
                "The selected file contains no rules.",
                "Import Rules",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"The file contains {import.Rules.Count} rule(s).\n\n" +
            "Yes = Replace all existing rules\n" +
            "No = Merge with existing rules (skip duplicates)",
            "Import Rules",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (result == DialogResult.Cancel)
            return;

        if (result == DialogResult.Yes)
        {
            _config.Rules.Clear();
            _config.Rules.AddRange(import.Rules);
            RefreshRulesList();
            SetStatus($"Replaced with {import.Rules.Count} imported rule(s) (not yet saved).", Color.DarkOrange);
            Log.Info($"Imported {import.Rules.Count} rule(s) (replace mode) from {dialog.FileName}");
        }
        else
        {
            int added = 0;
            foreach (var rule in import.Rules)
            {
                bool isDuplicate = _config.Rules.Any(existing =>
                    string.Equals(existing.Pattern, rule.Pattern, StringComparison.OrdinalIgnoreCase)
                    && existing.Type == rule.Type);

                if (!isDuplicate)
                {
                    _config.Rules.Add(rule);
                    added++;
                }
            }

            RefreshRulesList();
            int skipped = import.Rules.Count - added;
            SetStatus($"Merged: {added} added, {skipped} duplicate(s) skipped (not yet saved).", Color.DarkOrange);
            Log.Info($"Imported {added} rule(s), skipped {skipped} duplicate(s) (merge mode) from {dialog.FileName}");
        }
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
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
            VerboseLogging = source.VerboseLogging,
            Rules = source.Rules.Select(r => new ExclusionRule
            {
                Pattern = r.Pattern,
                Type = r.Type
            }).ToList()
        };
    }
}
