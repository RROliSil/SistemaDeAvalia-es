using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

public class InstallerForm : Form
{
    private static readonly Color BrandRed = Color.FromArgb(223, 70, 42);
    private static readonly Color BrandOrange = Color.FromArgb(241, 128, 36);
    private static readonly Color BrandYellow = Color.FromArgb(252, 195, 43);
    private static readonly Color WarmBg = Color.FromArgb(255, 249, 245);
    private static readonly Color TextDark = Color.FromArgb(45, 37, 34);

    private readonly TextBox _txtInstallDir;
    private readonly CheckBox _chkOpenAfterInstall;

    public InstallerForm()
    {
        Text = "Instalador - Rainha Sistema de Provas de Excel";
        Size = new Size(560, 410);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        BackColor = WarmBg;

        Icon? appIcon = AppConfigAndStorage.LoadEmbeddedIcon();
        if (appIcon != null)
            Icon = appIcon;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 115,
            BackColor = Color.White
        };

        var picLogo = new PictureBox
        {
            Size = new Size(180, 82),
            Location = new Point(22, 12),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        picLogo.Image = AppConfigAndStorage.LoadEmbeddedLogo();
        header.Controls.Add(picLogo);

        var lblTitle = new Label
        {
            Text = "Instalador Único - Provas de Excel",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = BrandRed,
            Location = new Point(215, 24),
            Size = new Size(320, 28)
        };
        header.Controls.Add(lblTitle);

        var lblSub = new Label
        {
            Text = "Configura automaticamente o Launcher, pastas de Templates,\npasta de Provas Feitas e cria o atalho na Área de Trabalho.",
            Font = new Font("Segoe UI", 9.25F, FontStyle.Regular),
            ForeColor = TextDark,
            Location = new Point(215, 54),
            Size = new Size(320, 42)
        };
        header.Controls.Add(lblSub);

        var stripe = new Panel { Dock = DockStyle.Bottom, Height = 6 };
        stripe.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(stripe.ClientRectangle, BrandRed, BrandYellow, LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, stripe.ClientRectangle);
        };
        header.Controls.Add(stripe);
        Controls.Add(header);

        var lblDir = new Label
        {
            Text = "Pasta de Instalação do Sistema (Templates e Provas Feitas ficarão aqui):",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
            ForeColor = TextDark,
            Location = new Point(28, 136),
            Size = new Size(490, 24)
        };
        Controls.Add(lblDir);

        _txtInstallDir = new TextBox
        {
            Text = AppConfigAndStorage.DefaultInstallDir,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
            Location = new Point(28, 164),
            Size = new Size(490, 28)
        };
        Controls.Add(_txtInstallDir);

        var lblFeatures = new Label
        {
            Text = "✔ Todas as dependências já estão embutidas neste executável\n" +
                   "✔ Cria as pastas 'Templates' (com modelos de prova) e 'Provas Feitas'\n" +
                   "✔ Cria o atalho 'Provas de Excel - Rainha' na Área de Trabalho",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(80, 70, 65),
            Location = new Point(28, 204),
            Size = new Size(490, 64)
        };
        Controls.Add(lblFeatures);

        _chkOpenAfterInstall = new CheckBox
        {
            Text = "Abrir o Launcher de Provas após concluir a instalação",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
            ForeColor = TextDark,
            Checked = true,
            Location = new Point(28, 274),
            Size = new Size(450, 26)
        };
        Controls.Add(_chkOpenAfterInstall);

        var btnInstall = new Button
        {
            Text = "🚀 Instalar Agora (1 Clique)",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            Location = new Point(28, 312),
            Size = new Size(490, 46),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnInstall.FlatAppearance.BorderSize = 0;
        btnInstall.Click += BtnInstall_Click;
        Controls.Add(btnInstall);
    }

    private void BtnInstall_Click(object? sender, EventArgs e)
    {
        try
        {
            string targetDir = _txtInstallDir.Text.Trim();
            if (string.IsNullOrWhiteSpace(targetDir))
                targetDir = AppConfigAndStorage.DefaultInstallDir;

            string finalDir = AppConfigAndStorage.InstallSystemAndCreateShortcut(targetDir);

            MessageBox.Show(
                this,
                $"Instalação concluída com sucesso!\n\n" +
                $"📁 Pasta do Sistema: {finalDir}\n" +
                $"📂 Pasta de Templates: {Path.Combine(finalDir, "Templates")}\n" +
                $"💾 Pasta de Provas Feitas: {Path.Combine(finalDir, "Provas Feitas")}\n" +
                $"🖥️ Atalho criado na Área de Trabalho: 'Provas de Excel - Rainha'",
                "Instalação Concluída - Rainha",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            if (_chkOpenAfterInstall.Checked)
            {
                string installedExe = Path.Combine(finalDir, AppConfigAndStorage.InstalledExeName);
                if (File.Exists(installedExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = installedExe,
                        WorkingDirectory = finalDir,
                        UseShellExecute = true
                    });
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Erro durante a instalação:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
