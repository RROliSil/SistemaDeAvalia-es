using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace RainhaLauncherProvas;

public class MainForm : Form
{
    // Paleta oficial extraída do logo Rainha (Desde 2007)
    private static readonly Color BrandRed = Color.FromArgb(223, 70, 42);        // #DF462A
    private static readonly Color BrandOrange = Color.FromArgb(241, 128, 36);    // #F18024
    private static readonly Color BrandYellow = Color.FromArgb(252, 195, 43);    // #FCC32B
    private static readonly Color WarmBg = Color.FromArgb(255, 249, 245);        // #FFF9F5
    private static readonly Color CardBg = Color.White;
    private static readonly Color CardBorder = Color.FromArgb(240, 215, 205);
    private static readonly Color CardHoverBg = Color.FromArgb(255, 242, 234);
    private static readonly Color TextDark = Color.FromArgb(45, 37, 34);
    private static readonly Color TextMuted = Color.FromArgb(120, 105, 98);

    private readonly Panel _selectionContainer;
    private readonly FlowLayoutPanel _examsListPanel;
    private readonly Panel _activeSessionPanel;
    private readonly Label _lblActiveCandidate;
    private readonly Label _lblActiveExam;
    private readonly Label _lblActiveFile;
    private readonly Label _lblTimer;
    private readonly System.Windows.Forms.Timer _uiTimer;

    private ExcelSessionManager? _currentSession;

    public MainForm()
    {
        Text = "Rainha - Central de Provas de Excel";
        Size = new Size(760, 620);
        MinimumSize = new Size(720, 580);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = WarmBg;
        DoubleBuffered = true;

        Icon? appIcon = AppConfigAndStorage.LoadEmbeddedIcon();
        if (appIcon != null)
            Icon = appIcon;

        // 1. Cabeçalho Superior com Logo Rainha
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 135,
            BackColor = Color.White
        };

        var picLogo = new PictureBox
        {
            Size = new Size(220, 96),
            Location = new Point(28, 14),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        Image? logoImg = AppConfigAndStorage.LoadEmbeddedLogo();
        if (logoImg != null)
            picLogo.Image = logoImg;
        headerPanel.Controls.Add(picLogo);

        var lblHeaderTitle = new Label
        {
            Text = "Sistema de Avaliação Prática de Excel",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = BrandRed,
            AutoSize = false,
            Location = new Point(265, 28),
            Size = new Size(460, 34)
        };
        headerPanel.Controls.Add(lblHeaderTitle);

        var lblHeaderSubtitle = new Label
        {
            Text = "Selecione abaixo a prova que você irá realizar.\nAo finalizar ou fechar o Excel, sua prova será salva automaticamente com seu nome.",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            ForeColor = TextMuted,
            AutoSize = false,
            Location = new Point(265, 64),
            Size = new Size(460, 44)
        };
        headerPanel.Controls.Add(lblHeaderSubtitle);

        // Faixa tricolor (Vermelho -> Laranja -> Amarelo Rainha) na base do cabeçalho
        var stripePanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 7
        };
        stripePanel.Paint += (s, e) =>
        {
            if (stripePanel.Width <= 0 || stripePanel.Height <= 0)
                return;
            using var brush = new LinearGradientBrush(
                stripePanel.ClientRectangle,
                BrandRed,
                BrandYellow,
                LinearGradientMode.Horizontal);
            var blend = new ColorBlend
            {
                Colors = new[] { BrandRed, BrandOrange, BrandYellow },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            brush.InterpolationColors = blend;
            e.Graphics.FillRectangle(brush, stripePanel.ClientRectangle);
        };
        headerPanel.Controls.Add(stripePanel);

        // 2. Rodapé com ícone de Cadeado (Acesso Restrito por Senha) e ações básicas
        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.FromArgb(248, 238, 232),
            Padding = new Padding(20, 10, 20, 10)
        };

