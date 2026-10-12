using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    private readonly Button checkButton;
    private readonly Panel inputCard;
    private readonly Panel statusBadge;
    private readonly Label statusLabel;
    private readonly Label resultHeading;
    private readonly Label resultSummary;
    private readonly Label messagePreview;
    private readonly FlowLayoutPanel resultDetails;
    private readonly Panel resultCard;
    private readonly TableLayoutPanel content;
    private readonly Panel body;
    private readonly TableLayoutPanel workspace;
    private readonly TableLayoutPanel inputColumn;
    private readonly Panel resultScroll;
    private readonly Button themeButton;
    private readonly Panel resultAccent;
    private readonly Timer fadeTimer;
    private readonly Timer resultAnimationTimer;
    private readonly BackgroundWorker analysisWorker = new BackgroundWorker();
    private bool darkMode;
    private int animationStep;
    private string pendingAnalysisMessage;

    private RedeSeguraApp()
    {
        Text = "Rede Segura";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 650);
        ClientSize = new Size(920, 850);
        BackColor = BackgroundColor;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
        Opacity = 0;

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
        Controls.Add(content);
        SizeChanged += delegate { ResizeContent(); };

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
        body.SizeChanged += delegate { ResizeContent(); };

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
        inputColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        workspace.Controls.Add(inputColumn, 0, 0);

        FlowLayoutPanel introduction = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackgroundColor,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(0),
            Tag = "background"
        };

        Panel header = new Panel
        {
            BackColor = BrandDarkColor,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(22, 18, 16, 18),
            Tag = "header"
        };
        PictureBox logo = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 50,
            Height = 50,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = officialLogo,
            Tag = "header"
        };
        Panel brandInfo = new Panel { Dock = DockStyle.Fill, BackColor = BrandDarkColor, Tag = "header" };
        brandInfo.Controls.Add(new Label
        {
            Text = "Rede Segura",
            AutoSize = true,
            Location = new Point(16, 9),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Tag = "header-text"
        });
        brandInfo.Controls.Add(new Label
        {
            Text = "Proteção contra burlas, com mais confiança",
            AutoSize = true,
            Location = new Point(18, 42),
            ForeColor = Color.FromArgb(216, 235, 224),
            Font = new Font("Segoe UI", 9F),
            Tag = "header-muted"
        });
        themeButton = MakeButton("☾  Modo escuro", Color.FromArgb(36, 91, 70), Color.White, true);
        themeButton.Dock = DockStyle.Right;
        themeButton.Width = 142;
        themeButton.Tag = "header-button";
        themeButton.FlatAppearance.BorderSize = 0;
        themeButton.AccessibleName = "Ativar modo escuro";
        themeButton.Click += delegate { ToggleTheme(); };
        header.Controls.Add(themeButton);
        header.Controls.Add(brandInfo);
        header.Controls.Add(logo);
        content.Controls.Add(header, 0, 0);

        Label eyebrow = MakeLabel("ANÁLISE PRIVADA NO SEU DISPOSITIVO", 9F, BrandColor, true);
        eyebrow.Margin = new Padding(2, 0, 0, 8);
        introduction.Controls.Add(eyebrow);

        Label title = MakeLabel("Analise com calma. Decida com segurança.", 25F, TextColor, true);
        title.Margin = new Padding(0, 0, 0, 7);
        introduction.Controls.Add(title);

        Label description = MakeLabel(
            "Cole uma SMS, email ou mensagem para receber uma avaliação de risco, os sinais encontrados e passos práticos a seguir.",
            10.5F, MutedColor, false);
        description.Margin = new Padding(0, 0, 0, 23);
        introduction.Controls.Add(description);
        inputColumn.Controls.Add(introduction, 0, 0);

        inputCard = MakeCard(300);
        inputCard.Dock = DockStyle.Top;
        inputCard.Controls.Add(MakeLabel("Mensagem recebida", 15F, TextColor, true, 20, 18));
        inputCard.Controls.Add(MakeLabel("A análise é privada. Não inclua palavras-passe, códigos ou dados bancários.", 9.5F, MutedColor, false, 20, 47));

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
            Location = new Point(20, 77),
            Size = new Size(650, 112),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Tag = "input",
            AccessibleName = "Mensagem para analisar",
            AccessibleDescription = "Cole ou escreva uma mensagem suspeita"
        };
        messageInput.TextChanged += delegate
        {
            int length = messageInput.Text.Length;
            characterCount.Text = length > ScamAnalyzer.MaximumMessageLength
                ? "O limite de 20 000 caracteres foi ultrapassado; reduza o texto antes de analisar."
                : length == 0
                    ? "A mensagem fica apenas neste dispositivo."
                    : length + (length == 1 ? " carácter" : " caracteres");
            characterCount.Tag = "muted";
            characterCount.ForeColor = darkMode ? DarkMutedColor : MutedColor;
            if (messagePreview.Text.Length > 0)
            {
                ResetResult();
            }
        };
        messageInput.KeyDown += delegate(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AnalyzeMessage();
            }
        };
        inputCard.Controls.Add(messageInput);

        characterCount = MakeLabel("A mensagem fica apenas neste dispositivo.", 9F, MutedColor, false, 20, 198);
        characterCount.MaximumSize = new Size(360, 0);
        characterCount.AutoEllipsis = true;
        inputCard.Controls.Add(characterCount);

        clearButton = MakeButton("Limpar", Color.White, BrandColor, false);
        clearButton.FlatAppearance.BorderColor = CardBorderColor;
        clearButton.FlatAppearance.BorderSize = 1;
        clearButton.Location = new Point(160, 191);
        clearButton.Size = new Size(96, 36);
        clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        clearButton.AccessibleName = "Limpar mensagem";
        clearButton.Click += delegate
        {
            messageInput.Clear();
            messageInput.Focus();
        };
        inputCard.Controls.Add(clearButton);

        checkButton = MakeButton("Analisar mensagem", BrandColor, Color.White, true);
        checkButton.Tag = "button-primary";
        checkButton.FlatAppearance.BorderSize = 0;
        checkButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(19, 91, 68);
        checkButton.FlatAppearance.MouseDownBackColor = BrandDarkColor;
        checkButton.Location = new Point(20, 237);
        checkButton.Size = new Size(650, 48);
        checkButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        checkButton.AccessibleName = "Enviar mensagem para análise";
        checkButton.Click += delegate { AnalyzeMessage(); };
        inputCard.Controls.Add(checkButton);
        AcceptButton = checkButton;
        inputColumn.Controls.Add(inputCard, 0, 1);

        resultCard = MakeCard(300);
        resultCard.Dock = DockStyle.Fill;
        resultCard.Margin = new Padding(12, 0, 0, 0);
        resultCard.Controls.Add(MakeLabel("RESULTADO DA ANÁLISE", 9F, MutedColor, true, 20, 19));

        resultAccent = new Panel
        {
            BackColor = BrandColor,
            Location = new Point(0, 0),
            Size = new Size(5, resultCard.Height)
        };
        resultCard.Controls.Add(resultAccent);

        statusBadge = new Panel
        {
            BackColor = Color.FromArgb(232, 243, 237),
            Location = new Point(20, 46),
            Size = new Size(202, 28),
            Tag = "status-ok"
        };
        statusLabel = MakeLabel("PRONTA PARA ANALISAR", 8F, BrandColor, true, 9, 6);
        statusBadge.Controls.Add(statusLabel);
        resultCard.Controls.Add(statusBadge);

        resultHeading = MakeLabel(
            "O resultado aparecerá aqui",
            17F, TextColor, true, 20, 86);
        resultCard.Controls.Add(resultHeading);

        resultSummary = MakeLabel(
            "Cole uma mensagem para ver a avaliação, os sinais encontrados e recomendações práticas.",
            10F, MutedColor, false, 20, 119);
        resultCard.Controls.Add(resultSummary);

        messagePreview = MakeLabel("", 9.5F, MutedColor, false, 20, 164);
        messagePreview.AutoEllipsis = true;
        messagePreview.MaximumSize = new Size(680, 48);
        resultCard.Controls.Add(messagePreview);

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
        resultScroll = new Panel
        {
            Location = new Point(20, 213),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoScroll = true,
            BackColor = Color.White,
            Tag = "card"
        };
        resultScroll.Controls.Add(resultDetails);
        resultCard.Controls.Add(resultScroll);
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
            checkButton.Text = "Analisar mensagem";
            if (e.Error != null)
            {
                pendingAnalysisMessage = null;
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
        ResizeContent();
    }

    private void ResizeContent()
    {
        if (body == null || workspace == null || inputColumn == null ||
            resultCard == null || messageInput == null)
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
        int breakpoint = (int)Math.Round(1040F * dpiScale);
        bool useColumns = availableWidth >= breakpoint;
        int workspaceWidth = Math.Min(availableWidth, (int)Math.Round(1680F * dpiScale));

        workspace.SuspendLayout();
        workspace.Controls.Clear();
        workspace.ColumnStyles.Clear();
        workspace.RowStyles.Clear();
        workspace.Margin = new Padding(0);
        workspace.Width = workspaceWidth;
        workspace.Location = new Point(
            Math.Max(0, (availableWidth - workspaceWidth) / 2),
            0);

        if (useColumns)
        {
            body.AutoScroll = false;
            workspace.ColumnCount = 2;
            workspace.RowCount = 1;
            workspace.Dock = DockStyle.None;
            workspace.Anchor = AnchorStyles.Top;
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            workspace.Height = availableHeight;
            inputColumn.Margin = new Padding(0, 0, 12, 0);
            resultCard.Margin = new Padding(12, 0, 0, 0);
            workspace.Controls.Add(inputColumn, 0, 0);
            workspace.Controls.Add(resultCard, 1, 0);
        }
        else
        {
            body.AutoScroll = true;
            workspace.ColumnCount = 1;
            workspace.RowCount = 2;
            workspace.Dock = DockStyle.None;
            workspace.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            FlowLayoutPanel intro = inputColumn.Controls[0] as FlowLayoutPanel;
            int introHeight = intro == null
                ? 0
                : intro.GetPreferredSize(new Size(workspaceWidth, 0)).Height;
            int inputColumnHeight = introHeight + 14 + inputCard.Height;
            int resultHeight = Math.Max(320, (int)Math.Round(380F * dpiScale));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, inputColumnHeight + 20));
            workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, resultHeight));
            int stackedHeight = inputColumnHeight + resultHeight + 20;
            workspace.Height = Math.Max(availableHeight, stackedHeight);
            inputColumn.Height = inputColumnHeight;
            inputColumn.Margin = new Padding(0, 0, 0, 20);
            resultCard.Margin = new Padding(0);
            workspace.Controls.Add(inputColumn, 0, 0);
            workspace.Controls.Add(resultCard, 0, 1);
        }

        workspace.ResumeLayout(true);

        int inputWidth = Math.Max(200, inputCard.ClientSize.Width - inputCard.Padding.Horizontal - 40);
        messageInput.Width = inputWidth;
        checkButton.Width = inputWidth;
        clearButton.Location = new Point(
            Math.Max(20, inputCard.ClientSize.Width - clearButton.Width - 20),
            191);
        characterCount.MaximumSize = new Size(Math.Max(100, inputWidth - 116), 0);

        int resultWidth = Math.Max(200, resultCard.ClientSize.Width - resultCard.Padding.Horizontal - 40);
        int detailHeight = Math.Max(80, resultCard.ClientSize.Height - 235);
        resultScroll.Size = new Size(resultWidth, detailHeight);
        resultAccent.Height = resultCard.ClientSize.Height;
        statusBadge.Width = Math.Min(330, resultWidth);
        resultHeading.MaximumSize = new Size(resultWidth, 0);
        resultSummary.MaximumSize = new Size(resultWidth, 0);
        messagePreview.MaximumSize = new Size(resultWidth, 48);
        resultDetails.Width = resultWidth;
        foreach (Control item in resultDetails.Controls)
        {
            item.MaximumSize = new Size(resultWidth, 0);
            item.Width = resultWidth;
        }

        foreach (Control control in inputColumn.Controls)
        {
            FlowLayoutPanel flow = control as FlowLayoutPanel;
            if (flow != null)
            {
                foreach (Control item in flow.Controls)
                {
                    Label label = item as Label;
                    if (label != null)
                    {
                        label.MaximumSize = new Size(Math.Max(200, workspaceWidth - 16), 0);
                    }
                }
            }
        }

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

    private void ToggleTheme()
    {
        darkMode = !darkMode;
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
        checkButton.Text = "A analisar…";
        characterCount.Text = "A análise é feita localmente. Aguarde um momento.";
        characterCount.Tag = "muted";
        characterCount.ForeColor = darkMode ? DarkMutedColor : MutedColor;
        pendingAnalysisMessage = message;
        analysisWorker.RunWorkerAsync(message);
    }

    private void DisplayAnalysis(ScamAnalysisResult analysis)
    {
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
        resultSummary.Text = analysis.Summary +
            "\r\nPontuação heurística: " + analysis.Score + " pontos (não representa uma probabilidade).";
        string message = pendingAnalysisMessage ?? "";
        messagePreview.Text = "Mensagem analisada: “" +
            (message.Length > 400 ? message.Substring(0, 397) + "..." : message) + "”";
        pendingAnalysisMessage = null;
        resultDetails.Controls.Clear();
        resultAccent.BackColor = levelColor;
        resultAccent.Width = 1;
        animationStep = 0;
        resultAnimationTimer.Stop();
        resultAnimationTimer.Start();

        if (analysis.Signals.Count > 0)
        {
            AddResultSection("SINAIS E EVIDÊNCIAS", analysis.Signals.SelectMany(signal =>
                signal.Evidence.Select(evidence =>
                    "• " + signal.Label + "\r\n  “" + evidence.Excerpt + "”\r\n  " + evidence.Explanation)));
        }
        else
        {
            AddResultSection("SINAIS", new[]
            {
                analysis.RiskLevel == ScamRiskLevel.Inconclusive
                    ? "Não foram encontrados sinais suficientes para uma avaliação."
                    : "Não foram encontrados sinais comuns de risco."
            });
        }
        AddResultSection("RECOMENDAÇÕES", analysis.Recommendations.Select(item => "• " + item));
        AddResultSection("LIMITAÇÕES", analysis.Limitations.Select(item => "• " + item));

        ApplyTheme();
        body.ScrollControlIntoView(resultCard);
    }

    private void AddResultSection(string heading, IEnumerable<string> items)
    {
        Label title = MakeLabel(heading, 8.5F, BrandColor, true);
        title.MaximumSize = new Size(Math.Max(560, resultCard.ClientSize.Width - 40), 0);
        title.Margin = new Padding(0, 5, 0, 6);
        resultDetails.Controls.Add(title);
        foreach (string item in items)
        {
            Label label = MakeLabel(item, 9.5F, TextColor, false);
            label.MaximumSize = new Size(Math.Max(560, resultCard.ClientSize.Width - 40), 0);
            label.Margin = new Padding(0, 0, 0, 7);
            resultDetails.Controls.Add(label);
        }
    }

    private void ResetResult()
    {
        statusLabel.Text = "PRONTA PARA ANALISAR";
        statusLabel.Tag = "status-text";
        statusBadge.Tag = "status-ok";
        resultHeading.Text = "O resultado aparecerá aqui";
        resultHeading.Tag = "text";
        resultSummary.Text = "Cole uma mensagem para ver a avaliação, os sinais encontrados e recomendações práticas.";
        messagePreview.Text = "";
        resultDetails.Controls.Clear();
        resultCard.Height = 300;
        resultAccent.Height = resultCard.Height;
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
