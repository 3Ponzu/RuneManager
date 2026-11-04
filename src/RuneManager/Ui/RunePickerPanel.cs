using RuneManager.Lcu;
using RuneManager.Models;
using RuneManager.Runes;

namespace RuneManager.Ui;

internal sealed class RunePickerPanel : Panel
{
    private readonly LcuClient _lcu;
    private readonly IconCache _icons;
    private RuneCatalog? _catalog;

    private readonly TableLayoutPanel _mainStylesLayout;
    private readonly TableLayoutPanel _subStylesLayout;

    private readonly FlowLayoutPanel _keystoneFlow;
    private readonly FlowLayoutPanel _primarySlot1Flow;
    private readonly FlowLayoutPanel _primarySlot2Flow;
    private readonly FlowLayoutPanel _primarySlot3Flow;

    private readonly FlowLayoutPanel _subSlot1Flow;
    private readonly FlowLayoutPanel _subSlot2Flow;
    private readonly FlowLayoutPanel _subSlot3Flow;

    private readonly FlowLayoutPanel _shardSlot1Flow;
    private readonly FlowLayoutPanel _shardSlot2Flow;
    private readonly FlowLayoutPanel _shardSlot3Flow;

    private int? _selectedPrimaryStyleId;
    private int? _selectedSubStyleId;
    private int? _selectedKeystoneId;
    private int? _selectedPrimaryPerk1;
    private int? _selectedPrimaryPerk2;
    private int? _selectedPrimaryPerk3;

    private readonly List<int> _selectedSubPerks = new();
    private readonly int?[] _selectedShardPerks = new int?[3];

    public event EventHandler<RuneIconButton>? ItemHovered;

    public RunePickerPanel(LcuClient lcu, IconCache icons)
    {
        _lcu = lcu;
        _icons = icons;

        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(4)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // --- メイン領域 ---
        var primaryGroup = CreateGroup("メイン");
        var primaryTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 2
        };
        primaryTable.ColumnStyles.Clear();
        primaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        primaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        primaryTable.RowStyles.Clear();
        primaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        primaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        primaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        primaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        primaryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

