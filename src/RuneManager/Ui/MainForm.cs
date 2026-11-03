using RuneManager.Lcu;
using RuneManager.Models;
using RuneManager.Runes;

namespace RuneManager.Ui;

public sealed class MainForm : Form
{
    private readonly LcuClient _lcu = new();
    private readonly IconCache _icons = new();
    private readonly PresetStore _store = new();
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly RunePickerPanel _picker;
    private readonly DetailPanel _detail = new() { Dock = DockStyle.Fill };
    private readonly TextBox _pageName = new();
    private readonly ListBox _presets = new();
    private readonly Label _status = new();
    private readonly Label _currentPage = new();
    private readonly Button _applyButton;
    private RuneCatalog? _catalog;
    private bool _busy;

    private LaneCategory? _selectedCategory = null;
    private readonly Dictionary<LaneCategory, Button> _laneButtons = new();

    public MainForm()
    {
        Text = "RuneManager";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 720);
        ClientSize = new Size(1180, 780);
        Font = new Font("Yu Gothic UI", 9f);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        DoubleBuffered = true;

        _picker = new RunePickerPanel(_lcu, _icons) { Dock = DockStyle.Fill };
        _picker.ItemHovered += async (_, button) => await ShowDetailAsync(button);

        _status.AutoSize = false;
        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.AutoEllipsis = true;
        _status.ForeColor = Theme.Muted;
        _currentPage.AutoSize = false;
        _currentPage.ForeColor = Theme.Text;
        _currentPage.Dock = DockStyle.Fill;
        _currentPage.TextAlign = ContentAlignment.MiddleLeft;
        _currentPage.AutoEllipsis = true;
        _pageName.Dock = DockStyle.Fill;
        _pageName.BackColor = Theme.PanelAlt;
        _pageName.ForeColor = Theme.Text;
        _pageName.BorderStyle = BorderStyle.FixedSingle;
        _presets.Dock = DockStyle.Fill;
        _presets.BackColor = Theme.PanelAlt;
        _presets.ForeColor = Theme.Text;
        _presets.BorderStyle = BorderStyle.FixedSingle;
        _presets.IntegralHeight = false;

        _applyButton = Theme.DarkButton("現在のページを上書き");
        _applyButton.Font = new Font("Yu Gothic UI", 10f, FontStyle.Bold);
        _applyButton.ForeColor = Theme.Gold;
        var refresh = Theme.DarkButton("再読込");
        var savePreset = Theme.DarkButton("プリセット保存");
        var loadPreset = Theme.DarkButton("プリセット読込");
        var renamePreset = Theme.DarkButton("名前変更");
        var deletePreset = Theme.DarkButton("削除");

        var fromClient = Theme.DarkButton("クライアント取込");
        fromClient.Font = new Font("Yu Gothic UI", 8.5f);
        fromClient.Padding = new Padding(0);

        refresh.Click += async (_, _) => await RefreshFromClientAsync();
        savePreset.Click += (_, _) => SavePreset();
        loadPreset.Click += (_, _) => LoadSelectedPreset();
        renamePreset.Click += (_, _) => RenameSelectedPreset();
        deletePreset.Click += (_, _) => DeleteSelectedPreset();
        fromClient.Click += async (_, _) => await ImportCurrentAsync();
        _applyButton.Click += async (_, _) => await ApplyAsync();
        _presets.DoubleClick += (_, _) => LoadSelectedPreset();

        var lanePanel = CreateLaneButtonPanel();

