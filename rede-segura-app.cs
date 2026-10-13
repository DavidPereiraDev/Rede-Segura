using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

internal sealed class RedeSeguraApp : Form
{
    private static readonly Color BackgroundColor = Color.FromArgb(244, 247, 242);
    private static readonly Color BrandColor = Color.FromArgb(23, 107, 80);
    private static readonly Color BrandDarkColor = Color.FromArgb(16, 83, 62);
    private static readonly Color CardBorderColor = Color.FromArgb(224, 233, 226);
    private static readonly Color TextColor = Color.FromArgb(35, 48, 42);
    private static readonly Color MutedColor = Color.FromArgb(98, 111, 103);
    private static readonly Color WarningColor = Color.FromArgb(159, 99, 15);
    private static readonly Color WarningBackground = Color.FromArgb(255, 247, 229);
    private static readonly Color DangerColor = Color.FromArgb(165, 52, 36);
    private static readonly Color DangerBackground = Color.FromArgb(255, 240, 237);
    private static readonly Color DarkBackgroundColor = Color.FromArgb(23, 31, 36);
    private static readonly Color DarkCardColor = Color.FromArgb(32, 42, 48);
    private static readonly Color DarkInputColor = Color.FromArgb(25, 35, 40);
    private static readonly Color DarkTextColor = Color.FromArgb(239, 244, 241);
    private static readonly Color DarkMutedColor = Color.FromArgb(174, 188, 181);
    private static readonly Color DarkBorderColor = Color.FromArgb(57, 72, 78);
    private static readonly Color DarkBrandColor = Color.FromArgb(112, 204, 157);
    private readonly TextBox messageInput;
    private readonly Label characterCount;
    private readonly Button clearButton;
    private readonly Button pasteButton;
    private readonly Button copyResultButton;
    private readonly Button checkButton;
    private readonly Panel inputCard;
    private readonly Panel statusBadge;
    private readonly Label statusLabel;
    private readonly Label resultHeading;
    private readonly Label resultSummary;
    private readonly FlowLayoutPanel resultDetails;
    private readonly FlowLayoutPanel exampleButtons;
    private readonly AnalysisResultSection[] analysisSections;
    private readonly Panel resultCard;
    private readonly TableLayoutPanel inputCardContent;
    private readonly TableLayoutPanel resultCardContent;
    private readonly TableLayoutPanel content;
    private readonly Panel body;
    private readonly TableLayoutPanel workspace;
    private readonly TableLayoutPanel inputColumn;
    private readonly Panel resultScroll;
    private readonly TableLayoutPanel shell;
    private readonly SidebarControl sidebar;
    private readonly Label sectionIconLabel;
    private readonly Label sectionTitleLabel;
    private readonly Label sectionDescriptionLabel;
    private readonly Panel placeholderView;
    private readonly Label placeholderTitle;
    private readonly Label placeholderDescription;
    private readonly LearningControl learningControl;
    private readonly SafetyGuideControl safetyGuideControl;
    private readonly LinkCheckerControl linkCheckerControl;
    private readonly SettingsControl settingsControl;
    private readonly AboutControl aboutControl;
    private readonly Button themeButton;
    private readonly Panel resultAccent;
    private readonly Timer fadeTimer;
    private readonly Timer resultAnimationTimer;
    private readonly BackgroundWorker analysisWorker = new BackgroundWorker();
    private bool darkMode;
    private bool sidebarCollapsed;
    private AppSection activeSection = AppSection.Analyzer;
    private ScamAnalysisResult displayedAnalysis;
    private int animationStep;

