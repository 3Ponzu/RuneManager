namespace RuneManager.Ui;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(14, 18, 24);
    public static readonly Color Panel = Color.FromArgb(22, 28, 36);
    public static readonly Color PanelAlt = Color.FromArgb(28, 36, 46);
    public static readonly Color Gold = Color.FromArgb(201, 170, 113);
    public static readonly Color GoldDim = Color.FromArgb(120, 98, 58);
    public static readonly Color Text = Color.FromArgb(230, 226, 214);
    public static readonly Color Muted = Color.FromArgb(150, 156, 164);
    public static readonly Color Line = Color.FromArgb(48, 58, 70);
    public static readonly Color Danger = Color.FromArgb(196, 92, 80);

    public static Button DarkButton(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = PanelAlt,
            ForeColor = Text,
            Height = 32
        };
}