        var nameRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Height = 28
        };
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nameRow.Controls.Add(new Label
        {
            Text = "ページ名",
            ForeColor = Theme.Muted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        nameRow.Controls.Add(_pageName, 1, 0);

        // 操作ボタン領域（保存・読込・名前変更・削除・取込・上書き）
        var sideButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
        sideButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        sideButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        sideButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        sideButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        sideButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        sideButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        sideButtons.Controls.Add(savePreset, 0, 0);
        sideButtons.Controls.Add(loadPreset, 1, 0);
        sideButtons.Controls.Add(renamePreset, 0, 1);
        sideButtons.Controls.Add(deletePreset, 1, 1);
        sideButtons.Controls.Add(fromClient, 0, 2);
        sideButtons.SetColumnSpan(fromClient, 2);
        sideButtons.Controls.Add(_applyButton, 0, 3);
        sideButtons.SetColumnSpan(_applyButton, 2);

        var side = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(8, 8, 0, 8)
        };
        side.RowStyles.Clear();
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); // 0: タイトル "プリセット"
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 1: ロール切替ボタン
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 2: ページ名
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 3: リスト
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 130)); // 4: 操作ボタン

        side.Controls.Add(new Label { Text = "プリセット", ForeColor = Theme.Gold, Dock = DockStyle.Fill, Font = new Font("Yu Gothic UI", 10f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        side.Controls.Add(lanePanel, 0, 1);
        side.Controls.Add(nameRow, 0, 2);
        side.Controls.Add(_presets, 0, 3);
        side.Controls.Add(sideButtons, 0, 4);

        var headerHeight = Math.Max(44, Font.Height + 28);
        refresh.Margin = new Padding(0, 4, 8, 4);
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        top.Controls.Add(refresh, 0, 0);
        top.Controls.Add(_status, 1, 0);
        top.Controls.Add(_currentPage, 2, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(6)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, headerHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.Controls.Add(top, 0, 0);
        root.SetColumnSpan(top, 2);
        root.Controls.Add(side, 0, 1);
        root.Controls.Add(_picker, 1, 1);
        root.Controls.Add(_detail, 0, 2);
        root.SetColumnSpan(_detail, 2);
        Controls.Add(root);

        _pollTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        Shown += async (_, _) =>
        {
            ReloadPresetList();
            await PollAsync();
            _pollTimer.Start();
        };
        FormClosed += (_, _) =>
        {
            _pollTimer.Stop();
            _icons.Dispose();
            _lcu.Dispose();
        };
    }

    private Panel CreateLaneButtonPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Height = 30
        };

        var categories = new[]
        {
            (LaneCategory.Top, "TOP"),
            (LaneCategory.Jungle, "JG"),
            (LaneCategory.Mid, "MID"),
            (LaneCategory.ADC, "ADC"),
            (LaneCategory.Support, "SUP")
        };

        int x = 0;
        int btnWidth = 48;
        int btnHeight = 26;

        foreach (var (category, label) in categories)
        {
            var btn = new Button
            {
                Text = label,
                Size = new Size(btnWidth, btnHeight),
                Location = new Point(x, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 45, 60),
                ForeColor = Color.White,
                Font = new Font("Yu Gothic UI", 8.5f, FontStyle.Bold),
                Tag = category
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(70, 80, 100);
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += LaneButton_Click;

            _laneButtons[category] = btn;
            panel.Controls.Add(btn);

            x += btnWidth + 2;
        }

        return panel;
    }

    private void LaneButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is LaneCategory clickedCategory)
        {
            _selectedCategory = _selectedCategory == clickedCategory ? null : clickedCategory;
            UpdateLaneButtonVisuals();
            ReloadPresetList();
        }
    }

    private void UpdateLaneButtonVisuals()
    {
        foreach (var (cat, btn) in _laneButtons)
        {
            if (_selectedCategory == cat)
            {
                btn.ForeColor = Theme.Gold;
                btn.BackColor = Color.FromArgb(60, 70, 95);
                btn.FlatAppearance.BorderColor = Theme.Gold;
            }
            else
            {
                btn.ForeColor = Color.White;
                btn.BackColor = Color.FromArgb(40, 45, 60);
                btn.FlatAppearance.BorderColor = Color.FromArgb(70, 80, 100);
            }
        }
    }

    private async Task ShowDetailAsync(RuneIconButton button)
    {
        if (_catalog is null) return;

        var icon = button.Icon ?? await _icons.GetAsync(_lcu, button.IconPath, 52);
        var style = _catalog.Style(button.ItemId);
        if (style is not null && !_catalog.Perks.ContainsKey(button.ItemId))
        {
            _detail.ShowStyle(style, icon);
            return;
        }

        _detail.ShowPerk(_catalog.Perk(button.ItemId), icon);
    }

    private async Task PollAsync()
    {
        if (_busy) return;

        var wasConnected = _lcu.IsConnected;
        if (!_lcu.TryConnect())
        {
            _catalog = null;
            _status.Text = "未接続: League クライアントを起動してください";
            _applyButton.Enabled = false;
            return;
        }

        _status.Text = $"接続中 {_lcu.BaseUrl}";
        _applyButton.Enabled = true;
        if (!wasConnected || _catalog is null)
        {
            await RefreshFromClientAsync();
        }
        else
        {
            await UpdateCurrentPageLabelAsync();
        }
    }

    private async Task RefreshFromClientAsync()
    {
        if (!_lcu.IsConnected && !_lcu.TryConnect()) return;

        _busy = true;
        try
        {
            var styles = await _lcu.GetStylesAsync() ?? [];
            var perks = await _lcu.GetPerksAsync() ?? [];
            _catalog = RuneCatalog.FromLcu(styles, perks);
            _picker.BindCatalog(_catalog);
            await ImportCurrentAsync(silent: true);
            await UpdateCurrentPageLabelAsync();
        }
        catch (Exception ex)
        {
            _status.Text = "接続中だがデータ取得に失敗: " + Short(ex);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task UpdateCurrentPageLabelAsync()
    {
        try
        {
            var page = await _lcu.GetCurrentPageAsync();
            if (page is null)
            {
                _currentPage.Text = "現在のページなし";
                return;
            }

            var editable = page.IsEditable ? "編集可" : "編集不可";
            _currentPage.Text = $"現在: {page.Name}  ({editable})";
        }
        catch
        {
            _currentPage.Text = "現在のページを取得できません";
        }
    }

    private async Task ImportCurrentAsync(bool silent = false)
    {
        try
        {
            var page = await _lcu.GetCurrentPageAsync();
            if (page is null)
            {
                if (!silent) MessageBox.Show(this, "現在のルーンページがありません。", Text);
                return;
            }

            _pageName.Text = page.Name;
            _picker.LoadPreset(page.ToPreset());
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show(this, Short(ex), Text);
        }
    }

    private async Task ApplyAsync()
    {
        if (!_lcu.IsConnected && !_lcu.TryConnect())
        {
            MessageBox.Show(this, "LCU に接続できません。", Text);
            return;
        }

        RunePreset preset;
        try
        {
            preset = ReadEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text);
            return;
        }

        _busy = true;
        _applyButton.Enabled = false;
        try
        {
            var current = await _lcu.GetCurrentPageAsync();
            var target = current is { IsEditable: true } ? current : null;
            if (target is null)
            {
                var pages = await _lcu.GetPagesAsync() ?? [];
                target = pages.FirstOrDefault(p => p.IsEditable);
            }

            if (target is null)
            {
                MessageBox.Show(this, "上書きできるカスタムページがありません。クライアントで空のページを1つ作ってください。", Text);
                return;
            }

            await _lcu.OverwritePageAsync(target.Id, preset);
            await UpdateCurrentPageLabelAsync();
            _status.Text = $"上書きしました: {preset.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, Short(ex), Text);
        }
        finally
        {
            _busy = false;
            _applyButton.Enabled = true;
        }
    }

    private void SavePreset()
    {
        try
        {
            var preset = ReadEditor();
            var targetCategory = _selectedCategory ?? LaneCategory.All;
            var list = _store.GetByCategory(targetCategory);
            var existing = list.FirstOrDefault(p => string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                preset.Id = existing.Id;
            }

            _store.Save(preset);
            ReloadPresetList();
            _presets.SelectedItem = preset.Name;
            MessageBox.Show(this, "プリセットを保存しました。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text);
        }
    }

    private void LoadSelectedPreset()
    {
        if (_presets.SelectedItem is not string name) return;

        var targetCategory = _selectedCategory ?? LaneCategory.All;
        var list = _store.GetByCategory(targetCategory);
        var preset = list.FirstOrDefault(p => p.Name == name);
        if (preset is not null)
        {
            _pageName.Text = preset.Name;
            _picker.LoadPreset(preset);
        }
    }

    private void RenameSelectedPreset()
    {
        if (_presets.SelectedItem is not string oldName)
        {
            MessageBox.Show(this, "名前を変更するプリセットを一覧から選択してください。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var newName = _pageName.Text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            MessageBox.Show(this, "新しいページ名を入力してください。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var targetCategory = _selectedCategory ?? LaneCategory.All;
        var list = _store.GetByCategory(targetCategory);
        var preset = list.FirstOrDefault(p => p.Name == oldName);
        if (preset is not null)
        {
            preset.Name = newName;
            _store.Save(preset);
            ReloadPresetList();
            _presets.SelectedItem = newName;
            MessageBox.Show(this, "プリセット名を変更しました。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void DeleteSelectedPreset()
    {
        if (_presets.SelectedItem is not string name) return;

        var targetCategory = _selectedCategory ?? LaneCategory.All;
        var list = _store.GetByCategory(targetCategory);
        var preset = list.FirstOrDefault(p => p.Name == name);
        if (preset is not null)
        {
            _store.Delete(preset.Id);
            ReloadPresetList();
        }
    }

    private void ReloadPresetList()
    {
        var selected = _presets.SelectedItem as string;
        _presets.BeginUpdate();
        _presets.Items.Clear();

        var targetCategory = _selectedCategory ?? LaneCategory.All;
        var list = _store.GetByCategory(targetCategory);

        foreach (var preset in list)
        {
            _presets.Items.Add(preset.Name);
        }

        _presets.EndUpdate();
        if (selected is not null && _presets.Items.Contains(selected))
        {
            _presets.SelectedItem = selected;
        }
    }

    private RunePreset ReadEditor()
    {
        var name = _pageName.Text.Trim();
        if (name.Length == 0)
        {
            throw new InvalidOperationException("ページ名を入力してください。");
        }

        var preset = _picker.ReadPreset(name);
        preset.Category = _selectedCategory ?? LaneCategory.All;
        return preset;
    }

    private static string Short(Exception ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Length > 600 ? message[..600] : message;
    }
}