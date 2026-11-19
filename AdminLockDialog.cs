using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

public class AdminLockDialog : Form
{
    private static readonly Color BrandRed = Color.FromArgb(223, 70, 42);      // #DF462A
    private static readonly Color BrandOrange = Color.FromArgb(241, 128, 36);  // #F18024
    private static readonly Color BrandYellow = Color.FromArgb(252, 195, 43);  // #FCC32B
    private static readonly Color WarmBg = Color.FromArgb(255, 249, 245);      // #FFF9F5
    private static readonly Color TextDark = Color.FromArgb(45, 37, 34);
    private static readonly Color TextMuted = Color.FromArgb(120, 105, 98);

    private readonly Panel _loginPanel;
    private readonly TextBox _txtPassword;
    private readonly Label _lblLoginError;
    private readonly Button _btnUnlock;

    private readonly Panel _adminPanel;
    private readonly TextBox _txtNewPassword;
    private readonly Label _lblPasswordStatus;

    public AdminLockDialog()
    {
        Text = "🔒 Acesso Restrito - Rainha";
        Size = new Size(480, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = WarmBg;

        Icon? appIcon = AppConfigAndStorage.LoadEmbeddedIcon();
        if (appIcon != null)
            Icon = appIcon;

        // Barra superior tricolor Rainha
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

        // =====================================================================
        // ETAPA 1: PAINEL DE SENHA (CADEADO FECHADO)
        // =====================================================================
        _loginPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = WarmBg
        };

        var lblLockTitle = new Label
        {
            Text = "🔒 Área Restrita do Avaliador",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = BrandRed,
            Location = new Point(28, 22),
            Size = new Size(410, 30)
        };
        _loginPanel.Controls.Add(lblLockTitle);

        var lblLockSub = new Label
        {
            Text = "Digite a senha de administrador para acessar as pastas de\nProvas Feitas, Pasta de Templates e configurações de senha:",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = TextDark,
            Location = new Point(28, 56),
            Size = new Size(410, 42)
        };
        _loginPanel.Controls.Add(lblLockSub);

        _txtPassword = new TextBox
        {
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Location = new Point(28, 108),
            Size = new Size(408, 34),
            UseSystemPasswordChar = true,
            MaxLength = 40,
            PlaceholderText = "Digite a senha..."
        };
        _loginPanel.Controls.Add(_txtPassword);

        _lblLoginError = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 30, 30),
            Location = new Point(28, 148),
            Size = new Size(408, 22)
        };
        _loginPanel.Controls.Add(_lblLoginError);

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            Location = new Point(204, 184),
            Size = new Size(108, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextDark,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(215, 195, 185);
        btnCancel.Click += (s, e) => Close();
        _loginPanel.Controls.Add(btnCancel);

        _btnUnlock = new Button
        {
            Text = "🔓 Acessar",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(320, 184),
            Size = new Size(116, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _btnUnlock.FlatAppearance.BorderSize = 0;
        _btnUnlock.Click += BtnUnlock_Click;
        _loginPanel.Controls.Add(_btnUnlock);

        // =====================================================================
        // ETAPA 2: PAINEL ADMINISTRATIVO DESBLOQUEADO
        // =====================================================================
        _adminPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = WarmBg,
            Visible = false
        };

        var lblAdminTitle = new Label
        {
            Text = "🔓 Painel de Pastas e Senha - Rainha",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = BrandRed,
            Location = new Point(26, 18),
            Size = new Size(415, 28)
        };
        _adminPanel.Controls.Add(lblAdminTitle);

        var lblFoldersHeader = new Label
        {
            Text = "Acesso às Pastas do Sistema:",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
            ForeColor = TextDark,
            Location = new Point(26, 54),
            Size = new Size(415, 22)
        };
        _adminPanel.Controls.Add(lblFoldersHeader);

        var btnOpenCompleted = new Button
        {
            Text = "📁 Abrir Pasta de Provas Feitas",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(26, 80),
            Size = new Size(412, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnOpenCompleted.FlatAppearance.BorderSize = 0;
        btnOpenCompleted.Click += (s, e) => OpenFolder(AppConfigAndStorage.CompletedExamsDirectory);
        _adminPanel.Controls.Add(btnOpenCompleted);

        var btnOpenTemplates = new Button
        {
            Text = "📂 Pasta de Templates",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(26, 132),
            Size = new Size(412, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextDark,
            Cursor = Cursors.Hand
        };
        btnOpenTemplates.FlatAppearance.BorderColor = BrandOrange;
        btnOpenTemplates.Click += (s, e) => OpenFolder(AppConfigAndStorage.TemplatesDirectory);
        _adminPanel.Controls.Add(btnOpenTemplates);

        // Divisor e seção de Alterar / Resetar Senha
        var divider = new Panel
        {
            Location = new Point(26, 192),
            Size = new Size(412, 1),
            BackColor = Color.FromArgb(230, 205, 195)
        };
        _adminPanel.Controls.Add(divider);

        var lblPwdHeader = new Label
        {
            Text = "🔑 Gerenciar / Resetar Senha do Cadeado:",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
            ForeColor = TextDark,
            Location = new Point(26, 204),
            Size = new Size(412, 22)
        };
        _adminPanel.Controls.Add(lblPwdHeader);

        _txtNewPassword = new TextBox
        {
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            Location = new Point(26, 232),
            Size = new Size(248, 30),
            MaxLength = 40,
            PlaceholderText = "Digite uma nova senha..."
        };
        _adminPanel.Controls.Add(_txtNewPassword);

        var btnSaveNewPwd = new Button
        {
            Text = "💾 Salvar Senha",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(282, 231),
            Size = new Size(156, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandOrange,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnSaveNewPwd.FlatAppearance.BorderSize = 0;
        btnSaveNewPwd.Click += BtnSaveNewPwd_Click;
        _adminPanel.Controls.Add(btnSaveNewPwd);

        var btnResetDefault = new Button
        {
            Text = "🔄 Resetar Senha para o Padrão (7272)",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(26, 272),
            Size = new Size(275, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = BrandRed,
            Cursor = Cursors.Hand
        };
        btnResetDefault.FlatAppearance.BorderColor = Color.FromArgb(225, 195, 185);
        btnResetDefault.Click += BtnResetDefault_Click;
        _adminPanel.Controls.Add(btnResetDefault);

        var btnCloseAdmin = new Button
        {
            Text = "Fechar",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Location = new Point(312, 272),
            Size = new Size(126, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = TextDark,
            Cursor = Cursors.Hand
        };
        btnCloseAdmin.FlatAppearance.BorderColor = Color.FromArgb(215, 195, 185);
        btnCloseAdmin.Click += (s, e) => Close();
        _adminPanel.Controls.Add(btnCloseAdmin);

        _lblPasswordStatus = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 135, 60),
            Location = new Point(26, 314),
            Size = new Size(412, 22)
        };
        _adminPanel.Controls.Add(_lblPasswordStatus);

        Controls.Add(_adminPanel);
        Controls.Add(_loginPanel);

        AcceptButton = _btnUnlock;
        CancelButton = btnCancel;
        Shown += (s, e) => _txtPassword.Focus();
    }

    private void BtnUnlock_Click(object? sender, EventArgs e)
    {
        string typed = _txtPassword.Text.Trim();
        if (!AppConfigAndStorage.VerifyAdminPassword(typed))
        {
            _lblLoginError.Text = "⚠ Senha incorreta. Tente novamente.";
            _txtPassword.SelectAll();
            _txtPassword.Focus();
            return;
        }

        // Desbloqueia e expande para o painel administrativo
        _loginPanel.Visible = false;
        Size = new Size(480, 390);
        Text = "🔓 Painel Administrativo - Rainha";
        _adminPanel.Visible = true;
        AcceptButton = null;
    }

    private void BtnSaveNewPwd_Click(object? sender, EventArgs e)
    {
        string newPwd = _txtNewPassword.Text.Trim();
        if (string.IsNullOrWhiteSpace(newPwd) || newPwd.Length < 4)
        {
            _lblPasswordStatus.ForeColor = Color.FromArgb(200, 30, 30);
            _lblPasswordStatus.Text = "⚠ A nova senha deve ter pelo menos 4 caracteres.";
            _txtNewPassword.Focus();
            return;
        }

        AppConfigAndStorage.SetAdminPassword(newPwd);
        _txtNewPassword.Clear();
        _lblPasswordStatus.ForeColor = Color.FromArgb(25, 135, 60);
        _lblPasswordStatus.Text = "✔ Nova senha salva com sucesso!";
    }

    private void BtnResetDefault_Click(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            this,
            "Deseja realmente resetar a senha do cadeado de volta para a senha inicial padrão (7272)?",
            "Confirmar Reset de Senha",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            AppConfigAndStorage.ResetAdminPasswordToDefault();
            _txtNewPassword.Clear();
            _lblPasswordStatus.ForeColor = Color.FromArgb(25, 135, 60);
            _lblPasswordStatus.Text = "✔ Senha resetada para o padrão: 7272";
        }
    }

    private void OpenFolder(string folderPath)
    {
        try
        {
            Directory.CreateDirectory(folderPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível abrir a pasta:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