        _mainStylesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 5,
            Margin = Padding.Empty
        };
        for (int i = 0; i < 5; i++)
        {
            _mainStylesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        }

        _keystoneFlow = CreateFlow();
        _primarySlot1Flow = CreateFlow();
        _primarySlot2Flow = CreateFlow();
        _primarySlot3Flow = CreateFlow();

        primaryTable.Controls.Add(_mainStylesLayout, 0, 0);
        primaryTable.SetColumnSpan(_mainStylesLayout, 2);

        AddRow(primaryTable, 1, "キーストーン", _keystoneFlow);
        AddRow(primaryTable, 2, "スロット 1", _primarySlot1Flow);
        AddRow(primaryTable, 3, "スロット 2", _primarySlot2Flow);
        AddRow(primaryTable, 4, "スロット 3", _primarySlot3Flow);

        primaryGroup.Controls.Add(primaryTable);

        // --- 右側（サブ ＋ ステータス） ---
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130f));

        // サブ領域
        var subGroup = CreateGroup("サブ (2つ)");
        var subTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 2
        };
        subTable.ColumnStyles.Clear();
        subTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        subTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        subTable.RowStyles.Clear();
        subTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        subTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        subTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
        subTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

        _subStylesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 4,
            Margin = Padding.Empty
        };
        for (int i = 0; i < 4; i++)
        {
            _subStylesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        _subSlot1Flow = CreateFlow();
        _subSlot2Flow = CreateFlow();
        _subSlot3Flow = CreateFlow();

        subTable.Controls.Add(_subStylesLayout, 0, 0);
        subTable.SetColumnSpan(_subStylesLayout, 2);

        AddRow(subTable, 1, "サブ 1", _subSlot1Flow);
        AddRow(subTable, 2, "サブ 2", _subSlot2Flow);
        AddRow(subTable, 3, "サブ 3", _subSlot3Flow);

        subGroup.Controls.Add(subTable);

        // ステータス（シャード）領域
        var shardGroup = CreateGroup("ステータス");
        var shardTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 2
        };
        shardTable.ColumnStyles.Clear();
        shardTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        shardTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        shardTable.RowStyles.Clear();
        shardTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        shardTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
        shardTable.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));

        _shardSlot1Flow = CreateFlow();
        _shardSlot2Flow = CreateFlow();
        _shardSlot3Flow = CreateFlow();

        AddRow(shardTable, 0, "シャード 1", _shardSlot1Flow);
        AddRow(shardTable, 1, "シャード 2", _shardSlot2Flow);
        AddRow(shardTable, 2, "シャード 3", _shardSlot3Flow);

        shardGroup.Controls.Add(shardTable);

        rightLayout.Controls.Add(subGroup, 0, 0);
        rightLayout.Controls.Add(shardGroup, 0, 1);

        mainLayout.Controls.Add(primaryGroup, 0, 0);
        mainLayout.Controls.Add(rightLayout, 1, 0);

        Controls.Add(mainLayout);
    }

    public void BindCatalog(RuneCatalog catalog)
    {
        _catalog = catalog;
        BuildPrimaryStyles();
        BuildShardSlots();
    }

    public void LoadPreset(RunePreset preset)
    {
        if (_catalog is null) return;

        _selectedPrimaryStyleId = preset.PrimaryStyleId;
        _selectedSubStyleId = preset.SubStyleId;

        var remainingPerks = new List<int>(preset.SelectedPerkIds ?? Array.Empty<int>());

        var primaryStyle = _catalog.Style(preset.PrimaryStyleId);
        if (primaryStyle is not null)
        {
            _selectedKeystoneId = ExtractSelectedFromSlot(primaryStyle.Slots.ElementAtOrDefault(0), remainingPerks);
            _selectedPrimaryPerk1 = ExtractSelectedFromSlot(primaryStyle.Slots.ElementAtOrDefault(1), remainingPerks);
            _selectedPrimaryPerk2 = ExtractSelectedFromSlot(primaryStyle.Slots.ElementAtOrDefault(2), remainingPerks);
            _selectedPrimaryPerk3 = ExtractSelectedFromSlot(primaryStyle.Slots.ElementAtOrDefault(3), remainingPerks);
        }

        var subStyle = _catalog.Style(preset.SubStyleId);
        _selectedSubPerks.Clear();
        if (subStyle is not null)
        {
            for (int i = 1; i < subStyle.Slots.Count; i++)
            {
                var perkId = ExtractSelectedFromSlot(subStyle.Slots[i], remainingPerks);
                if (perkId.HasValue) _selectedSubPerks.Add(perkId.Value);
            }
        }

        Array.Clear(_selectedShardPerks, 0, _selectedShardPerks.Length);
        if (_catalog.ShardSlots.Count >= 3)
        {
            for (int r = 0; r < 3; r++)
            {
                int foundIdx = -1;
                for (int i = 0; i < remainingPerks.Count; i++)
                {
                    int pid = remainingPerks[i];
                    if (_catalog.ShardSlots[r].Contains(pid))
                    {
                        _selectedShardPerks[r] = pid;
                        foundIdx = i;
                        break;
                    }
                }
                if (foundIdx >= 0)
                {
                    remainingPerks.RemoveAt(foundIdx);
                }
            }
        }

        RebuildAll();
    }

    public RunePreset ReadPreset(string name)
    {
        if (!_selectedPrimaryStyleId.HasValue || !_selectedSubStyleId.HasValue)
        {
            throw new InvalidOperationException("メインパスとサブパスを選択してください。");
        }

        if (!_selectedKeystoneId.HasValue || !_selectedPrimaryPerk1.HasValue ||
            !_selectedPrimaryPerk2.HasValue || !_selectedPrimaryPerk3.HasValue)
        {
            throw new InvalidOperationException("メインルーンをすべて選択してください。");
        }

        if (_selectedSubPerks.Count != 2)
        {
            throw new InvalidOperationException("サブルーンを正確に2つ選択してください。");
        }

        var selectedPerks = new List<int>
        {
            _selectedKeystoneId.Value,
            _selectedPrimaryPerk1.Value,
            _selectedPrimaryPerk2.Value,
            _selectedPrimaryPerk3.Value
        };
        selectedPerks.AddRange(_selectedSubPerks);

        for (int r = 0; r < 3; r++)
        {
            if (_selectedShardPerks[r].HasValue)
            {
                selectedPerks.Add(_selectedShardPerks[r]!.Value);
            }
        }

        return new RunePreset
        {
            Name = name,
            PrimaryStyleId = _selectedPrimaryStyleId.Value,
            SubStyleId = _selectedSubStyleId.Value,
            SelectedPerkIds = selectedPerks.ToArray()
        };
    }

    private static GroupBox CreateGroup(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Gold,
            BackColor = Theme.Panel,
            Font = new Font("Yu Gothic UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(4)
        };
    }

    private static FlowLayoutPanel CreateFlow()
    {
        return new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            WrapContents = false,
            AutoScroll = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
    }

    private static void AddRow(TableLayoutPanel table, int row, string labelText, Control control)
    {
        table.Controls.Add(new Label
        {
            Text = labelText,
            ForeColor = Theme.Muted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Yu Gothic UI", 8.5f),
            Margin = Padding.Empty
        }, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static int? ExtractSelectedFromSlot(int[]? perkIds, List<int> remainingPerks)
    {
        if (perkIds is null) return null;
        for (int i = 0; i < remainingPerks.Count; i++)
        {
            int pid = remainingPerks[i];
            if (perkIds.Contains(pid))
            {
                remainingPerks.RemoveAt(i);
                return pid;
            }
        }
        return null;
    }

    private void RebuildAll()
    {
        BuildPrimaryStyles();
        BuildPrimarySlots();
        BuildSubStyles();
        BuildSubSlots();
        BuildShardSlots();
    }

    private void BuildPrimaryStyles()
    {
        _mainStylesLayout.Controls.Clear();
        if (_catalog is null) return;

        foreach (var style in _catalog.Styles)
        {
            var btn = CreateStyleButton(style, style.Id == _selectedPrimaryStyleId, 44, showCaption: true);
            btn.Click += (_, _) =>
            {
                if (_selectedPrimaryStyleId == style.Id) return;
                _selectedPrimaryStyleId = style.Id;
                if (_selectedSubStyleId == style.Id) _selectedSubStyleId = null;

                _selectedKeystoneId = null;
                _selectedPrimaryPerk1 = null;
                _selectedPrimaryPerk2 = null;
                _selectedPrimaryPerk3 = null;
                _selectedSubPerks.Clear();

                RebuildAll();
            };
            _mainStylesLayout.Controls.Add(btn);
        }
    }

    private void BuildSubStyles()
    {
        _subStylesLayout.Controls.Clear();
        if (_catalog is null || !_selectedPrimaryStyleId.HasValue) return;

        var subStyles = _catalog.SubStylesFor(_selectedPrimaryStyleId.Value);
        foreach (var style in subStyles)
        {
            var btn = CreateStyleButton(style, style.Id == _selectedSubStyleId, 44, showCaption: true);
            btn.Click += (_, _) =>
            {
                if (_selectedSubStyleId == style.Id) return;
                _selectedSubStyleId = style.Id;
                _selectedSubPerks.Clear();

                BuildSubStyles();
                BuildSubSlots();
            };
            _subStylesLayout.Controls.Add(btn);
        }
    }

    private void BuildPrimarySlots()
    {
        _keystoneFlow.Controls.Clear();
        _primarySlot1Flow.Controls.Clear();
        _primarySlot2Flow.Controls.Clear();
        _primarySlot3Flow.Controls.Clear();

        if (_catalog is null || !_selectedPrimaryStyleId.HasValue) return;
        var style = _catalog.Style(_selectedPrimaryStyleId.Value);
        if (style is null || style.Slots.Count < 4) return;

        PopulateSlot(_keystoneFlow, style.Slots[0], _selectedKeystoneId, id => { _selectedKeystoneId = id; BuildPrimarySlots(); }, 50, showCaption: true);
        PopulateSlot(_primarySlot1Flow, style.Slots[1], _selectedPrimaryPerk1, id => { _selectedPrimaryPerk1 = id; BuildPrimarySlots(); }, 38, showCaption: true);
        PopulateSlot(_primarySlot2Flow, style.Slots[2], _selectedPrimaryPerk2, id => { _selectedPrimaryPerk2 = id; BuildPrimarySlots(); }, 38, showCaption: true);
        PopulateSlot(_primarySlot3Flow, style.Slots[3], _selectedPrimaryPerk3, id => { _selectedPrimaryPerk3 = id; BuildPrimarySlots(); }, 38, showCaption: true);
    }

    private void BuildSubSlots()
    {
        _subSlot1Flow.Controls.Clear();
        _subSlot2Flow.Controls.Clear();
        _subSlot3Flow.Controls.Clear();

        if (_catalog is null || !_selectedSubStyleId.HasValue) return;
        var style = _catalog.Style(_selectedSubStyleId.Value);
        if (style is null || style.Slots.Count < 4) return;

        PopulateSubSlot(_subSlot1Flow, style.Slots[1]);
        PopulateSubSlot(_subSlot2Flow, style.Slots[2]);
        PopulateSubSlot(_subSlot3Flow, style.Slots[3]);
    }

    private void BuildShardSlots()
    {
        _shardSlot1Flow.Controls.Clear();
        _shardSlot2Flow.Controls.Clear();
        _shardSlot3Flow.Controls.Clear();

        if (_catalog is null || _catalog.ShardSlots.Count < 3) return;

        PopulateShardSlot(_shardSlot1Flow, _catalog.ShardSlots[0], 0);
        PopulateShardSlot(_shardSlot2Flow, _catalog.ShardSlots[1], 1);
        PopulateShardSlot(_shardSlot3Flow, _catalog.ShardSlots[2], 2);
    }

    private void PopulateSubSlot(FlowLayoutPanel panel, int[] perkIds)
    {
        foreach (var perkId in perkIds)
        {
            var perk = _catalog!.Perk(perkId);
            var isSelected = _selectedSubPerks.Contains(perkId);
            var btn = CreatePerkButton(perk, isSelected, 38, showCaption: true);
            btn.Click += (_, _) =>
            {
                if (isSelected)
                {
                    _selectedSubPerks.Remove(perkId);
                }
                else
                {
                    // 同じ段に既に選択されているルーンがあれば解除
                    _selectedSubPerks.RemoveAll(id => perkIds.Contains(id));

                    if (_selectedSubPerks.Count >= 2)
                    {
                        _selectedSubPerks.RemoveAt(0);
                    }
                    _selectedSubPerks.Add(perkId);
                }
                BuildSubSlots();
            };
            panel.Controls.Add(btn);
        }
    }

    private void PopulateSlot(FlowLayoutPanel panel, int[] perkIds, int? selectedId, Action<int> onSelect, int size, bool showCaption = true)
    {
        foreach (var perkId in perkIds)
        {
            var perk = _catalog!.Perk(perkId);
            var btn = CreatePerkButton(perk, perkId == selectedId, size, showCaption: showCaption);
            btn.Click += (_, _) => onSelect(perkId);
            panel.Controls.Add(btn);
        }
    }

    private void PopulateShardSlot(FlowLayoutPanel panel, int[] perkIds, int rowIndex)
    {
        foreach (var perkId in perkIds)
        {
            var perk = _catalog!.Perk(perkId);
            var isSelected = _selectedShardPerks[rowIndex] == perkId;
            var btn = CreatePerkButton(perk, isSelected, 30, showCaption: false);
            btn.Click += (_, _) =>
            {
                _selectedShardPerks[rowIndex] = perkId;
                BuildShardSlots();
            };
            panel.Controls.Add(btn);
        }
    }

    private RuneIconButton CreateStyleButton(StyleInfo style, bool selected, int size, bool showCaption = true)
    {
        var btn = new RuneIconButton
        {
            Size = new Size(size + 24, size + (showCaption ? 18 : 0)),
            Margin = new Padding(3, 2, 3, 2),
            Selected = selected,
            ShowCaption = showCaption
        };
        btn.BindStyle(style);
        btn.Hovered += (_, _) => ItemHovered?.Invoke(this, btn);
        _ = LoadIconAsync(btn);
        return btn;
    }

    private RuneIconButton CreatePerkButton(PerkInfo perk, bool selected, int size, bool showCaption = true)
    {
        var btn = new RuneIconButton
        {
            Size = new Size(size + 38, size + (showCaption ? 18 : 0)),
            Margin = new Padding(3, 2, 3, 2),
            Selected = selected,
            ShowCaption = showCaption
        };
        btn.Bind(perk);
        btn.Hovered += (_, _) => ItemHovered?.Invoke(this, btn);
        _ = LoadIconAsync(btn);
        return btn;
    }

    private async Task LoadIconAsync(RuneIconButton btn)
    {
        if (!string.IsNullOrEmpty(btn.IconPath))
        {
            try
            {
                btn.Icon = await _icons.GetAsync(_lcu, btn.IconPath, btn.Width);
            }
            catch
            {
            }
        }
    }
}