        var btnAdminLock = new Button
        {
            Text = "🔒",
            Font = new Font("Segoe UI Emoji", 13F, FontStyle.Regular),
            Location = new Point(24, 10),
            Size = new Size(46, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnAdminLock.FlatAppearance.BorderSize = 0;
        var toolTip = new ToolTip();
        toolTip.SetToolTip(btnAdminLock, "Acesso Restrito (Provas Feitas, Pasta de Templates e Senha)");
        btnAdminLock.Click += (s, e) =>
        {
            using var lockDlg = new AdminLockDialog();
            lockDlg.ShowDialog(this);
            LoadExamTemplates();
        };
        footerPanel.Controls.Add(btnAdminLock);

        var btnRefresh = CreateFooterButton("🔄 Atualizar Lista", Color.White, TextDark);
        btnRefresh.Location = new Point(445, 11);
        btnRefresh.Size = new Size(140, 34);
        btnRefresh.Click += (s, e) => LoadExamTemplates();
        footerPanel.Controls.Add(btnRefresh);

        var btnShortcut = CreateFooterButton("📌 Atalho Desktop", Color.White, BrandOrange);
        btnShortcut.Location = new Point(595, 11);
        btnShortcut.Size = new Size(130, 34);
        btnShortcut.Click += (s, e) =>
        {
            string exePath = Environment.ProcessPath ?? Path.Combine(AppConfigAndStorage.BaseDirectory, AppConfigAndStorage.InstalledExeName);
            AppConfigAndStorage.CreateDesktopShortcut(exePath, AppConfigAndStorage.BaseDirectory);
            MessageBox.Show(
                "Atalho 'Testes - Rainha' criado/atualizado com sucesso na Área de Trabalho!",
                "Atalho Criado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };
        footerPanel.Controls.Add(btnShortcut);

        // 3. Container Principal de Seleção de Provas
        _selectionContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 18, 26, 14),
            BackColor = WarmBg
        };

        var lblSectionTitle = new Label
        {
            Text = "📋 Escolha a Prova para Iniciar:",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = TextDark,
            Dock = DockStyle.Top,
            Height = 34
        };

        _examsListPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = WarmBg,
            Padding = new Padding(0, 6, 0, 6)
        };
        _examsListPanel.SizeChanged += (s, e) => ResizeExamCards();

        _selectionContainer.Controls.Add(_examsListPanel);
        _selectionContainer.Controls.Add(lblSectionTitle);

        // 4. Painel de "Prova em Andamento" (exibido enquanto o Excel está aberto)
        _activeSessionPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = WarmBg,
            Visible = false,
            Padding = new Padding(40, 28, 40, 28)
        };