    private RedeSeguraApp()
    {
        Text = "Rede Segura";
        StartPosition = FormStartPosition.CenterScreen;
        Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
        MinimumSize = new Size(
            Math.Min(1320, Math.Max(320, workingArea.Width - 48)),
            Math.Min(700, Math.Max(300, workingArea.Height - 48)));
        ClientSize = new Size(
            Math.Min(1400, Math.Max(320, workingArea.Width - 48)),
            Math.Min(860, Math.Max(300, workingArea.Height - 48)));
        BackColor = BackgroundColor;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
        Opacity = 0;
        darkMode = true;

        Bitmap officialLogo = CreateOfficialLogoImage();
        IntPtr iconHandle = officialLogo.GetHicon();
        try
        {
            using (Icon icon = Icon.FromHandle(iconHandle))
            {
                Icon = (Icon)icon.Clone();
            }
        }
        finally
        {
            DestroyIcon(iconHandle);
        }

        content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = BackgroundColor,
            Margin = new Padding(0),
            Tag = "background"
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkBackgroundColor,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            Tag = "background"
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238F));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sidebar = new SidebarControl(officialLogo);
        sidebar.SectionSelected += OnSectionSelected;
        shell.Controls.Add(sidebar, 0, 0);
        shell.Controls.Add(content, 1, 0);
        Controls.Add(shell);
        SizeChanged += delegate { UpdateShellLayout(); };

        body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BackgroundColor,
            Padding = new Padding(24, 20, 24, 16),
            Tag = "background"
        };
        content.Controls.Add(body, 0, 1);

        workspace = new TableLayoutPanel
        {
            BackColor = BackgroundColor,
            Margin = new Padding(0),
            Padding = new Padding(0),
            RowCount = 1,
            ColumnCount = 2,
            Tag = "background"
        };
        body.Controls.Add(workspace);
        body.SizeChanged += delegate
        {
            if (activeSection == AppSection.Analyzer)
            {
                ResizeContent();
            }
        };

        inputColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackgroundColor,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(0),
            Tag = "background"
        };
        inputColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        inputColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        inputColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
        workspace.Controls.Add(inputColumn, 0, 0);

        Panel header = new Panel
        {
            BackColor = BrandDarkColor,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(22, 18, 16, 18),
            Tag = "header"
        };
        sectionIconLabel = new Label
        {
            Dock = DockStyle.Left,
            Width = 52,
            Text = "\u270e",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(139, 221, 190),
            Font = new Font("Segoe UI Symbol", 24F),
            Tag = "header-icon"
        };
        Panel sectionInfo = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BrandDarkColor,
            Padding = new Padding(12, 2, 0, 0),
            Tag = "header"
        };
        TableLayoutPanel headerText = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        headerText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerText.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        headerText.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        sectionTitleLabel = new Label
        {
            Text = "Analisador de mensagens",
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            Margin = new Padding(0),
            Tag = "header-text"
        };
        sectionDescriptionLabel = new Label
        {
            Text = "Analise SMS, emails ou mensagens e identifique possíveis burlas.",
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(216, 235, 224),
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(1, 0, 0, 0),
            Tag = "header-muted"
        };
        headerText.Controls.Add(sectionTitleLabel, 0, 0);
        headerText.Controls.Add(sectionDescriptionLabel, 0, 1);
        sectionInfo.Controls.Add(headerText);
        themeButton = MakeButton("☀  Modo claro", Color.FromArgb(36, 91, 70), Color.White, true);
        themeButton.Dock = DockStyle.Right;
        themeButton.Width = 142;
        themeButton.Tag = "header-button";
        themeButton.FlatAppearance.BorderSize = 0;
        themeButton.AccessibleName = "Ativar modo claro";
        themeButton.Click += delegate { ToggleTheme(); };
        header.Controls.Add(themeButton);
        header.Controls.Add(sectionInfo);
        header.Controls.Add(sectionIconLabel);
        content.Controls.Add(header, 0, 0);

        placeholderView = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkBackgroundColor,
            Padding = new Padding(32),
            Visible = false,
            Tag = "background"
        };
        TableLayoutPanel placeholderContent = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Location = new Point(32, 32),
            BackColor = Color.Transparent
        };
        placeholderContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        placeholderContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        placeholderContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        placeholderTitle = MakeLabel("Secção selecionada", 22F, TextColor, true);
        placeholderTitle.Margin = new Padding(0, 0, 0, 10);
        placeholderDescription = MakeLabel(
            "Esta área será apresentada nesta janela. Pode regressar ao analisador através da navegação.",
            11F, MutedColor, false);
        placeholderDescription.MaximumSize = new Size(680, 0);
        placeholderContent.Controls.Add(placeholderTitle, 0, 0);
        placeholderContent.Controls.Add(placeholderDescription, 0, 1);
        placeholderView.Controls.Add(placeholderContent);
        body.Controls.Add(placeholderView);
        learningControl = new LearningControl { Visible = false };
        safetyGuideControl = new SafetyGuideControl { Visible = false };
        linkCheckerControl = new LinkCheckerControl { Visible = false };
        settingsControl = new SettingsControl { Visible = false };
        aboutControl = new AboutControl { Visible = false };
        settingsControl.ThemeSelected += delegate(object sender, ThemeSelectionEventArgs e)
        {
            SetTheme(e.DarkMode);
        };
        body.Controls.Add(learningControl);
        body.Controls.Add(safetyGuideControl);
        body.Controls.Add(linkCheckerControl);
        body.Controls.Add(settingsControl);
        body.Controls.Add(aboutControl);

        inputCard = MakeCard(300);
        inputCard.Dock = DockStyle.Fill;
        inputCardContent = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(20),
            Margin = new Padding(0),
            Tag = "card-content"
        };
        inputCardContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        inputCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputCardContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        inputCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputCard.Controls.Add(inputCardContent);
        Label messageTitle = MakeLabel("1. Mensagem para analisar", 15F, TextColor, true);
        messageTitle.Margin = new Padding(0, 0, 0, 7);
        inputCardContent.Controls.Add(messageTitle, 0, 0);
        Label messageHint = MakeLabel(
            "Cole uma mensagem suspeita. Não inclua palavras-passe, códigos ou dados bancários.",
            9.5F, MutedColor, false);
        messageHint.Margin = new Padding(0, 0, 0, 10);
        messageHint.MaximumSize = new Size(440, 0);
        inputCardContent.Controls.Add(messageHint, 0, 1);

        messageInput = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            AcceptsTab = false,
            MaxLength = ScamAnalyzer.MaximumMessageLength + 1,
            Font = new Font("Segoe UI", 11F),
            BackColor = Color.FromArgb(250, 252, 250),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 90),
            Margin = new Padding(0, 0, 0, 8),
            Tag = "input",
            AccessibleName = "Mensagem para analisar",
            AccessibleDescription = "Cole ou escreva uma mensagem suspeita"
        };
        messageInput.TextChanged += delegate
        {
            int length = messageInput.Text.Length;
            characterCount.Text = length > ScamAnalyzer.MaximumMessageLength
                ? "O limite de 20 000 caracteres foi ultrapassado; reduza o texto antes de analisar."
                : length + (length == 1 ? " carácter" : " caracteres");
            characterCount.Tag = "muted";
            characterCount.ForeColor = darkMode ? DarkMutedColor : MutedColor;
            if (displayedAnalysis != null)
            {
                ResetResult();
            }
        };
        inputCardContent.Controls.Add(messageInput, 0, 2);

        characterCount = MakeLabel("0 caracteres", 9F, MutedColor, false);
        characterCount.MaximumSize = new Size(440, 0);
        characterCount.AutoEllipsis = true;
        characterCount.Margin = new Padding(0, 0, 0, 10);
        inputCardContent.Controls.Add(characterCount, 0, 3);

        clearButton = MakeButton("Limpar mensagem", Color.White, BrandColor, false);
        clearButton.FlatAppearance.BorderColor = CardBorderColor;
        clearButton.FlatAppearance.BorderSize = 1;
        clearButton.Size = new Size(150, 36);
        clearButton.Margin = new Padding(0, 0, 0, 10);
        clearButton.AccessibleName = "Limpar mensagem";
        clearButton.Click += delegate
        {
            ClearMessage();
        };
        inputCardContent.Controls.Add(clearButton, 0, 4);

        checkButton = MakeButton("Analisar mensagem    Ctrl + Enter", BrandColor, Color.White, true);
        checkButton.Tag = "button-primary";
        checkButton.FlatAppearance.BorderSize = 0;
        checkButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(19, 91, 68);
        checkButton.FlatAppearance.MouseDownBackColor = BrandDarkColor;
        checkButton.Dock = DockStyle.Fill;
        checkButton.Height = 46;
        checkButton.Margin = new Padding(0);
        checkButton.AccessibleName = "Enviar mensagem para análise";
        checkButton.Click += delegate { AnalyzeMessage(); };
        inputCardContent.Controls.Add(checkButton, 0, 5);
        AcceptButton = checkButton;
        inputColumn.Controls.Add(inputCard, 0, 0);

        FlowLayoutPanel messageActions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(0),
            Tag = "card-content"
        };
        pasteButton = MakeButton("Colar mensagem", Color.White, BrandColor, false);
        pasteButton.Size = new Size(132, 34);
        pasteButton.Margin = new Padding(0, 0, 6, 4);
        pasteButton.FlatAppearance.BorderColor = CardBorderColor;
        pasteButton.FlatAppearance.BorderSize = 1;
        pasteButton.AccessibleName = "Colar mensagem da área de transferência";
        pasteButton.Click += delegate { PasteMessage(); };
        messageActions.Controls.Add(pasteButton);

        clearButton.Size = new Size(150, 34);
        clearButton.Margin = new Padding(0, 0, 0, 4);
        messageActions.Controls.Add(clearButton);
        inputCardContent.Controls.Add(messageActions, 0, 4);
        inputCardContent.RowStyles[4] = new RowStyle(SizeType.AutoSize);
        inputCardContent.RowStyles[5] = new RowStyle(SizeType.AutoSize);

        Panel exampleCard = MakeCard(190);
        exampleCard.Dock = DockStyle.Fill;
        exampleCard.Margin = new Padding(0, 10, 0, 0);
        TableLayoutPanel exampleCardContent = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16, 12, 16, 10),
            Margin = new Padding(0),
            Tag = "card-content"
        };
        exampleCardContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        exampleCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exampleCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exampleCardContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        exampleCard.Controls.Add(exampleCardContent);
        Label examplesTitle = MakeLabel("Exemplos de mensagens", 12F, TextColor, true);
        examplesTitle.Margin = new Padding(0, 0, 0, 3);
        exampleCardContent.Controls.Add(examplesTitle, 0, 0);
        Label examplesHint = MakeLabel(
            "Selecione um exemplo para o colocar no campo acima.",
            8.5F, MutedColor, false);
        examplesHint.Margin = new Padding(0, 0, 0, 5);
        exampleCardContent.Controls.Add(examplesHint, 0, 1);
        exampleButtons = new FlowLayoutPanel
        {
            AutoScroll = true,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
            WrapContents = true,
            Tag = "card-content"
        };
        string[] exampleLabels =
        {
            "Burla bancária\n(SMS)",
            "Encomenda\nsuspeita",
            "Pedido de\ncódigo",
            "Mensagem legítima",
            "Mensagem inconclusiva"
        };
        for (int index = 1; index <= 5; index++)
        {
            int selectedExample = index;
            Button exampleButton = MakeButton(
                exampleLabels[index - 1], Color.White, BrandColor, false);
            exampleButton.Size = new Size(124, 48);
            exampleButton.Margin = new Padding(0, 0, 7, 5);
            exampleButton.FlatAppearance.BorderColor = CardBorderColor;
            exampleButton.FlatAppearance.BorderSize = 1;
            exampleButton.AccessibleName = "Experimentar exemplo: " +
                exampleLabels[index - 1].Replace("\n", " ");
            exampleButton.Click += delegate
            {
                string example = GetExampleMessage(selectedExample);
                if (example != null)
                {
                    messageInput.Text = example;
                    messageInput.Focus();
                }
            };
            exampleButtons.Controls.Add(exampleButton);
        }
        exampleCardContent.Controls.Add(exampleButtons, 0, 2);
        inputColumn.Controls.Add(exampleCard, 0, 1);

        resultCard = MakeCard(300);
        resultCard.Dock = DockStyle.Fill;
        resultCard.Margin = new Padding(10, 0, 0, 0);
        resultCardContent = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20),
            Margin = new Padding(0),
            Tag = "card-content"
        };
        resultCardContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        resultCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultCardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultCardContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        resultCard.Controls.Add(resultCardContent);
        FlowLayoutPanel resultToolbar = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0),
            Tag = "card-content"
        };
        Label resultTitle = MakeLabel("2. Resultado da análise", 13F, TextColor, true);
        resultTitle.Margin = new Padding(0, 4, 8, 4);
        resultToolbar.Controls.Add(resultTitle);
        copyResultButton = MakeButton("Copiar resultado", Color.White, BrandColor, false);
        copyResultButton.Size = new Size(142, 32);
        copyResultButton.Margin = new Padding(0, 0, 0, 4);
        copyResultButton.FlatAppearance.BorderColor = CardBorderColor;
        copyResultButton.FlatAppearance.BorderSize = 1;
        copyResultButton.AccessibleName = "Copiar resumo do resultado da análise";
        copyResultButton.Enabled = false;
        copyResultButton.Click += delegate { CopyAnalysisResult(); };
        resultToolbar.Controls.Add(copyResultButton);
        resultCardContent.Controls.Add(resultToolbar, 0, 0);

        resultAccent = new Panel
        {
            BackColor = BrandColor,
            Location = Point.Empty,
            Size = new Size(5, resultCard.Height),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
        };
        resultCard.Controls.Add(resultAccent);

        statusBadge = new Panel
        {
            BackColor = Color.FromArgb(232, 243, 237),
            Size = new Size(202, 28),
            Margin = new Padding(0, 0, 0, 10),
            Tag = "status-ok"
        };
        statusLabel = MakeLabel("PRONTA PARA ANALISAR", 8F, BrandColor, true, 9, 6);
        statusBadge.Controls.Add(statusLabel);
        resultCardContent.Controls.Add(statusBadge, 0, 1);

        resultHeading = MakeLabel("Pronto para analisar", 17F, TextColor, true);
        resultHeading.Margin = new Padding(0, 0, 0, 6);
        resultCardContent.Controls.Add(resultHeading, 0, 2);

        resultSummary = MakeLabel(
            "Cole uma mensagem no campo à esquerda e escolha Analisar mensagem para ver os resultados.",
            10F, MutedColor, false);
        resultSummary.Margin = new Padding(0, 0, 0, 8);
        resultCardContent.Controls.Add(resultSummary, 0, 3);

        resultDetails = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            Tag = "card-content"
        };
        analysisSections = new AnalysisResultSection[]
        {
            new AnalysisResultSection("Resumo da análise", "A análise ainda não foi executada."),
            new AnalysisResultSection("Sinais identificados", "Os sinais encontrados serão apresentados após a análise."),
            new AnalysisResultSection("Explicação e contexto", "As explicações do motor serão apresentadas após a análise."),
            new AnalysisResultSection("Recomendações de segurança", "As recomendações serão apresentadas após a análise."),
            new AnalysisResultSection("Limitações da análise", "As limitações serão apresentadas após a análise.")
        };
        foreach (AnalysisResultSection section in analysisSections)
        {
            resultDetails.Controls.Add(section);
        }
        resultScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White,
            Margin = new Padding(0),
            Tag = "card"
        };
        resultScroll.Controls.Add(resultDetails);
        resultCardContent.Controls.Add(resultScroll, 0, 4);
        workspace.Controls.Add(inputColumn, 0, 0);
        workspace.Controls.Add(resultCard, 1, 0);

        Label privacy = MakeLabel(
            "PRIVACIDADE  ·  A análise é feita neste dispositivo. O texto não é enviado nem guardado.",
            9F, MutedColor, false);
        privacy.Margin = new Padding(0, 4, 0, 4);

        Label disclaimer = MakeLabel(
            "A análise procura padrões comuns e pode enganar-se. Confirme mensagens importantes sempre por um contacto oficial que já conhece.",
            8.5F, MutedColor, false);
        disclaimer.Margin = new Padding(0, 0, 0, 4);

        FlowLayoutPanel footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackgroundColor,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(28, 8, 28, 8),
            Margin = new Padding(0),
            Tag = "background"
        };
        footer.Controls.Add(privacy);
        footer.Controls.Add(disclaimer);
        content.Controls.Add(footer, 0, 2);

        fadeTimer = new Timer { Interval = 15 };
        fadeTimer.Tick += delegate
        {
            Opacity = Math.Min(1, Opacity + 0.12);
            if (Opacity >= 1)
            {
                fadeTimer.Stop();
            }
        };
        resultAnimationTimer = new Timer { Interval = 18 };
        resultAnimationTimer.Tick += delegate
        {
            animationStep++;
            resultAccent.Width = Math.Min(9, 1 + animationStep);
            if (animationStep >= 8)
            {
                resultAccent.Width = 5;
                resultAnimationTimer.Stop();
            }
        };
        Shown += delegate { fadeTimer.Start(); };
        KeyDown += delegate(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (activeSection == AppSection.Analyzer)
                {
                    AnalyzeMessage();
                }
                else if (activeSection == AppSection.LinkChecker)
                {
                    linkCheckerControl.AnalyzeAddress();
                }
            }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.L)
            {
                e.SuppressKeyPress = true;
                if (activeSection == AppSection.Analyzer)
                {
                    ClearMessage();
                }
                else if (activeSection == AppSection.LinkChecker)
                {
                    linkCheckerControl.ClearAddress();
                }
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.C &&
                activeSection == AppSection.Analyzer)
            {
                e.SuppressKeyPress = true;
                CopyAnalysisResult();
            }
        };
        FormClosed += delegate
        {
            fadeTimer.Dispose();
            resultAnimationTimer.Dispose();
            officialLogo.Dispose();
        };
        analysisWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
        {
            e.Result = ScamAnalyzer.Analyze((string)e.Argument);
        };
        analysisWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
        {
            checkButton.Enabled = true;
            messageInput.Enabled = true;
            clearButton.Enabled = true;
            pasteButton.Enabled = true;
            checkButton.Text = "Analisar mensagem    Ctrl + Enter";
            if (e.Error != null)
            {
                characterCount.Text = "Não foi possível analisar a mensagem. Reveja o texto e tente novamente.";
                characterCount.Tag = "risk-danger";
                characterCount.ForeColor = darkMode ? Color.FromArgb(255, 150, 137) : DangerColor;
                MessageBox.Show(
                    "A análise não foi concluída.\r\n\r\n" + e.Error.Message,
                    "Erro na análise",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            DisplayAnalysis((ScamAnalysisResult)e.Result);
        };

        ApplyTheme();
        sidebar.SetActiveSection(AppSection.Analyzer);
        UpdateShellLayout();
    }

    private void ResizeContent()
    {
        if (body == null || workspace == null || inputColumn == null ||
            resultCard == null || messageInput == null || inputCardContent == null ||
            resultCardContent == null || resultScroll == null)
        {
            return;
        }

        float dpiScale = 1F;
        using (Graphics graphics = CreateGraphics())
        {
            dpiScale = graphics.DpiX / 96F;
        }

        int availableWidth = Math.Max(0, body.ClientSize.Width - body.Padding.Horizontal);
        int availableHeight = Math.Max(0, body.ClientSize.Height - body.Padding.Vertical);
        int minimumWorkspaceWidth = (int)Math.Round(1060F * dpiScale);
        int minimumWorkspaceHeight = (int)Math.Round(520F * dpiScale);
        int workspaceWidth = Math.Max(availableWidth, minimumWorkspaceWidth);
        int workspaceHeight = Math.Max(availableHeight, minimumWorkspaceHeight);

        workspace.SuspendLayout();
        workspace.Margin = new Padding(0);
        workspace.Width = workspaceWidth;
        workspace.Height = workspaceHeight;
        workspace.Location = Point.Empty;
        workspace.Dock = DockStyle.None;
        workspace.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        workspace.ColumnCount = 2;
        workspace.RowCount = 1;
        body.AutoScroll = workspaceWidth > availableWidth || workspaceHeight > availableHeight;
        workspace.ColumnStyles.Clear();
        workspace.RowStyles.Clear();
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        inputColumn.Dock = DockStyle.Fill;
        inputColumn.Margin = new Padding(0, 0, 10, 0);
        resultCard.Dock = DockStyle.Fill;
        resultCard.Margin = new Padding(10, 0, 0, 0);

        workspace.ResumeLayout(true);

        int inputWidth = Math.Max(180, inputCardContent.ClientSize.Width - inputCardContent.Padding.Horizontal);
        messageInput.MinimumSize = new Size(0, (int)Math.Round(90F * dpiScale));
        characterCount.MaximumSize = new Size(inputWidth, 0);
        int resultWidth = Math.Max(180, resultCardContent.ClientSize.Width - resultCardContent.Padding.Horizontal);
        resultAccent.Height = resultCard.ClientSize.Height;
        statusBadge.Width = Math.Min(330, resultWidth);
        resultHeading.MaximumSize = new Size(resultWidth, 0);
        resultSummary.MaximumSize = new Size(resultWidth, 0);
        int detailWidth = Math.Max(100, resultScroll.ClientSize.Width -
            SystemInformation.VerticalScrollBarWidth - 8);
        resultDetails.Width = detailWidth;
        foreach (Control item in resultDetails.Controls)
        {
            AnalysisResultSection section = item as AnalysisResultSection;
            if (section != null)
            {
                section.Width = detailWidth;
            }
        }

        exampleButtons.Width = inputWidth;

        Control footer = content.GetControlFromPosition(0, 2);
        FlowLayoutPanel footerFlow = footer as FlowLayoutPanel;
        if (footerFlow != null)
        {
            foreach (Control item in footerFlow.Controls)
            {
                Label label = item as Label;
                if (label != null)
                {
                    label.MaximumSize = new Size(Math.Max(240, content.ClientSize.Width - footerFlow.Padding.Horizontal), 0);
                }
            }
        }

        content.PerformLayout();
    }

    private void UpdateShellLayout()
    {
        if (shell == null || sidebar == null)
        {
            return;
        }

        bool collapseSidebar = ClientSize.Width < 1344;
        if (sidebarCollapsed != collapseSidebar)
        {
            sidebarCollapsed = collapseSidebar;
            sidebar.SetCollapsed(sidebarCollapsed);
            shell.ColumnStyles[0].Width = sidebarCollapsed ? 68F : 238F;
        }

        if (activeSection == AppSection.Analyzer)
        {
            ResizeContent();
        }
    }

    private void OnSectionSelected(object sender, AppSectionEventArgs e)
    {
        string title;
        string description;
        string icon;
        switch (e.Section)
        {
            case AppSection.Analyzer:
                title = "Analisador de mensagens";
                description = "Analise SMS, emails ou mensagens e identifique possíveis burlas.";
                icon = "\u270e";
                break;
            case AppSection.LinkChecker:
                title = "Verificar links";
                description = "Avalie localmente sinais suspeitos na estrutura de um endereço.";
                icon = "\u2197";
                break;
            case AppSection.Learning:
                title = "Aprender";
                description = "Conheça sinais comuns de burla e formas de se proteger.";
                icon = "\u25c7";
                break;
            case AppSection.SafetyGuide:
                title = "Guia de segurança";
                description = "Consulte passos prudentes para situações do dia a dia.";
                icon = "\u25a4";
                break;
            case AppSection.Settings:
                title = "Definições";
                description = "Personalize a apresentação e reveja a privacidade.";
                icon = "\u2699";
                break;
            default:
                title = "Sobre a Rede Segura";
                description = "Informações sobre a aplicação e a análise local.";
                icon = "\u24d8";
                break;
        }

        sectionTitleLabel.Text = title;
        sectionDescriptionLabel.Text = description;
        sectionIconLabel.Text = icon;
        placeholderTitle.Text = title;
        placeholderDescription.Text = e.Section == AppSection.Analyzer
            ? "Analise uma mensagem nesta área."
            : "Esta secção será implementada na etapa correspondente. A navegação mantém-se nesta janela.";

        bool showAnalyzer = e.Section == AppSection.Analyzer;
        bool showLearning = e.Section == AppSection.Learning;
        bool showSafetyGuide = e.Section == AppSection.SafetyGuide;
        bool showLinkChecker = e.Section == AppSection.LinkChecker;
        bool showSettings = e.Section == AppSection.Settings;
        bool showAbout = e.Section == AppSection.About;
        workspace.Visible = showAnalyzer;
        placeholderView.Visible = !showAnalyzer && !showLearning &&
            !showSafetyGuide && !showLinkChecker && !showSettings && !showAbout;
        learningControl.Visible = showLearning;
        safetyGuideControl.Visible = showSafetyGuide;
        linkCheckerControl.Visible = showLinkChecker;
        settingsControl.Visible = showSettings;
        aboutControl.Visible = showAbout;
        sidebar.SetActiveSection(e.Section);
        activeSection = e.Section;
        AcceptButton = showAnalyzer
            ? (IButtonControl)checkButton
            : showLinkChecker
                ? linkCheckerControl.AnalyzeButton
                : null;
        body.AutoScroll = false;
        if (showAnalyzer)
        {
            ResizeContent();
        }
        else
        {
            if (showLearning)
            {
                learningControl.BringToFront();
            }
            else if (showSafetyGuide)
            {
                safetyGuideControl.BringToFront();
            }
            else if (showLinkChecker)
            {
                linkCheckerControl.BringToFront();
            }
            else if (showSettings)
            {
                settingsControl.BringToFront();
            }
            else if (showAbout)
            {
                aboutControl.BringToFront();
            }
            else
            {
                placeholderView.BringToFront();
            }
            body.PerformLayout();
        }
    }

    private void ToggleTheme()
    {
        SetTheme(!darkMode);
    }

    private void SetTheme(bool useDarkMode)
    {
        darkMode = useDarkMode;
        themeButton.Text = darkMode ? "☀  Modo claro" : "☾  Modo escuro";
        themeButton.AccessibleName = darkMode ? "Ativar modo claro" : "Ativar modo escuro";
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        BackColor = darkMode ? DarkBackgroundColor : BackgroundColor;
        ApplyThemeToControl(this);
        content.Invalidate(true);
    }

    private void ApplyThemeToControl(Control control)
    {
        SidebarControl sidebarControl = control as SidebarControl;
        if (sidebarControl != null)
        {
            sidebarControl.ApplyTheme(darkMode);
            return;
        }
        SecurityContentPage securityPage = control as SecurityContentPage;
        if (securityPage != null)
        {
            securityPage.ApplyTheme(darkMode);
            return;
        }
        LinkCheckerControl linkPage = control as LinkCheckerControl;
        if (linkPage != null)
        {
            linkPage.ApplyTheme(darkMode);
            return;
        }
        SettingsControl settingsPage = control as SettingsControl;
        if (settingsPage != null)
        {
            settingsPage.ApplyTheme(darkMode);
            return;
        }

        string role = control.Tag as string;
        Color accent = darkMode ? DarkBrandColor : BrandColor;

        switch (role)
        {
            case "background":
                control.BackColor = darkMode ? DarkBackgroundColor : BackgroundColor;
                break;
            case "header":
                control.BackColor = BrandDarkColor;
                break;
            case "header-text":
                control.ForeColor = Color.White;
                break;
            case "header-muted":
                control.ForeColor = Color.FromArgb(216, 235, 224);
                break;
            case "header-button":
                control.BackColor = darkMode ? Color.FromArgb(41, 91, 73) : Color.FromArgb(36, 91, 70);
                control.ForeColor = Color.White;
                ((Button)control).FlatAppearance.MouseOverBackColor = darkMode
                    ? Color.FromArgb(51, 111, 88)
                    : Color.FromArgb(45, 108, 82);
                break;
            case "card":
            case "card-content":
                control.BackColor = darkMode ? DarkCardColor : Color.White;
                break;
            case "result-section":
                control.BackColor = darkMode ? DarkCardColor : Color.White;
                ((AnalysisResultSection)control).ApplyTheme(darkMode);
                break;
            case "result-section-header":
                control.BackColor = darkMode ? DarkCardColor : Color.White;
                control.ForeColor = darkMode ? DarkTextColor : TextColor;
                ((Button)control).FlatAppearance.MouseOverBackColor = darkMode
                    ? Color.FromArgb(43, 56, 62)
                    : Color.FromArgb(242, 247, 243);
                break;
            case "result-section-content":
                control.BackColor = darkMode ? DarkCardColor : Color.White;
                break;
            case "input":
                control.BackColor = darkMode ? DarkInputColor : Color.FromArgb(250, 252, 250);
                control.ForeColor = darkMode ? DarkTextColor : TextColor;
                break;
            case "muted":
                control.ForeColor = darkMode ? DarkMutedColor : MutedColor;
                break;
            case "brand":
                control.ForeColor = accent;
                break;
            case "risk-warning":
                control.ForeColor = darkMode ? Color.FromArgb(255, 196, 105) : WarningColor;
                break;
            case "risk-danger":
                control.ForeColor = darkMode ? Color.FromArgb(255, 150, 137) : DangerColor;
                break;
            case "text":
                control.ForeColor = darkMode ? DarkTextColor : TextColor;
                break;
            case "button-primary":
                control.BackColor = BrandColor;
                control.ForeColor = Color.White;
                ((Button)control).FlatAppearance.MouseOverBackColor = Color.FromArgb(19, 91, 68);
                break;
            case "button-secondary":
                control.BackColor = darkMode ? DarkCardColor : Color.White;
                control.ForeColor = accent;
                ((Button)control).FlatAppearance.BorderColor = darkMode ? DarkBorderColor : CardBorderColor;
                ((Button)control).FlatAppearance.MouseOverBackColor = darkMode
                    ? Color.FromArgb(43, 56, 62)
                    : Color.FromArgb(242, 247, 243);
                break;
            case "status-ok":
                control.BackColor = darkMode
                    ? Color.FromArgb(37, 68, 55)
                    : Color.FromArgb(232, 243, 237);
                statusLabel.ForeColor = accent;
                break;
            case "status-text":
                break;
            case "status-warning":
                control.BackColor = darkMode
                    ? Color.FromArgb(83, 64, 37)
                    : WarningBackground;
                statusLabel.ForeColor = darkMode ? Color.FromArgb(255, 196, 105) : WarningColor;
                break;
            case "status-danger":
                control.BackColor = darkMode
                    ? Color.FromArgb(82, 48, 47)
                    : DangerBackground;
                statusLabel.ForeColor = darkMode ? Color.FromArgb(255, 150, 137) : DangerColor;
                break;
            case "status-neutral":
                control.BackColor = darkMode
                    ? Color.FromArgb(49, 58, 62)
                    : Color.FromArgb(238, 241, 240);
                statusLabel.ForeColor = darkMode ? DarkMutedColor : MutedColor;
                break;
        }

        Panel card = control as Panel;
        if (card != null && role == "card")
        {
            card.Invalidate();
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
    }

    private void AnalyzeMessage()
    {
        string message = messageInput.Text;
        if (String.IsNullOrWhiteSpace(message))
        {
            characterCount.Text = "Escreva ou cole uma mensagem para analisar.";
            characterCount.Tag = "risk-danger";
            characterCount.ForeColor = darkMode ? Color.FromArgb(255, 150, 137) : DangerColor;
            messageInput.Focus();
            return;
        }
        if (message.Length > ScamAnalyzer.MaximumMessageLength)
        {
            characterCount.Text = "O texto atingiu o limite de segurança. Reduza-o e tente novamente.";
            characterCount.Tag = "risk-danger";
            characterCount.ForeColor = darkMode ? Color.FromArgb(255, 150, 137) : DangerColor;
            messageInput.Focus();
            return;
        }

        if (analysisWorker.IsBusy)
        {
            return;
        }

        checkButton.Enabled = false;
        messageInput.Enabled = false;
        clearButton.Enabled = false;
        pasteButton.Enabled = false;
        checkButton.Text = "A analisar…";
        characterCount.Text = "A análise é feita localmente. Aguarde um momento.";
        characterCount.Tag = "muted";
        characterCount.ForeColor = darkMode ? DarkMutedColor : MutedColor;
        analysisWorker.RunWorkerAsync(message);
    }

    private void ClearMessage()
    {
        if (analysisWorker.IsBusy)
        {
            return;
        }

        messageInput.Clear();
        ResetResult();
        messageInput.Focus();
    }

    private void PasteMessage()
    {
        try
        {
            if (!Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                characterCount.Text = "A área de transferência não contém texto.";
                characterCount.Tag = "muted";
                characterCount.ForeColor = darkMode ? DarkMutedColor : MutedColor;
                return;
            }

            messageInput.Text = Clipboard.GetText(TextDataFormat.UnicodeText);
            messageInput.Focus();
        }
        catch (ExternalException error)
        {
            characterCount.Text = "Não foi possível ler a área de transferência.";
            characterCount.Tag = "risk-danger";
            characterCount.ForeColor = darkMode ? Color.FromArgb(255, 150, 137) : DangerColor;
            MessageBox.Show(
                "Não foi possível aceder à área de transferência.\r\n\r\n" + error.Message,
                "Erro ao colar mensagem",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string GetExampleMessage(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 1:
                return "A sua conta bancária será bloqueada. Envie o código de acesso para evitar a suspensão.";
            case 2:
                return "A sua encomenda está retida. Pague uma taxa de entrega para receber a encomenda.";
            case 3:
                return "Envie o código de verificação que recebeu para confirmarmos a sua identidade.";
            case 4:
                return "A consulta está confirmada para terça-feira às 10:30. Pode alterar a marcação através da aplicação oficial.";
            case 5:
                return "Olá, tudo bem?";
            default:
                return null;
        }
    }

    private void CopyAnalysisResult()
    {
        if (displayedAnalysis == null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(BuildAnalysisSummary());
            characterCount.Text = "Resumo copiado. A mensagem original não foi incluída.";
            characterCount.Tag = "muted";
            characterCount.ForeColor = darkMode ? DarkMutedColor : MutedColor;
        }
        catch (ExternalException error)
        {
            MessageBox.Show(
                "Não foi possível copiar o resultado para a área de transferência.\r\n\r\n" + error.Message,
                "Erro ao copiar resultado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private string BuildAnalysisSummary()
    {
        StringBuilder summary = new StringBuilder();
        summary.AppendLine(statusLabel.Text);
        summary.AppendLine("Pontuação heurística: " + displayedAnalysis.Score +
            " pontos (não representa uma probabilidade).");
        summary.AppendLine("Avaliação conclusiva: " +
            (displayedAnalysis.IsConclusive ? "sim" : "não") + ".");
        summary.AppendLine("Sinais encontrados: " + displayedAnalysis.Signals.Count);
        summary.AppendLine();
        summary.AppendLine("Resumo");
        summary.AppendLine(displayedAnalysis.Summary);

        if (displayedAnalysis.Signals.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("Sinais identificados");
            foreach (AnalysisSignal signal in displayedAnalysis.Signals)
            {
                summary.AppendLine("• " + signal.Label + " (peso heurístico: " +
                    signal.Weight + " pontos)");
                foreach (AnalysisEvidence evidence in signal.Evidence)
                {
                    summary.AppendLine("  " + evidence.Explanation);
                }
            }
        }
        else
        {
            summary.AppendLine();
            summary.AppendLine("Sinais identificados");
            summary.AppendLine("O motor não identificou sinais nesta mensagem.");
        }

        summary.AppendLine();
        summary.AppendLine("Explicação e contexto");
        foreach (AnalysisSignal signal in displayedAnalysis.Signals)
        {
            foreach (AnalysisEvidence evidence in signal.Evidence)
            {
                summary.AppendLine("• " + signal.Label + ": " + evidence.Explanation);
            }
        }

        summary.AppendLine();
        summary.AppendLine("Recomendações de segurança");
        if (displayedAnalysis.Recommendations.Count == 0)
        {
            summary.AppendLine("O motor não devolveu recomendações para esta análise.");
        }
        foreach (string recommendation in displayedAnalysis.Recommendations)
        {
            summary.AppendLine("• " + recommendation);
        }
        summary.AppendLine();
        summary.AppendLine("Limitações da análise");
        if (displayedAnalysis.Limitations.Count == 0)
        {
            summary.AppendLine("O motor não devolveu limitações para esta análise.");
        }
        foreach (string limitation in displayedAnalysis.Limitations)
        {
            summary.AppendLine("• " + limitation);
        }
        return summary.ToString().Trim();
    }

    private void DisplayAnalysis(ScamAnalysisResult analysis)
    {
        displayedAnalysis = analysis;
        copyResultButton.Enabled = true;
        Color levelColor;
        string levelLabel;
        string heading;
        string themeRole;
        switch (analysis.RiskLevel)
        {
            case ScamRiskLevel.High:
                levelColor = darkMode ? Color.FromArgb(255, 150, 137) : DangerColor;
                levelLabel = "ALTO RISCO";
                heading = "A mensagem apresenta sinais fortes de risco";
                themeRole = "risk-danger";
                break;
            case ScamRiskLevel.Moderate:
                levelColor = darkMode ? Color.FromArgb(255, 196, 105) : WarningColor;
                levelLabel = "RISCO MODERADO";
                heading = "Confirme antes de agir";
                themeRole = "risk-warning";
                break;
            case ScamRiskLevel.Low:
                levelColor = darkMode ? DarkBrandColor : BrandColor;
                levelLabel = "BAIXO RISCO APARENTE";
                heading = "Não foram detetados sinais comuns";
                themeRole = "brand";
                break;
            default:
                levelColor = darkMode ? DarkMutedColor : MutedColor;
                levelLabel = "INCONCLUSIVO";
                heading = "Não há contexto suficiente";
                themeRole = "muted";
                break;
        }

        statusLabel.Text = levelLabel;
        statusLabel.Tag = "status-text";
        statusBadge.Tag = analysis.RiskLevel == ScamRiskLevel.High
            ? "status-danger"
            : analysis.RiskLevel == ScamRiskLevel.Moderate
                ? "status-warning"
                : analysis.RiskLevel == ScamRiskLevel.Inconclusive
                    ? "status-neutral"
                    : "status-ok";
        resultHeading.Text = heading;
        resultHeading.Tag = themeRole;
        resultSummary.Text = analysis.Summary;
        resultAccent.BackColor = levelColor;
        resultAccent.Width = 1;
        animationStep = 0;
        resultAnimationTimer.Stop();
        resultAnimationTimer.Start();

        List<string> summaryItems = new List<string>
        {
            "Pontuação heurística: " + analysis.Score + " pontos (não representa uma probabilidade).",
            "Sinais identificados: " + analysis.Signals.Count + ".",
            "Avaliação conclusiva segundo as regras: " + (analysis.IsConclusive ? "sim." : "não.")
        };
        List<string> signalItems = new List<string>();
        List<string> explanationItems = new List<string>();
        foreach (AnalysisSignal signal in analysis.Signals)
        {
            signalItems.Add(signal.Label + " — peso heurístico: " + signal.Weight + " pontos.");
            foreach (AnalysisEvidence evidence in signal.Evidence)
            {
                signalItems.Add("Excerto: \"" + evidence.Excerpt + "\"");
                explanationItems.Add(signal.Label + ": " + evidence.Explanation);
            }
        }
        if (signalItems.Count == 0)
        {
            signalItems.Add("O motor não identificou sinais nesta mensagem.");
        }
        if (explanationItems.Count == 0)
        {
            explanationItems.Add("O motor não forneceu explicações adicionais para esta análise.");
        }

        List<string> recommendationItems = new List<string>(analysis.Recommendations);
        if (recommendationItems.Count == 0)
        {
            recommendationItems.Add("O motor não devolveu recomendações para esta análise.");
        }
        List<string> limitationItems = new List<string>(analysis.Limitations);
        if (limitationItems.Count == 0)
        {
            limitationItems.Add("O motor não devolveu limitações para esta análise.");
        }
        analysisSections[0].SetItems(summaryItems);
        analysisSections[1].SetItems(signalItems);
        analysisSections[2].SetItems(explanationItems);
        analysisSections[3].SetItems(recommendationItems);
        analysisSections[4].SetItems(limitationItems);
        analysisSections[0].SetExpanded(true);

        ResizeContent();
        ApplyTheme();
    }

    private void ResetResult()
    {
        statusLabel.Text = "PRONTA PARA ANALISAR";
        statusLabel.Tag = "status-text";
        statusBadge.Tag = "status-ok";
        displayedAnalysis = null;
        copyResultButton.Enabled = false;
        resultHeading.Text = "Pronto para analisar";
        resultHeading.Tag = "text";
        resultSummary.Text = "Cole uma mensagem no campo à esquerda e escolha Analisar mensagem para ver os resultados.";
        foreach (AnalysisResultSection section in analysisSections)
        {
            section.SetItems(new string[0]);
            section.SetExpanded(false);
        }
        resultAccent.Height = resultCard.ClientSize.Height;
        resultAccent.BackColor = BrandColor;
        ApplyTheme();
    }

    private static Bitmap CreateOfficialLogoImage()
    {
        Bitmap bitmap = new Bitmap(128, 128);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using (GraphicsPath background = new GraphicsPath())
            {
                background.AddArc(0, 0, 56, 56, 180, 90);
                background.AddArc(72, 0, 56, 56, 270, 90);
                background.AddArc(72, 72, 56, 56, 0, 90);
                background.AddArc(0, 72, 56, 56, 90, 90);
                background.CloseFigure();
                using (SolidBrush green = new SolidBrush(Color.FromArgb(23, 107, 80)))
                {
                    graphics.FillPath(green, background);
                }
            }

            graphics.ScaleTransform(0.25F, 0.25F);
            using (GraphicsPath outerShield = new GraphicsPath())
            {
                outerShield.AddLine(256, 64, 400, 120);
                outerShield.AddLine(400, 120, 400, 232);
                outerShield.AddBezier(400, 232, 400, 332, 338, 404, 256, 448);
                outerShield.AddBezier(256, 448, 174, 404, 112, 332, 112, 232);
                outerShield.AddLine(112, 232, 112, 120);
                outerShield.CloseFigure();
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    graphics.FillPath(white, outerShield);
                }
            }
            using (GraphicsPath innerShield = new GraphicsPath())
            {
                innerShield.AddLine(256, 105, 365, 148);
                innerShield.AddLine(365, 148, 365, 232);
                innerShield.AddBezier(365, 232, 365, 309, 322, 367, 256, 407);
                innerShield.AddBezier(256, 407, 190, 367, 147, 309, 147, 232);
                innerShield.AddLine(147, 232, 147, 148);
                innerShield.CloseFigure();
                using (SolidBrush green = new SolidBrush(Color.FromArgb(23, 107, 80)))
                {
                    graphics.FillPath(green, innerShield);
                }
            }

            Point[] checkMark =
            {
                new Point(226, 282), new Point(199, 255), new Point(177, 277),
                new Point(226, 326), new Point(335, 217), new Point(313, 195)
            };
            using (SolidBrush white = new SolidBrush(Color.White))
            {
                graphics.FillPolygon(white, checkMark);
            }
        }
        return bitmap;
    }

    private static void SaveOfficialIcon(string path)
    {
        using (Bitmap bitmap = CreateOfficialLogoImage())
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[bitmap.Width * bitmap.Height * 4];
            try
            {
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            int maskStride = ((bitmap.Width + 31) / 32) * 4;
            int imageSize = 40 + pixels.Length + maskStride * bitmap.Height;
            using (BinaryWriter icon = new BinaryWriter(File.Create(path)))
            {
                icon.Write((short)0);
                icon.Write((short)1);
                icon.Write((short)1);
                icon.Write((byte)bitmap.Width);
                icon.Write((byte)bitmap.Height);
                icon.Write((byte)0);
                icon.Write((byte)0);
                icon.Write((short)1);
                icon.Write((short)32);
                icon.Write(imageSize);
                icon.Write(22);
                icon.Write(40);
                icon.Write(bitmap.Width);
                icon.Write(bitmap.Height * 2);
                icon.Write((short)1);
                icon.Write((short)32);
                icon.Write(0);
                icon.Write(pixels.Length + maskStride * bitmap.Height);
                icon.Write(0);
                icon.Write(0);
                icon.Write(0);
                icon.Write(0);

                for (int row = bitmap.Height - 1; row >= 0; row--)
                {
                    icon.Write(pixels, row * bitmap.Width * 4, bitmap.Width * 4);
                }

                for (int row = bitmap.Height - 1; row >= 0; row--)
                {
                    byte[] mask = new byte[maskStride];
                    for (int column = 0; column < bitmap.Width; column++)
                    {
                        if (pixels[(row * bitmap.Width + column) * 4 + 3] < 128)
                        {
                            mask[column / 8] |= (byte)(0x80 >> (column % 8));
                        }
                    }
                    icon.Write(mask);
                }
            }
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    private Panel MakeCard(int height)
    {
        Panel card = new Panel
        {
            BackColor = Color.White,
            Height = height,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 12),
            Tag = "card"
        };
        card.Paint += delegate(object sender, PaintEventArgs e)
        {
            using (Pen border = new Pen(darkMode ? DarkBorderColor : CardBorderColor))
            {
                Rectangle bounds = card.ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                e.Graphics.DrawRectangle(border, bounds);
            }
        };
        return card;
    }

    private static Label MakeLabel(
        string text, float size, Color color, bool bold,
        int left = 0, int top = 0)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Location = new Point(left, top),
            Font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", size,
                bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color,
            Tag = GetLabelThemeRole(color)
        };
    }

    private static string GetLabelThemeRole(Color color)
    {
        if (color.ToArgb() == MutedColor.ToArgb())
        {
            return "muted";
        }
        if (color.ToArgb() == BrandColor.ToArgb())
        {
            return "brand";
        }
        if (color.ToArgb() == WarningColor.ToArgb())
        {
            return "risk-warning";
        }
        if (color.ToArgb() == DangerColor.ToArgb())
        {
            return "risk-danger";
        }
        return color.ToArgb() == Color.White.ToArgb() ? "header-text" : "text";
    }

    private static Button MakeButton(string text, Color background, Color foreground, bool bold)
    {
        Button button = new Button
        {
            Text = text,
            BackColor = background,
            ForeColor = foreground,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, bold ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Tag = background.ToArgb() == Color.White.ToArgb() ? "button-secondary" : "button-primary"
        };
        button.FlatAppearance.MouseOverBackColor = background == BrandColor
            ? Color.FromArgb(19, 91, 68)
            : Color.FromArgb(242, 247, 243);
        return button;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--export-icon")
        {
            try
            {
                SaveOfficialIcon(args[1]);
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("Não foi possível criar o ícone: " + error.Message);
                return 1;
            }
        }

        if (args.Length == 1 && args[0] == "--self-test")
        {
            return RunSelfTests();
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            Application.Run(new RedeSeguraApp());
            return 0;
        }
        catch (Exception error)
        {
            MessageBox.Show(
                "A aplicação Rede Segura não conseguiu iniciar.\r\n\r\n" + error.Message,
                "Erro ao abrir a aplicação",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }

    private static int RunSelfTests()
    {
        string[] messages =
        {
            "A sua conta foi bloqueada. Envie o código de autenticação agora para recuperar o acesso.",
            "O banco nunca pede a sua senha nem o código por mensagem.",
            "A fatura está disponível na aplicação oficial e pode ser consultada quando quiser.",
            "Dame 50 $ agora, por favor.",
            "para continuar envie 43$",
            "Olá, tudo bem?"
        };
        ScamRiskLevel[] expectedLevels =
        {
            ScamRiskLevel.High,
            ScamRiskLevel.Low,
            ScamRiskLevel.Low,
            ScamRiskLevel.Moderate,
            ScamRiskLevel.Moderate,
            ScamRiskLevel.Inconclusive
        };
        for (int index = 0; index < messages.Length; index++)
        {
            ScamAnalysisResult result = ScamAnalyzer.Analyze(messages[index]);
            if (result.RiskLevel != expectedLevels[index])
            {
                Console.Error.WriteLine("O teste interno do analisador falhou: {0}; esperado={1}; real={2}.",
                    index, expectedLevels[index], result.RiskLevel);
                return 1;
            }
        }

        ScamAnalysisResult conditionalPayment = ScamAnalyzer.Analyze("para continuar envie 43$");
        if (!conditionalPayment.Signals.Any(signal =>
            signal.Id == "payment-condition-combination"))
        {
            Console.Error.WriteLine("O pedido de pagamento condicionado não foi reconhecido.");
            return 1;
        }

        Console.WriteLine("Seis verificações rápidas do analisador passaram.");
        return 0;
    }
}
