using System;
using System.Drawing;
using System.Windows.Forms;

internal sealed class LinkCheckerControl : UserControl
{
    private static readonly Color DarkBackground = Color.FromArgb(23, 31, 36);
    private static readonly Color DarkCard = Color.FromArgb(32, 42, 48);
    private static readonly Color DarkInput = Color.FromArgb(25, 35, 40);
    private static readonly Color DarkText = Color.FromArgb(239, 244, 241);
    private static readonly Color DarkMuted = Color.FromArgb(174, 188, 181);
    private static readonly Color DarkBorder = Color.FromArgb(57, 72, 78);
    private static readonly Color LightBackground = Color.FromArgb(244, 247, 242);
    private static readonly Color LightText = Color.FromArgb(35, 48, 42);
    private static readonly Color LightMuted = Color.FromArgb(98, 111, 103);
    private static readonly Color LightBorder = Color.FromArgb(224, 233, 226);
    private readonly Panel scrollViewport;
    private readonly FlowLayoutPanel pageContent;
    private readonly Panel inputCard;
    private readonly Panel resultCard;
    private readonly Panel resultDetailsViewport;
    private readonly TextBox addressInput;
    private readonly Button analyzeButton;
    private readonly Button clearButton;
    private readonly Label statusHeading;
    private readonly Label resultSummary;
    private readonly Label hostValue;
    private readonly Label domainValue;
    private readonly AnalysisResultSection findingsSection;
    private readonly AnalysisResultSection recommendationsSection;
    private readonly AnalysisResultSection limitationsSection;
    private bool darkMode = true;
    private bool hasResult;

