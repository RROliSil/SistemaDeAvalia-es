using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

/// <summary>
/// Barra de título própria responsiva ao tema (Claro / Noturno) para garantir
/// aparência consistente no Windows 10 IoT, Windows 10 e Windows 11.
/// </summary>
public sealed class ThemedTitleBar : Panel
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    private readonly Form _owner;
    private readonly Label _lblTitle;
    private readonly Button _btnClose;
    private readonly Button? _btnMaximize;
    private readonly Button? _btnMinimize;
    private ThemePalette _theme;

    public ThemedTitleBar(Form owner, ThemePalette initialTheme, bool showMaximize = false, bool showMinimize = false)
    {
        _owner = owner;
        _theme = initialTheme;

        Dock = DockStyle.Top;
        Height = 34;

        var picIcon = new PictureBox
        {
            Location = new Point(10, 8),
            Size = new Size(18, 18),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        Icon? appIcon = AppConfigAndStorage.LoadEmbeddedIcon();
        if (appIcon != null)
        {
            picIcon.Image = appIcon.ToBitmap();
        }
        Controls.Add(picIcon);

        _lblTitle = new Label
        {
            Text = owner.Text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Location = new Point(34, 7),
            Size = new Size(340, 20),
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        Controls.Add(_lblTitle);

        // Atualiza o texto automaticamente quando owner.Text mudar
        owner.TextChanged += (s, e) => _lblTitle.Text = owner.Text;

        int rightX = Width;

        _btnClose = CreateCaptionButton("✕");
        _btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnClose.Location = new Point(rightX - 42, 0);
        _btnClose.Click += (s, e) => _owner.Close();
        _btnClose.MouseEnter += (s, e) =>
        {
            _btnClose.BackColor = Color.FromArgb(232, 17, 35);
            _btnClose.ForeColor = Color.White;
        };
        _btnClose.MouseLeave += (s, e) =>
        {
            _btnClose.BackColor = _theme.HeaderBg;
            _btnClose.ForeColor = _theme.TextPrimary;
        };
        Controls.Add(_btnClose);
        rightX -= 42;

        if (showMaximize)
        {
            _btnMaximize = CreateCaptionButton("☐");
            _btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnMaximize.Location = new Point(rightX - 42, 0);
            _btnMaximize.Click += (s, e) => ToggleMaximize();
            AttachStandardHover(_btnMaximize);
            Controls.Add(_btnMaximize);
            rightX -= 42;
        }

        if (showMinimize)
        {
            _btnMinimize = CreateCaptionButton("—");
            _btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnMinimize.Location = new Point(rightX - 42, 0);
            _btnMinimize.Click += (s, e) => _owner.WindowState = FormWindowState.Minimized;
            AttachStandardHover(_btnMinimize);
            Controls.Add(_btnMinimize);
            rightX -= 42;
        }

        SizeChanged += (s, e) =>
        {
            _lblTitle.Width = Math.Max(120, Width - (42 * (1 + (showMaximize ? 1 : 0) + (showMinimize ? 1 : 0))) - 44);
        };

        void StartDrag(object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.Clicks == 2 && showMaximize)
                {
                    ToggleMaximize();
                }
                else
                {
                    ReleaseCapture();
                    SendMessage(_owner.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                }
            }
        }

        MouseDown += StartDrag;
        _lblTitle.MouseDown += StartDrag;
        picIcon.MouseDown += StartDrag;

        ApplyTheme(initialTheme);
    }

    private void ToggleMaximize()
    {
        _owner.WindowState = _owner.WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;

        if (_btnMaximize != null)
        {
            _btnMaximize.Text = _owner.WindowState == FormWindowState.Maximized ? "❐" : "☐";
        }
    }

    private Button CreateCaptionButton(string symbol)
    {
        var btn = new Button
        {
            Text = symbol,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Size = new Size(42, 34),
            FlatStyle = FlatStyle.Flat,
            TabStop = false,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void AttachStandardHover(Button btn)
    {
        btn.MouseEnter += (s, e) => btn.BackColor = _theme.CardHoverBg;
        btn.MouseLeave += (s, e) => btn.BackColor = _theme.HeaderBg;
    }

    public void ApplyTheme(ThemePalette theme)
    {
        _theme = theme;
        BackColor = theme.HeaderBg;
        _lblTitle.ForeColor = theme.TextPrimary;

        _btnClose.BackColor = theme.HeaderBg;
        _btnClose.ForeColor = theme.TextPrimary;

        if (_btnMaximize != null)
        {
            _btnMaximize.BackColor = theme.HeaderBg;
            _btnMaximize.ForeColor = theme.TextPrimary;
        }

        if (_btnMinimize != null)
        {
            _btnMinimize.BackColor = theme.HeaderBg;
            _btnMinimize.ForeColor = theme.TextPrimary;
        }
    }
}
