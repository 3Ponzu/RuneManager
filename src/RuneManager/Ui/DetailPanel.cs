using RuneManager.Models;

namespace RuneManager.Ui;

internal sealed class DetailPanel : Panel
{
    private readonly PictureBox _icon = new()
    {
        Size = new Size(48, 48),
        SizeMode = PictureBoxSizeMode.Zoom,
        Location = new Point(12, 12)
    };

    private readonly Label _title = new()
    {
        AutoSize = false,
        Location = new Point(70, 8),
        Height = 22,
        ForeColor = Theme.Gold,
        Font = new Font("Yu Gothic UI", 11f, FontStyle.Bold)
    };

    private readonly Label _body = new()
    {
        AutoSize = false,
        Location = new Point(70, 30),
        ForeColor = Theme.Text
    };

    public DetailPanel()
    {
        BackColor = Theme.Panel;
        Height = 88;
        Controls.Add(_icon);
        Controls.Add(_title);
        Controls.Add(_body);
        SetHint("ルーンにマウスを乗せると説明が出ます。クリックで選択します。");
    }

    protected override void OnResize(EventArgs e)
    {
        _title.Width = Math.Max(80, Width - 86);
        _body.Size = new Size(Math.Max(80, Width - 86), Height - 40);
        base.OnResize(e);
    }

    public void ShowPerk(PerkInfo perk, Image? icon)
    {
        _icon.Image = icon;
        _title.Text = perk.Name;
        _body.Text = perk.Description.Length > 0 ? perk.Description : "このルーンの説明は取得できませんでした。";
    }

    public void ShowStyle(StyleInfo style, Image? icon)
    {
        _icon.Image = icon;
        _title.Text = style.Name;
        _body.Text = style.Description.Length > 0 ? style.Description : "系統を選ぶと、下にキーストーンとルーンが出ます。";
    }

    public void SetHint(string text)
    {
        _icon.Image = null;
        _title.Text = "ルーン情報";
        _body.Text = text;
    }
}
