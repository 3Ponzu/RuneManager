using System.ComponentModel;
using RuneManager.Models;

namespace RuneManager.Ui;

internal sealed class RuneIconButton : Control
{
    private bool _selected;
    private bool _dimmed;
    private bool _hover;
    private Image? _icon;

    public RuneIconButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ItemId { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ItemName { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Description { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string IconPath { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCaption { get; set; } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Dimmed
    {
        get => _dimmed;
        set
        {
            _dimmed = value;
            Invalidate();
        }
    }

    public event EventHandler<RuneIconButton>? Hovered;

    public void Bind(PerkInfo perk)
    {
        ItemId = perk.Id;
        ItemName = perk.Name;
        Description = perk.Description;
        IconPath = perk.IconPath;
        Invalidate();
    }

    public void BindStyle(StyleInfo style)
    {
        ItemId = style.Id;
        ItemName = style.Name;
        Description = style.Description;
        IconPath = style.IconPath;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        Hovered?.Invoke(this, this);
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var caption = ShowCaption ? 16 : 0;
        var box = Math.Min(Width - 4, Height - caption - 4);
        var x = (Width - box) / 2;
        var y = 2;
        var rect = new Rectangle(x, y, box, box);

        using (var fill = new SolidBrush(_selected ? Color.FromArgb(50, 42, 28) : Theme.PanelAlt))
        {
            g.FillEllipse(fill, rect);
        }

        if (_icon is not null)
        {
            var pad = 4;
            g.DrawImage(_icon, rect.X + pad, rect.Y + pad, rect.Width - pad * 2, rect.Height - pad * 2);
        }
        else if (ItemName.Length > 0)
        {
            TextRenderer.DrawText(
                g,
                ItemName[..1],
                Font,
                rect,
                Theme.Gold,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        if (_dimmed && !_selected)
        {
            using var overlay = new SolidBrush(Color.FromArgb(140, 14, 18, 24));
            g.FillEllipse(overlay, rect);
        }

        var border = _selected ? Theme.Gold : _hover ? Theme.GoldDim : Theme.Line;
        using var pen = new Pen(border, _selected ? 2.2f : 1f);
        g.DrawEllipse(pen, rect);

        if (ShowCaption && ItemName.Length > 0)
        {
            var textRect = new Rectangle(0, rect.Bottom, Width, caption);
            TextRenderer.DrawText(
                g,
                ItemName,
                Font,
                textRect,
                _selected ? Theme.Gold : Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }
}
