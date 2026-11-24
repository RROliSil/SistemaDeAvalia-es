using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

public class AdminLockDialog : Form
{
    private readonly ThemePalette _theme;

    private readonly Panel _loginPanel;
    private readonly TextBox _txtPassword;
    private readonly Label _lblLoginError;
    private readonly Button _btnUnlock;

    private readonly Panel _adminPanel;
    private readonly TextBox _txtNewPassword;
    private readonly Label _lblPasswordStatus;

    public AdminLockDialog()
    {
        _theme = AppConfigAndStorage.GetActiveTheme();

        Text = "🔒 Acesso Restrito - Rainha";
        Size = new Size(480, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = _theme.WindowBg;

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
                _theme.BrandRed,
                _theme.BrandYellow,
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
            BackColor = _theme.WindowBg
        };

        var lblLockTitle = new Label
        {
            Text = "🔒 Área Restrita do Avaliador",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = _theme.BrandRed,
            Location = new Point(28, 22),
            Size = new Size(410, 30)
        };
        _loginPanel.Controls.Add(lblLockTitle);

        var lblLockSub = new Label
        {
            Text = "Digite a senha de administrador para acessar as pastas de\nAvaliações Feitas, Pasta de Templates e configurações de senha:",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = _theme.TextPrimary,
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
            BackColor = _theme.InputBg,
            ForeColor = _theme.TextPrimary,
            PlaceholderText = "Digite a senha..."
        };
        _loginPanel.Controls.Add(_txtPassword);

        _lblLoginError = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = _theme.ErrorText,
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
            BackColor = _theme.SecondaryBtnBg,
            ForeColor = _theme.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = _theme.SecondaryBtnBorder;
        btnCancel.Click += (s, e) => Close();
        _loginPanel.Controls.Add(btnCancel);

        _btnUnlock = new Button
        {
            Text = "🔓 Acessar",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(320, 184),
            Size = new Size(116, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.BrandRed,
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
            BackColor = _theme.WindowBg,
            Visible = false
        };

        var lblAdminTitle = new Label
        {
            Text = "🔓 Painel de Pastas e Senha - Rainha",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = _theme.BrandRed,
            Location = new Point(26, 18),
            Size = new Size(415, 28)
        };
        _adminPanel.Controls.Add(lblAdminTitle);

        var lblFoldersHeader = new Label
        {
            Text = "Acesso às Pastas do Sistema:",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
            ForeColor = _theme.TextPrimary,
            Location = new Point(26, 54),
            Size = new Size(415, 22)
        };
        _adminPanel.Controls.Add(lblFoldersHeader);

        var btnOpenCompleted = new Button
        {
            Text = "📁 Abrir Pasta de Avaliações Feitas",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Location = new Point(26, 80),
            Size = new Size(412, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.BrandRed,
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
            BackColor = _theme.SecondaryBtnBg,
            ForeColor = _theme.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnOpenTemplates.FlatAppearance.BorderColor = _theme.BrandOrange;
        btnOpenTemplates.Click += (s, e) => OpenFolder(AppConfigAndStorage.TemplatesDirectory);
        _adminPanel.Controls.Add(btnOpenTemplates);

        // Divisor e seção de Alterar / Resetar Senha
        var divider = new Panel
        {
            Location = new Point(26, 192),
            Size = new Size(412, 1),
            BackColor = _theme.CardBorder
        };
        _adminPanel.Controls.Add(divider);

        var lblPwdHeader = new Label
        {
            Text = "🔑 Gerenciar / Resetar Senha do Cadeado:",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
            ForeColor = _theme.TextPrimary,
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
            BackColor = _theme.InputBg,
            ForeColor = _theme.TextPrimary,
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
            BackColor = _theme.BrandOrange,
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
            BackColor = _theme.SecondaryBtnBg,
            ForeColor = _theme.BrandRed,
            Cursor = Cursors.Hand
        };
        btnResetDefault.FlatAppearance.BorderColor = _theme.SecondaryBtnBorder;
        btnResetDefault.Click += BtnResetDefault_Click;
        _adminPanel.Controls.Add(btnResetDefault);

        var btnCloseAdmin = new Button
        {
            Text = "Fechar",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Location = new Point(312, 272),
            Size = new Size(126, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.SecondaryBtnBg,
            ForeColor = _theme.TextPrimary,
            Cursor = Cursors.Hand
        };
        btnCloseAdmin.FlatAppearance.BorderColor = _theme.SecondaryBtnBorder;
        btnCloseAdmin.Click += (s, e) => Close();
        _adminPanel.Controls.Add(btnCloseAdmin);

        _lblPasswordStatus = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = _theme.SuccessText,
            Location = new Point(26, 314),
            Size = new Size(412, 22)
        };
        _adminPanel.Controls.Add(_lblPasswordStatus);

        Controls.Add(_adminPanel);
        Controls.Add(_loginPanel);

        AcceptButton = _btnUnlock;
        CancelButton = btnCancel;
        HandleCreated += (s, e) => AppConfigAndStorage.ApplyWindowTitleBarTheme(Handle, _theme.IsDark);
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
            _lblPasswordStatus.ForeColor = _theme.ErrorText;
            _lblPasswordStatus.Text = "⚠ A nova senha deve ter pelo menos 4 caracteres.";
            _txtNewPassword.Focus();
            return;
        }

        AppConfigAndStorage.SetAdminPassword(newPwd);
        _txtNewPassword.Clear();
        _lblPasswordStatus.ForeColor = _theme.SuccessText;
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
            _lblPasswordStatus.ForeColor = _theme.SuccessText;
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