        var activeCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };
        activeCard.Paint += (s, e) =>
        {
            using var pen = new Pen(BrandOrange, 2);
            e.Graphics.DrawRectangle(pen, 1, 1, activeCard.Width - 3, activeCard.Height - 3);
        };

        var lblInProgressBadge = new Label
        {
            Text = "⏳ PROVA EM ANDAMENTO NO EXCEL",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = BrandRed,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 52
        };

        _lblActiveCandidate = new Label
        {
            Text = "Candidato: -",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = TextDark,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 34
        };

        _lblActiveExam = new Label
        {
            Text = "Prova: -",
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = BrandOrange,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };

        _lblActiveFile = new Label
        {
            Text = "Arquivo salvo automaticamente em: -",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 28
        };

        _lblTimer = new Label
        {
            Text = "Tempo decorrido: 00:00:00",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = TextDark,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 54
        };

        var lblSaveNote = new Label
        {
            Text = "💡 Você pode fechar o Excel normalmente no 'X' ao terminar ou clicar no botão abaixo.\nSua prova será salva automaticamente na pasta de Provas Feitas com seu nome completo.",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            ForeColor = TextMuted,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 48
        };

        var btnFinishWrapper = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68
        };

        var btnFinishAndSave = new Button
        {
            Text = "✅ Finalizar e Salvar Prova Agora",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Size = new Size(340, 48),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnFinishAndSave.FlatAppearance.BorderSize = 0;
        btnFinishWrapper.SizeChanged += (s, e) =>
        {
            btnFinishAndSave.Location = new Point((btnFinishWrapper.Width - btnFinishAndSave.Width) / 2, 10);
        };
        btnFinishAndSave.Click += (s, e) =>
        {
            _currentSession?.SaveAndCloseSession();
        };
        btnFinishWrapper.Controls.Add(btnFinishAndSave);

        activeCard.Controls.Add(btnFinishWrapper);
        activeCard.Controls.Add(lblSaveNote);
        activeCard.Controls.Add(_lblTimer);
        activeCard.Controls.Add(_lblActiveFile);
        activeCard.Controls.Add(_lblActiveExam);
        activeCard.Controls.Add(_lblActiveCandidate);
        activeCard.Controls.Add(lblInProgressBadge);
        _activeSessionPanel.Controls.Add(activeCard);

        Controls.Add(_activeSessionPanel);
        Controls.Add(_selectionContainer);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);

        _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _uiTimer.Tick += UiTimer_Tick;

        Load += (s, e) => LoadExamTemplates();
        FormClosing += MainForm_FormClosing;
    }

    private Button CreateFooterButton(string text, Color bg, Color fg)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = fg,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(220, 195, 185);
        return btn;
    }

    public void LoadExamTemplates()
    {
        _examsListPanel.SuspendLayout();
        _examsListPanel.Controls.Clear();

        List<ExamTemplateItem> templates = AppConfigAndStorage.GetAvailableTemplates();

        if (templates.Count == 0)
        {
            var lblEmpty = new Label
            {
                Text = "Nenhuma prova (.xlsx) encontrada na pasta Templates.\nClique em '📂 Pasta de Templates' abaixo para adicionar arquivos de prova.",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(650, 90),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _examsListPanel.Controls.Add(lblEmpty);
        }
        else
        {
            foreach (var item in templates)
            {
                Panel card = CreateExamCard(item);
                _examsListPanel.Controls.Add(card);
            }
        }

        ResizeExamCards();
        _examsListPanel.ResumeLayout();
    }

    private Panel CreateExamCard(ExamTemplateItem item)
    {
        int cardWidth = Math.Max(640, _examsListPanel.ClientSize.Width - 28);
        var card = new Panel
        {
            Size = new Size(cardWidth, 78),
            Margin = new Padding(4, 6, 4, 6),
            BackColor = CardBg,
            Cursor = Cursors.Hand
        };

        card.Paint += (s, e) =>
        {
            using var borderPen = new Pen(CardBorder, 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);

            // Barra lateral esquerda nas cores da Rainha
            using var accentBrush = new LinearGradientBrush(
                new Rectangle(0, 0, 7, card.Height),
                BrandRed,
                BrandOrange,
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 7, card.Height);
        };

        var lblIcon = new Label
        {
            Text = "📊",
            Font = new Font("Segoe UI Emoji", 20F, FontStyle.Regular),
            Location = new Point(20, 18),
            Size = new Size(46, 44),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var lblTitle = new Label
        {
            Text = item.DisplayName,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = TextDark,
            Location = new Point(70, 14),
            Size = new Size(410, 28),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var lblMeta = new Label
        {
            Text = $"Modelo Original Protegido (.xlsx) • Clique para informar seu nome e iniciar",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = TextMuted,
            Location = new Point(70, 44),
            Size = new Size(430, 20),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var btnSelect = new Button
        {
            Text = "Fazer Prova  ➔",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(148, 40),
            Location = new Point(card.Width - 168, 19),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandRed,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnSelect.FlatAppearance.BorderSize = 0;

        void TriggerSelection(object? s, EventArgs e) => OnExamSelected(item);

        card.Click += TriggerSelection;
        lblIcon.Click += TriggerSelection;
        lblTitle.Click += TriggerSelection;
        lblMeta.Click += TriggerSelection;
        btnSelect.Click += TriggerSelection;

        void OnMouseEnterCard(object? s, EventArgs e) => card.BackColor = CardHoverBg;
        void OnMouseLeaveCard(object? s, EventArgs e) => card.BackColor = CardBg;

        card.MouseEnter += OnMouseEnterCard;
        card.MouseLeave += OnMouseLeaveCard;
        lblTitle.MouseEnter += OnMouseEnterCard;
        lblTitle.MouseLeave += OnMouseLeaveCard;
        lblMeta.MouseEnter += OnMouseEnterCard;
        lblMeta.MouseLeave += OnMouseLeaveCard;

        card.Controls.Add(btnSelect);
        card.Controls.Add(lblMeta);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblIcon);

        return card;
    }

    private void ResizeExamCards()
    {
        int targetWidth = Math.Max(620, _examsListPanel.ClientSize.Width - 26);
        foreach (Control ctrl in _examsListPanel.Controls)
        {
            if (ctrl is Panel card)
            {
                card.Width = targetWidth;
            }
        }
    }

    private void OnExamSelected(ExamTemplateItem item)
    {
        if (_currentSession != null)
        {
            MessageBox.Show(
                "Já existe uma prova em andamento no Excel. Finalize a prova atual antes de iniciar outra.",
                "Prova em Andamento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new CandidateNameDialog(item.DisplayName);
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        string candidateName = dlg.CandidateFullName;
        StartExamForCandidate(candidateName, item);
    }

    private void StartExamForCandidate(string candidateName, ExamTemplateItem template)
    {
        try
        {
            string targetFilePath = AppConfigAndStorage.BuildCandidateOutputPath(candidateName, template.FilePath);

            var session = new ExcelSessionManager(
                candidateName: candidateName,
                examName: template.DisplayName,
                templatePath: template.FilePath,
                targetFilePath: targetFilePath);

            session.SessionCompleted += () =>
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                BeginInvoke(new Action(() => OnExamSessionCompleted(session)));
            };

            _currentSession = session;
            session.StartSession();

            // Atualiza tela para modo "Prova em Andamento"
            _lblActiveCandidate.Text = $"Candidato(a): {candidateName}";
            _lblActiveExam.Text = $"Prova: {template.DisplayName}";
            _lblActiveFile.Text = $"Arquivo em Provas Feitas: {Path.GetFileName(targetFilePath)}";
            _lblTimer.Text = "Tempo decorrido: 00:00:00";

            _selectionContainer.Visible = false;
            _activeSessionPanel.Visible = true;
            _uiTimer.Start();
        }
        catch (Exception ex)
        {
            _currentSession?.Dispose();
            _currentSession = null;

            MessageBox.Show(
                $"Não foi possível abrir a prova no Microsoft Excel:\n\n{ex.Message}",
                "Erro ao Iniciar Prova",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void UiTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentSession == null)
            return;

        TimeSpan elapsed = DateTime.Now - _currentSession.StartTime;
        _lblTimer.Text = $"Tempo decorrido: {elapsed:hh\\:mm\\:ss}";
    }

    private void OnExamSessionCompleted(ExcelSessionManager finishedSession)
    {
        _uiTimer.Stop();

        DateTime endTime = finishedSession.EndTime ?? DateTime.Now;
        AppConfigAndStorage.RecordExamHistory(
            finishedSession.CandidateName,
            finishedSession.ExamName,
            finishedSession.TargetFilePath,
            finishedSession.StartTime,
            endTime);

        _currentSession?.Dispose();
        _currentSession = null;

        _activeSessionPanel.Visible = false;
        _selectionContainer.Visible = true;

        // Traz o Launcher para frente e confirma salvamento
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        Activate();

        MessageBox.Show(
            this,
            $"Prova finalizada e salva com sucesso!\n\n" +
            $"👤 Candidato(a): {finishedSession.CandidateName}\n" +
            $"📊 Prova: {finishedSession.ExamName}\n" +
            $"💾 Arquivo salvo: {Path.GetFileName(finishedSession.TargetFilePath)}\n" +
            $"📁 Pasta: {AppConfigAndStorage.CompletedExamsDirectory}",
            "Prova Salva com Sucesso - Rainha",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void OpenFolderInExplorer(string folderPath)
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
            MessageBox.Show($"Não foi possível abrir a pasta:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_currentSession != null)
        {
            var resp = MessageBox.Show(
                "Existe uma prova aberta no Excel no momento.\nDeseja salvar automaticamente e fechar a prova antes de sair?",
                "Prova em Andamento",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (resp == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (resp == DialogResult.Yes)
            {
                _currentSession.SaveAndCloseSession();
            }
        }
    }
}
