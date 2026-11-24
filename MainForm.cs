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
    private ThemePalette _theme;

    private readonly ThemedTitleBar _titleBar;
    private readonly Panel _headerPanel;
    private readonly Label _lblHeaderTitle;
    private readonly Label _lblHeaderSubtitle;
    private readonly Panel _footerPanel;
    private readonly Button _btnAdminLock;
    private readonly Button _btnThemeToggle;
    private readonly Button _btnRefresh;
    private readonly Button _btnShortcut;

    private readonly Panel _selectionContainer;
    private readonly Label _lblSectionTitle;
    private readonly FlowLayoutPanel _examsListPanel;

    private readonly Panel _activeSessionPanel;
    private readonly Panel _activeCard;
    private readonly Label _lblInProgressBadge;
    private readonly Label _lblActiveCandidate;
    private readonly Label _lblActiveExam;
    private readonly Label _lblActiveFile;
    private readonly Label _lblTimer;
    private readonly Label _lblSaveNote;
    private readonly Button _btnFinishAndSave;

    private readonly System.Windows.Forms.Timer _uiTimer;

    private ExcelSessionManager? _currentSession;

    public MainForm()
    {
        _theme = AppConfigAndStorage.GetActiveTheme();

        Text = "Rainha - Central de Avaliações de Excel";
        Size = new Size(760, 620);
        MinimumSize = new Size(720, 580);
        FormBorderStyle = FormBorderStyle.None;
        Padding = new Padding(1);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        Icon? appIcon = AppConfigAndStorage.LoadEmbeddedIcon();
        if (appIcon != null)
            Icon = appIcon;

        _titleBar = new ThemedTitleBar(this, _theme, showMaximize: true, showMinimize: true);

        Paint += (s, e) =>
        {
            if (WindowState != FormWindowState.Maximized)
            {
                using var borderPen = new Pen(_theme.CardBorder, 1);
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }
        };

        // 1. Cabeçalho Superior com Logo Rainha
        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 135
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
        _headerPanel.Controls.Add(picLogo);

        _lblHeaderTitle = new Label
        {
            Text = "Sistema de Avaliação Prática de Excel",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            AutoSize = false,
            Location = new Point(265, 28),
            Size = new Size(475, 34)
        };
        _headerPanel.Controls.Add(_lblHeaderTitle);

        _lblHeaderSubtitle = new Label
        {
            Text = "Selecione abaixo a avaliação que você irá realizar.\nAo finalizar ou fechar o Excel, sua avaliação será salva automaticamente.",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            AutoSize = false,
            Location = new Point(265, 64),
            Size = new Size(475, 44)
        };
        _headerPanel.Controls.Add(_lblHeaderSubtitle);

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
                _theme.BrandRed,
                _theme.BrandYellow,
                LinearGradientMode.Horizontal);
            var blend = new ColorBlend
            {
                Colors = new[] { _theme.BrandRed, _theme.BrandOrange, _theme.BrandYellow },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            brush.InterpolationColors = blend;
            e.Graphics.FillRectangle(brush, stripePanel.ClientRectangle);
        };
        _headerPanel.Controls.Add(stripePanel);

        // 2. Rodapé com ícone de Cadeado (Acesso Restrito por Senha), Modo Noturno e ações básicas
        _footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(20, 10, 20, 10)
        };

        _btnAdminLock = new Button
        {
            Text = "🔒",
            Font = new Font("Segoe UI Emoji", 13F, FontStyle.Regular),
            Location = new Point(24, 10),
            Size = new Size(46, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _btnAdminLock.FlatAppearance.BorderSize = 0;
        var toolTip = new ToolTip();
        toolTip.SetToolTip(_btnAdminLock, "Acesso Restrito (Avaliações Feitas, Pasta de Templates e Senha)");
        _btnAdminLock.Click += (s, e) =>
        {
            using var lockDlg = new AdminLockDialog();
            lockDlg.ShowDialog(this);
            LoadExamTemplates();
        };
        _footerPanel.Controls.Add(_btnAdminLock);

        _btnThemeToggle = CreateFooterButton("🌙 Modo Noturno", _theme.SecondaryBtnBg, _theme.TextPrimary);
        _btnThemeToggle.Size = new Size(150, 34);
        _btnThemeToggle.Click += (s, e) => ToggleTheme();
        _footerPanel.Controls.Add(_btnThemeToggle);

        _btnRefresh = CreateFooterButton("🔄 Atualizar Lista", _theme.SecondaryBtnBg, _theme.TextPrimary);
        _btnRefresh.Size = new Size(140, 34);
        _btnRefresh.Click += (s, e) => LoadExamTemplates();
        _footerPanel.Controls.Add(_btnRefresh);

        _btnShortcut = CreateFooterButton("📌 Atalho Desktop", _theme.SecondaryBtnBg, _theme.BrandOrange);
        _btnShortcut.Size = new Size(130, 34);
        _btnShortcut.Click += (s, e) =>
        {
            string exePath = Environment.ProcessPath ?? Path.Combine(AppConfigAndStorage.BaseDirectory, AppConfigAndStorage.InstalledExeName);
            AppConfigAndStorage.CreateDesktopShortcut(exePath, AppConfigAndStorage.BaseDirectory);
            ThemedMessageDialog.Show(
                this,
                "Atalho 'Avaliações - Rainha' criado/atualizado com sucesso na Área de Trabalho!",
                "Atalho Criado",
                MessageBoxIcon.Information);
        };
        _footerPanel.Controls.Add(_btnShortcut);

        void LayoutFooterButtons()
        {
            int rightMargin = 20;
            int gap = 10;
            int x = _footerPanel.ClientSize.Width - rightMargin;

            x -= _btnShortcut.Width;
            _btnShortcut.Location = new Point(x, 11);

            x -= gap + _btnRefresh.Width;
            _btnRefresh.Location = new Point(x, 11);

            x -= gap + _btnThemeToggle.Width;
            _btnThemeToggle.Location = new Point(x, 11);
        }
        _footerPanel.SizeChanged += (s, e) => LayoutFooterButtons();
        LayoutFooterButtons();

        // 3. Container Principal de Seleção de Avaliações
        _selectionContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 18, 26, 14)
        };

        _lblSectionTitle = new Label
        {
            Text = "📋 Escolha a Avaliação para Iniciar:",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 34
        };

        _examsListPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 6)
        };
        _examsListPanel.SizeChanged += (s, e) => ResizeExamCards();

        _selectionContainer.Controls.Add(_examsListPanel);
        _selectionContainer.Controls.Add(_lblSectionTitle);

        // 4. Painel de "Avaliação em Andamento" (exibido enquanto o Excel está aberto)
        _activeSessionPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Visible = false,
            Padding = new Padding(40, 28, 40, 28)
        };

        _activeCard = new Panel
        {
            Dock = DockStyle.Fill
        };
        _activeCard.Paint += (s, e) =>
        {
            using var pen = new Pen(_theme.BrandOrange, 2);
            e.Graphics.DrawRectangle(pen, 1, 1, _activeCard.Width - 3, _activeCard.Height - 3);
        };

        _lblInProgressBadge = new Label
        {
            Text = "⏳ AVALIAÇÃO EM ANDAMENTO NO EXCEL",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 52
        };

        _lblActiveCandidate = new Label
        {
            Text = "Candidato: -",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 34
        };

        _lblActiveExam = new Label
        {
            Text = "Avaliação: -",
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };

        _lblActiveFile = new Label
        {
            Text = "Arquivo salvo automaticamente em: -",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 28
        };

        _lblTimer = new Label
        {
            Text = "Tempo decorrido: 00:00:00",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 54
        };

        _lblSaveNote = new Label
        {
            Text = "💡 Você pode fechar o Excel normalmente no 'X' ao terminar ou clicar no botão abaixo.\nSua avaliação será salva automaticamente com seu nome completo.",
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 48
        };

        var btnFinishWrapper = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.Transparent
        };

        _btnFinishAndSave = new Button
        {
            Text = "✅ Finalizar e Salvar Avaliação Agora",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Size = new Size(360, 48),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _btnFinishAndSave.FlatAppearance.BorderSize = 0;
        btnFinishWrapper.SizeChanged += (s, e) =>
        {
            _btnFinishAndSave.Location = new Point((btnFinishWrapper.Width - _btnFinishAndSave.Width) / 2, 10);
        };
        _btnFinishAndSave.Click += (s, e) =>
        {
            _currentSession?.SaveAndCloseSession();
        };
        btnFinishWrapper.Controls.Add(_btnFinishAndSave);

        _activeCard.Controls.Add(btnFinishWrapper);
        _activeCard.Controls.Add(_lblSaveNote);
        _activeCard.Controls.Add(_lblTimer);
        _activeCard.Controls.Add(_lblActiveFile);
        _activeCard.Controls.Add(_lblActiveExam);
        _activeCard.Controls.Add(_lblActiveCandidate);
        _activeCard.Controls.Add(_lblInProgressBadge);
        _activeSessionPanel.Controls.Add(_activeCard);

        Controls.Add(_activeSessionPanel);
        Controls.Add(_selectionContainer);
        Controls.Add(_footerPanel);
        Controls.Add(_titleBar);
        Controls.Add(_headerPanel);
        Controls.SetChildIndex(_headerPanel, 0);
        Controls.SetChildIndex(_titleBar, 1);

        _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _uiTimer.Tick += UiTimer_Tick;

        ApplyTheme();

        Load += (s, e) => LoadExamTemplates();
        FormClosing += MainForm_FormClosing;
    }

    private void ToggleTheme()
    {
        bool nextDark = !_theme.IsDark;
        AppConfigAndStorage.SetDarkModeEnabled(nextDark);
        _theme = nextDark ? ThemePalette.Dark : ThemePalette.Light;
        ApplyTheme();
        LoadExamTemplates();
    }

    private void ApplyTheme()
    {
        _titleBar.ApplyTheme(_theme);
        BackColor = _theme.WindowBg;
        _headerPanel.BackColor = _theme.HeaderBg;
        _lblHeaderTitle.ForeColor = _theme.BrandRed;
        _lblHeaderSubtitle.ForeColor = _theme.TextMuted;

        _footerPanel.BackColor = _theme.FooterBg;
        _btnAdminLock.BackColor = _theme.BrandRed;

        _btnThemeToggle.Text = _theme.IsDark ? "☀️ Modo Claro" : "🌙 Modo Noturno";
        _btnThemeToggle.BackColor = _theme.SecondaryBtnBg;
        _btnThemeToggle.ForeColor = _theme.TextPrimary;
        _btnThemeToggle.FlatAppearance.BorderColor = _theme.SecondaryBtnBorder;

        _btnRefresh.BackColor = _theme.SecondaryBtnBg;
        _btnRefresh.ForeColor = _theme.TextPrimary;
        _btnRefresh.FlatAppearance.BorderColor = _theme.SecondaryBtnBorder;

        _btnShortcut.BackColor = _theme.SecondaryBtnBg;
        _btnShortcut.ForeColor = _theme.BrandOrange;
        _btnShortcut.FlatAppearance.BorderColor = _theme.SecondaryBtnBorder;

        _selectionContainer.BackColor = _theme.WindowBg;
        _lblSectionTitle.ForeColor = _theme.TextPrimary;
        _examsListPanel.BackColor = _theme.WindowBg;

        _activeSessionPanel.BackColor = _theme.WindowBg;
        _activeCard.BackColor = _theme.CardBg;
        _lblInProgressBadge.ForeColor = _theme.BrandRed;
        _lblActiveCandidate.ForeColor = _theme.TextPrimary;
        _lblActiveExam.ForeColor = _theme.BrandOrange;
        _lblActiveFile.ForeColor = _theme.TextMuted;
        _lblTimer.ForeColor = _theme.TextPrimary;
        _lblSaveNote.ForeColor = _theme.TextMuted;
        _btnFinishAndSave.BackColor = _theme.BrandRed;

        Invalidate(true);
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
        btn.FlatAppearance.BorderColor = _theme.SecondaryBtnBorder;
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
                Text = "Nenhuma avaliação (.xlsx) encontrada na pasta Templates.\nAcesse o cadeado '🔒' abaixo para adicionar arquivos de avaliação na Pasta de Templates.",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = _theme.TextMuted,
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
            BackColor = _theme.CardBg,
            Cursor = Cursors.Hand
        };

        card.Paint += (s, e) =>
        {
            using var borderPen = new Pen(_theme.CardBorder, 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);

            // Barra lateral esquerda nas cores da Rainha
            using var accentBrush = new LinearGradientBrush(
                new Rectangle(0, 0, 7, card.Height),
                _theme.BrandRed,
                _theme.BrandOrange,
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 7, card.Height);
        };

        var lblIcon = new Label
        {
            Text = "📊",
            Font = new Font("Segoe UI Emoji", 20F, FontStyle.Regular),
            ForeColor = _theme.TextPrimary,
            Location = new Point(20, 18),
            Size = new Size(46, 44),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var lblTitle = new Label
        {
            Text = item.DisplayName,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = _theme.TextPrimary,
            Location = new Point(70, 14),
            Size = new Size(410, 28),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var lblMeta = new Label
        {
            Text = $"Modelo Original Protegido (.xlsx) • Clique para informar seu nome e iniciar",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = _theme.TextMuted,
            Location = new Point(70, 44),
            Size = new Size(430, 20),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var btnSelect = new Button
        {
            Text = "Fazer Avaliação  ➔",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(164, 40),
            Location = new Point(card.Width - 184, 19),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.BrandRed,
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

        void OnMouseEnterCard(object? s, EventArgs e) => card.BackColor = _theme.CardHoverBg;
        void OnMouseLeaveCard(object? s, EventArgs e) => card.BackColor = _theme.CardBg;

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
            ThemedMessageDialog.Show(
                this,
                "Já existe uma avaliação em andamento no Excel. Finalize a avaliação atual antes de iniciar outra.",
                "Avaliação em Andamento",
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

            // Atualiza tela para modo "Avaliação em Andamento"
            _lblActiveCandidate.Text = $"Candidato(a): {candidateName}";
            _lblActiveExam.Text = $"Avaliação: {template.DisplayName}";
            _lblActiveFile.Text = $"Arquivo salvo: {Path.GetFileName(targetFilePath)}";
            _lblTimer.Text = "Tempo decorrido: 00:00:00";

            _selectionContainer.Visible = false;
            _activeSessionPanel.Visible = true;
            _uiTimer.Start();
        }
        catch (Exception ex)
        {
            _currentSession?.Dispose();
            _currentSession = null;

            ThemedMessageDialog.Show(
                this,
                $"Não foi possível abrir a avaliação no Microsoft Excel:\n\n{ex.Message}",
                "Erro ao Iniciar Avaliação",
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

        ThemedMessageDialog.Show(
            this,
            $"Avaliação finalizada e salva com sucesso!\n\n" +
            $"👤 Candidato(a): {finishedSession.CandidateName}\n" +
            $"📊 Avaliação: {finishedSession.ExamName}\n" +
            $"💾 Arquivo salvo: {Path.GetFileName(finishedSession.TargetFilePath)}\n" +
            $"📁 Pasta: {AppConfigAndStorage.CompletedExamsDirectory}",
            "Avaliação Salva com Sucesso - Rainha",
            MessageBoxIcon.Information);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_currentSession != null)
        {
            var resp = ThemedMessageDialog.Show(
                this,
                "Existe uma avaliação aberta no Excel no momento.\nDeseja salvar automaticamente e fechar a avaliação antes de sair?",
                "Avaliação em Andamento",
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
