using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

public class CandidateNameDialog : Form
{
    private static readonly Color BrandRed = Color.FromArgb(223, 70, 42);      // #DF462A
    private static readonly Color BrandOrange = Color.FromArgb(241, 128, 36);  // #F18024
    private static readonly Color BrandYellow = Color.FromArgb(252, 195, 43);  // #FCC32B
    private static readonly Color WarmBg = Color.FromArgb(255, 249, 245);      // #FFF9F5
    private static readonly Color TextDark = Color.FromArgb(45, 37, 34);

    private readonly TextBox _txtFullName;
    private readonly Label _lblError;

    public string CandidateFullName => _txtFullName.Text.Trim();

    public CandidateNameDialog(string selectedExamName)
    {
        Text = "Identificação do Candidato - Rainha";
        Size = new Size(500, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = WarmBg;

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
                BrandRed,
                BrandYellow,
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, topBanner.ClientRectangle);
        };
        Controls.Add(topBanner);

        var lblTitle = new Label
        {
            Text = "Identificação para Início da Prova",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = BrandRed,
            AutoSize = false,
            Location = new Point(28, 24),
            Size = new Size(430, 30)
        };
        Controls.Add(lblTitle);

        var lblExam = new Label
        {
            Text = $"Prova selecionada: {selectedExamName}",
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
            ForeColor = BrandOrange,
            AutoSize = false,
            Location = new Point(28, 56),
            Size = new Size(430, 26)
        };
        Controls.Add(lblExam);

        var lblPrompt = new Label
        {
            Text = "Digite seu NOME COMPLETO abaixo:\n(Seu nome será usado para salvar sua prova automaticamente)",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            ForeColor = TextDark,
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
            PlaceholderText = "Ex: Maria Aparecida da Silva"
        };
        Controls.Add(_txtFullName);

        _lblError = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 30, 30),
            AutoSize = false,
            Location = new Point(28, 178),
            Size = new Size(428, 22)
        };
        Controls.Add(_lblError);

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            Location = new Point(216, 218),
            Size = new Size(110, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextDark,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(210, 195, 188);
        btnCancel.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(btnCancel);

        var btnConfirm = new Button
        {
            Text = "Iniciar Prova",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(336, 218),
            Size = new Size(120, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnConfirm.FlatAppearance.BorderSize = 0;
        btnConfirm.Click += BtnConfirm_Click;
        Controls.Add(btnConfirm);

        AcceptButton = btnConfirm;
        CancelButton = btnCancel;
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
