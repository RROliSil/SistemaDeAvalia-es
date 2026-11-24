using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

public class CandidateNameDialog : Form
{
    private readonly TextBox _txtFullName;
    private readonly Label _lblError;

    public string CandidateFullName => _txtFullName.Text.Trim();

    public CandidateNameDialog(string selectedExamName)
    {
        ThemePalette theme = AppConfigAndStorage.GetActiveTheme();

        Text = "Identificação do Candidato - Rainha";
        Size = new Size(500, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = theme.WindowBg;

        Icon? appIcon = AppConfigAndStorage.LoadEmbeddedIcon();
        if (appIcon != null)
            Icon = appIcon;

        // Barra superior colorida com gradiente Rainha
        var topBanner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 8
        };
        topBanner.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(
                topBanner.ClientRectangle,
                theme.BrandRed,
                theme.BrandYellow,
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, topBanner.ClientRectangle);
        };
        Controls.Add(topBanner);

        var lblTitle = new Label
        {
            Text = "Identificação para Início da Avaliação",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = theme.BrandRed,
            AutoSize = false,
            Location = new Point(28, 24),
            Size = new Size(430, 30)
        };
        Controls.Add(lblTitle);

        var lblExam = new Label
        {
            Text = $"Avaliação selecionada: {selectedExamName}",
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            ForeColor = theme.BrandOrange,
            AutoSize = false,
            Location = new Point(28, 56),
            Size = new Size(430, 26)
        };
        Controls.Add(lblExam);

        var lblPrompt = new Label
        {
            Text = "Digite seu NOME COMPLETO abaixo:\n(Seu nome será usado para salvar sua avaliação automaticamente)",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            ForeColor = theme.TextPrimary,
            AutoSize = false,
            Location = new Point(28, 92),
            Size = new Size(430, 42)
        };
        Controls.Add(lblPrompt);

        _txtFullName = new TextBox
        {
            Font = new Font("Segoe UI", 12F, FontStyle.Regular),
            Location = new Point(28, 140),
            Size = new Size(428, 34),
            MaxLength = 120,
            BackColor = theme.InputBg,
            ForeColor = theme.TextPrimary,
            PlaceholderText = "Ex: Maria Aparecida da Silva"
        };
        Controls.Add(_txtFullName);

        _lblError = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = theme.ErrorText,
            AutoSize = false,
            Location = new Point(28, 178),
            Size = new Size(428, 22)
        };
        Controls.Add(_lblError);

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            Location = new Point(192, 218),
            Size = new Size(110, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = theme.SecondaryBtnBg,
            ForeColor = theme.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = theme.SecondaryBtnBorder;
        btnCancel.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(btnCancel);

        var btnConfirm = new Button
        {
            Text = "Iniciar Avaliação",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(312, 218),
            Size = new Size(144, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = theme.BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnConfirm.FlatAppearance.BorderSize = 0;
        btnConfirm.Click += BtnConfirm_Click;
        Controls.Add(btnConfirm);

        AcceptButton = btnConfirm;
        CancelButton = btnCancel;
        HandleCreated += (s, e) => AppConfigAndStorage.ApplyWindowTitleBarTheme(Handle, theme.IsDark);
        Shown += (s, e) => _txtFullName.Focus();
    }

    private void BtnConfirm_Click(object? sender, EventArgs e)
    {
        string raw = _txtFullName.Text.Trim();
        string sanitized = AppConfigAndStorage.SanitizeFileName(raw);

        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length < 3)
        {
            _lblError.Text = "⚠ Por favor, informe seu nome completo para continuar.";
            _txtFullName.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
