using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

public sealed class ThemedMessageDialog : Form
{
    private ThemedMessageDialog(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        ThemePalette theme = AppConfigAndStorage.GetActiveTheme();

        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = theme.WindowBg;
        Width = 480;

        Icon? appIcon = AppConfigAndStorage.LoadEmbeddedIcon();
        if (appIcon != null)
            Icon = appIcon;

        var topBanner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 6
        };
        topBanner.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(
                topBanner.ClientRectangle,
                theme.BrandRed,
                theme.BrandYellow,
                LinearGradientMode.Horizontal);
            var blend = new ColorBlend
            {
                Colors = new[] { theme.BrandRed, theme.BrandOrange, theme.BrandYellow },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            brush.InterpolationColors = blend;
            e.Graphics.FillRectangle(brush, topBanner.ClientRectangle);
        };
        Controls.Add(topBanner);

        string badgeSymbol = icon switch
        {
            MessageBoxIcon.Error => "✖",
            MessageBoxIcon.Warning => "⚠",
            MessageBoxIcon.Question => "?",
            _ => "i"
        };

        Color badgeColor = icon switch
        {
            MessageBoxIcon.Error => theme.ErrorText,
            MessageBoxIcon.Warning => theme.BrandOrange,
            MessageBoxIcon.Question => theme.BrandOrange,
            _ => theme.BrandRed
        };

        var picBadge = new Panel
        {
            Location = new Point(24, 28),
            Size = new Size(42, 42),
            BackColor = Color.Transparent
        };
        picBadge.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var bgBrush = new SolidBrush(badgeColor);
            e.Graphics.FillEllipse(bgBrush, 1, 1, 39, 39);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI", 16F, FontStyle.Bold);
            e.Graphics.DrawString(badgeSymbol, font, Brushes.White, new RectangleF(1, 1, 39, 39), sf);
        };
        Controls.Add(picBadge);

        int textWidth = 370;
        var textFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        Size measured = TextRenderer.MeasureText(
            message,
            textFont,
            new Size(textWidth, 0),
            TextFormatFlags.WordBreak);

        int textHeight = Math.Max(46, measured.Height + 8);
        var lblMessage = new Label
        {
            Text = message,
            Font = textFont,
            ForeColor = theme.TextPrimary,
            Location = new Point(80, 26),
            Size = new Size(textWidth, textHeight),
            BackColor = Color.Transparent
        };
        Controls.Add(lblMessage);

        int footerTop = Math.Max(92, lblMessage.Bottom + 20);
        var footerPanel = new Panel
        {
            Location = new Point(0, footerTop),
            Size = new Size(ClientSize.Width, 62),
            BackColor = theme.FooterBg,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        AddButtons(footerPanel, buttons, theme);
        Controls.Add(footerPanel);

        ClientSize = new Size(464, footerTop + 62);
        HandleCreated += (s, e) => AppConfigAndStorage.ApplyWindowTitleBarTheme(Handle, theme.IsDark);
    }

    private void AddButtons(Panel footerPanel, MessageBoxButtons buttons, ThemePalette theme)
    {
        int rightX = 440;
        int btnY = 13;

        Button MakeBtn(string text, DialogResult result, bool primary)
        {
            var btn = new Button
            {
                Text = text,
                DialogResult = result,
                Font = new Font("Segoe UI", 9.75F, primary ? FontStyle.Bold : FontStyle.Regular),
                Size = new Size(104, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? theme.BrandRed : theme.SecondaryBtnBg,
                ForeColor = primary ? Color.White : theme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = primary ? 0 : 1;
            btn.FlatAppearance.BorderColor = theme.SecondaryBtnBorder;
            return btn;
        }

        if (buttons == MessageBoxButtons.YesNoCancel)
        {
            var btnCancel = MakeBtn("Cancelar", DialogResult.Cancel, false);
            var btnNo = MakeBtn("Não", DialogResult.No, false);
            var btnYes = MakeBtn("Sim", DialogResult.Yes, true);

            btnCancel.Location = new Point(rightX - 104, btnY);
            btnNo.Location = new Point(rightX - 218, btnY);
            btnYes.Location = new Point(rightX - 332, btnY);

            footerPanel.Controls.AddRange(new Control[] { btnYes, btnNo, btnCancel });
            AcceptButton = btnYes;
            CancelButton = btnCancel;
        }
        else if (buttons == MessageBoxButtons.YesNo)
        {
            var btnNo = MakeBtn("Não", DialogResult.No, false);
            var btnYes = MakeBtn("Sim", DialogResult.Yes, true);

            btnNo.Location = new Point(rightX - 104, btnY);
            btnYes.Location = new Point(rightX - 218, btnY);

            footerPanel.Controls.AddRange(new Control[] { btnYes, btnNo });
            AcceptButton = btnYes;
            CancelButton = btnNo;
        }
        else
        {
            var btnOk = MakeBtn("OK", DialogResult.OK, true);
            btnOk.Location = new Point(rightX - 104, btnY);

            footerPanel.Controls.Add(btnOk);
            AcceptButton = btnOk;
            CancelButton = btnOk;
        }
    }

    public static DialogResult Show(string message, string title, MessageBoxIcon icon = MessageBoxIcon.Information)
        => Show(null, message, title, MessageBoxButtons.OK, icon);

    public static DialogResult Show(IWin32Window? owner, string message, string title, MessageBoxIcon icon = MessageBoxIcon.Information)
        => Show(owner, message, title, MessageBoxButtons.OK, icon);

    public static DialogResult Show(IWin32Window? owner, string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        using var dlg = new ThemedMessageDialog(message, title, buttons, icon);
        if (owner == null)
        {
            dlg.StartPosition = FormStartPosition.CenterScreen;
            return dlg.ShowDialog();
        }
        return dlg.ShowDialog(owner);
    }
}