    public LinkCheckerControl()
    {
        Dock = DockStyle.Fill;
        BackColor = DarkBackground;
        Margin = new Padding(0);
        AccessibleName = "Verificar links";
        AccessibleRole = AccessibleRole.Pane;

        scrollViewport = new Panel
        {
            AutoScroll = true,
            BackColor = DarkBackground,
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            Tag = "link-background"
        };
        pageContent = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0),
            Padding = new Padding(0),
            WrapContents = false
        };
        scrollViewport.Controls.Add(pageContent);
        Controls.Add(scrollViewport);
        scrollViewport.SizeChanged += delegate { ResizePage(); };

        inputCard = CreateCard(224);
        TableLayoutPanel inputLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18),
            Margin = new Padding(0),
            Tag = "link-card-content"
        };
        inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        inputLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        inputLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        inputCard.Controls.Add(inputLayout);

        Label inputTitle = MakeLabel("Endereço para analisar", 14F, true, "link-text");
        inputTitle.Margin = new Padding(0, 0, 0, 4);
        inputLayout.Controls.Add(inputTitle, 0, 0);
        Label inputHint = MakeLabel(
            "A análise é local e verifica apenas a estrutura do URL. O endereço nunca é aberto nem contactado.",
            9.5F, false, "link-muted");
        inputHint.MaximumSize = new Size(760, 0);
        inputHint.Margin = new Padding(0, 0, 0, 10);
        inputLayout.Controls.Add(inputHint, 0, 1);

        addressInput = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5F),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = DarkInput,
            ForeColor = DarkText,
            Margin = new Padding(0, 0, 0, 8),
            Tag = "link-input",
            AccessibleName = "Endereço para verificar",
            AccessibleDescription = "Introduza um URL HTTP ou HTTPS. Não é aberto nem enviado."
        };
        addressInput.TextChanged += delegate
        {
            if (hasResult)
            {
                ResetResult();
            }
        };
        inputLayout.Controls.Add(addressInput, 0, 2);

        FlowLayoutPanel actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(0),
            WrapContents = true
        };
        analyzeButton = MakeButton("Analisar link", true);
        analyzeButton.AccessibleName = "Analisar a estrutura do link";
        analyzeButton.Click += delegate { AnalyzeAddress(); };
        clearButton = MakeButton("Limpar", false);
        clearButton.AccessibleName = "Limpar o endereço introduzido e o resultado";
        clearButton.Click += delegate { ClearAddress(); };
        actions.Controls.Add(analyzeButton);
        actions.Controls.Add(clearButton);
        inputLayout.Controls.Add(actions, 0, 3);
        Label privacyHint = MakeLabel(
            "Sem pedidos de rede, sem abertura automática e sem histórico.",
            8.5F, false, "link-muted");
        privacyHint.Margin = new Padding(0, 2, 0, 0);
        inputLayout.Controls.Add(privacyHint, 0, 4);
        pageContent.Controls.Add(inputCard);

        resultCard = CreateCard(520);
        TableLayoutPanel resultLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18),
            Margin = new Padding(0),
            Tag = "link-card-content"
        };
        resultLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resultLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        resultCard.Controls.Add(resultLayout);

        Label resultTitle = MakeLabel("Resultado da análise local", 14F, true, "link-text");
        resultTitle.Margin = new Padding(0, 0, 0, 8);
        resultLayout.Controls.Add(resultTitle, 0, 0);
        statusHeading = MakeLabel("Pronto para verificar", 16F, true, "link-text");
        statusHeading.Margin = new Padding(0, 0, 0, 5);
        resultLayout.Controls.Add(statusHeading, 0, 1);
        resultSummary = MakeLabel(
            "Introduza um endereço e selecione Analisar link. A análise não determina se um site é seguro.",
            9.5F, false, "link-muted");
        resultSummary.Margin = new Padding(0, 0, 0, 8);
        resultLayout.Controls.Add(resultSummary, 0, 2);

        FlowLayoutPanel hostDetails = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0),
            WrapContents = false
        };
        hostValue = MakeLabel("", 9.5F, false, "link-text");
        hostValue.MaximumSize = new Size(760, 0);
        domainValue = MakeLabel("", 9.5F, false, "link-text");
        domainValue.MaximumSize = new Size(760, 0);
        hostDetails.Controls.Add(hostValue);
        hostDetails.Controls.Add(domainValue);
        resultLayout.Controls.Add(hostDetails, 0, 3);

        FlowLayoutPanel resultSections = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0),
            Padding = new Padding(0),
            WrapContents = false,
            Tag = "link-card-content"
        };
        findingsSection = new AnalysisResultSection(
            "Sinais estruturais", "A análise ainda não foi executada.");
        recommendationsSection = new AnalysisResultSection(
            "Recomendações de segurança", "As recomendações serão apresentadas após a análise.");
        limitationsSection = new AnalysisResultSection(
            "Limitações", "As limitações serão apresentadas após a análise.");
        resultSections.Controls.Add(findingsSection);
        resultSections.Controls.Add(recommendationsSection);
        resultSections.Controls.Add(limitationsSection);
        resultDetailsViewport = new Panel
        {
            AutoScroll = true,
            BackColor = DarkCard,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Tag = "link-card-content"
        };
        resultDetailsViewport.Controls.Add(resultSections);
        resultLayout.Controls.Add(resultDetailsViewport, 0, 4);
        pageContent.Controls.Add(resultCard);

        ApplyTheme(true);
    }

    public Button AnalyzeButton
    {
        get { return analyzeButton; }
    }

    public void AnalyzeAddress()
    {
        UrlStructureResult analysis = UrlStructureAnalyzer.Analyze(addressInput.Text);
        hasResult = true;
        if (!analysis.IsValid)
        {
            statusHeading.Text = "Endereço inválido ou não suportado";
            resultSummary.Text = analysis.ErrorMessage;
            hostValue.Text = "";
            domainValue.Text = "";
        }
        else
        {
            statusHeading.Text = "Análise estrutural concluída";
            resultSummary.Text = "Esta análise não confirma a segurança nem a identidade do site.";
            hostValue.Text = "Hostname: " + analysis.HostName;
            domainValue.Text = "Domínio registável estimado: " +
                (analysis.RegistrableDomain ?? "não estimado");
        }

        if (analysis.IsValid)
        {
            findingsSection.SetItems(analysis.Findings);
        }
        else
        {
            findingsSection.SetItems(new[]
            {
                "Não foi feita uma avaliação estrutural porque o endereço é inválido."
            });
        }
        recommendationsSection.SetItems(analysis.Recommendations);
        limitationsSection.SetItems(analysis.Limitations);
        findingsSection.SetExpanded(true);
        ApplyTheme(darkMode);
    }

    public void ClearAddress()
    {
        addressInput.Clear();
        ResetResult();
        addressInput.Focus();
    }

    public void ApplyTheme(bool useDarkMode)
    {
        darkMode = useDarkMode;
        Color background = darkMode ? DarkBackground : LightBackground;
        Color card = darkMode ? DarkCard : Color.White;
        Color text = darkMode ? DarkText : LightText;
        Color muted = darkMode ? DarkMuted : LightMuted;

        BackColor = background;
        scrollViewport.BackColor = background;
        addressInput.BackColor = darkMode ? DarkInput : Color.FromArgb(250, 252, 250);
        addressInput.ForeColor = text;
        ApplyTextColor(scrollViewport, text, muted);
        inputCard.BackColor = card;
        resultCard.BackColor = card;
        resultDetailsViewport.BackColor = card;
        findingsSection.ApplyTheme(darkMode);
        recommendationsSection.ApplyTheme(darkMode);
        limitationsSection.ApplyTheme(darkMode);
        analyzeButton.BackColor = Color.FromArgb(23, 107, 80);
        analyzeButton.ForeColor = Color.White;
        clearButton.BackColor = card;
        clearButton.ForeColor = text;
        analyzeButton.FlatAppearance.BorderColor = darkMode ? DarkBorder : LightBorder;
        clearButton.FlatAppearance.BorderColor = darkMode ? DarkBorder : LightBorder;
        inputCard.Invalidate();
        resultCard.Invalidate();
    }

    private void ApplyTextColor(Control parent, Color text, Color muted)
    {
        foreach (Control child in parent.Controls)
        {
            string role = child.Tag as string;
            if (role == "link-text")
            {
                child.ForeColor = text;
            }
            else if (role == "link-muted")
            {
                child.ForeColor = muted;
            }
            ApplyTextColor(child, text, muted);
        }
    }

    private void ResetResult()
    {
        hasResult = false;
        statusHeading.Text = "Pronto para verificar";
        resultSummary.Text =
            "Introduza um endereço e selecione Analisar link. A análise não determina se um site é seguro.";
        hostValue.Text = "";
        domainValue.Text = "";
        findingsSection.SetItems(new string[0]);
        recommendationsSection.SetItems(new string[0]);
        limitationsSection.SetItems(new string[0]);
        findingsSection.SetExpanded(false);
        recommendationsSection.SetExpanded(false);
        limitationsSection.SetExpanded(false);
    }

    private void ResizePage()
    {
        if (scrollViewport == null || pageContent == null ||
            inputCard == null || resultCard == null || hostValue == null || domainValue == null)
        {
            return;
        }

        int width = Math.Max(220, scrollViewport.ClientSize.Width - scrollViewport.Padding.Horizontal);
        pageContent.Width = width;
        inputCard.Width = width;
        resultCard.Width = width;
        int contentWidth = Math.Max(160, width - 2 * 18 - SystemInformation.VerticalScrollBarWidth - 8);
        hostValue.MaximumSize = new Size(contentWidth, 0);
        domainValue.MaximumSize = new Size(contentWidth, 0);
        findingsSection.Width = contentWidth;
        recommendationsSection.Width = contentWidth;
        limitationsSection.Width = contentWidth;
    }

    private Panel CreateCard(int height)
    {
        Panel card = new Panel
        {
            BackColor = DarkCard,
            Height = height,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(0)
        };
        card.Paint += delegate(object sender, PaintEventArgs e)
        {
            using (Pen border = new Pen(darkMode ? DarkBorder : LightBorder))
            {
                Rectangle bounds = card.ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                e.Graphics.DrawRectangle(border, bounds);
            }
        };
        return card;
    }

    private Button MakeButton(string text, bool primary)
    {
        return new Button
        {
            AutoSize = true,
            BackColor = primary ? Color.FromArgb(23, 107, 80) : (darkMode ? DarkCard : Color.White),
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, primary ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = primary ? Color.White : (darkMode ? DarkText : LightText),
            Margin = new Padding(0, 0, 8, 4),
            Padding = new Padding(12, 3, 12, 3),
            Text = text,
            UseVisualStyleBackColor = false,
            AccessibleRole = AccessibleRole.PushButton
        };
    }

    private Label MakeLabel(string text, float fontSize, bool bold, string role)
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", fontSize,
                bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = role == "link-muted" ? DarkMuted : DarkText,
            Tag = role,
            Text = text
        };
    }
}
