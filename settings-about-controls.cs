using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

internal sealed class ThemeSelectionEventArgs : EventArgs
{
    public ThemeSelectionEventArgs(bool darkMode)
    {
        DarkMode = darkMode;
    }

    public bool DarkMode { get; private set; }
}

internal sealed class SettingsControl : UserControl
{
    private sealed class SettingsCardLayout
    {
        public Panel Card;
        public TableLayoutPanel Content;
        public Label Heading;
        public Label Description;
        public Control AdditionalContent;
    }

    private static readonly Color DarkBackground = Color.FromArgb(23, 31, 36);
    private static readonly Color DarkCard = Color.FromArgb(32, 42, 48);
    private static readonly Color DarkText = Color.FromArgb(239, 244, 241);
    private static readonly Color DarkMuted = Color.FromArgb(174, 188, 181);
    private static readonly Color DarkBorder = Color.FromArgb(57, 72, 78);
    private static readonly Color LightBackground = Color.FromArgb(244, 247, 242);
    private static readonly Color LightText = Color.FromArgb(35, 48, 42);
    private static readonly Color LightMuted = Color.FromArgb(98, 111, 103);
    private static readonly Color LightBorder = Color.FromArgb(224, 233, 226);
    private static readonly Color Brand = Color.FromArgb(23, 107, 80);
    private readonly Panel scrollViewport;
    private readonly FlowLayoutPanel pageContent;
    private readonly List<SettingsCardLayout> cards = new List<SettingsCardLayout>();
    private readonly Label pageTitle;
    private readonly Label pageDescription;
    private readonly Label activeTheme;
    private readonly Button lightThemeButton;
    private readonly Button darkThemeButton;
    private bool darkMode = true;

    public SettingsControl()
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(0);
        AccessibleName = "Definições";
        AccessibleRole = AccessibleRole.Pane;

        scrollViewport = new Panel
        {
            AutoScroll = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            Tag = "settings-background"
        };
        pageContent = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0),
            Padding = new Padding(0),
            WrapContents = false
        };
        scrollViewport.Controls.Add(pageContent);
        Controls.Add(scrollViewport);

        pageTitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5),
            Text = "Definições",
            Tag = "settings-title"
        };
        pageContent.Controls.Add(pageTitle);
        pageDescription = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(0, 0, 0, 16),
            Text = "Personalize a aparência da aplicação e reveja as opções de privacidade.",
            Tag = "settings-muted"
        };
        pageContent.Controls.Add(pageDescription);

        FlowLayoutPanel themeChoices = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(0),
            WrapContents = true
        };
        lightThemeButton = CreateThemeButton("Modo claro", "Ativar o modo claro.");
        lightThemeButton.Click += delegate { RaiseThemeSelected(false); };
        darkThemeButton = CreateThemeButton("Modo escuro", "Ativar o modo escuro.");
        darkThemeButton.Click += delegate { RaiseThemeSelected(true); };
        themeChoices.Controls.Add(lightThemeButton);
        themeChoices.Controls.Add(darkThemeButton);
        activeTheme = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(0),
            Tag = "settings-muted"
        };
        TableLayoutPanel themeOptions = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        themeOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        themeOptions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        themeOptions.Controls.Add(themeChoices, 0, 0);
        themeOptions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        themeOptions.Controls.Add(activeTheme, 0, 1);
        CreateCard(
            "Aparência",
            "Escolha o tema visual. A preferência aplica-se apenas durante esta sessão e não é guardada.",
            themeOptions);

        CreateCard(
            "Privacidade",
            "As mensagens e os endereços analisados não são guardados nem enviados. A análise é executada localmente; fechar a aplicação termina esta sessão.",
            null);

        scrollViewport.SizeChanged += delegate { ResizePage(); };
        ResizePage();
        ApplyTheme(true);
    }

    public event EventHandler<ThemeSelectionEventArgs> ThemeSelected;

    public void ApplyTheme(bool useDarkMode)
    {
        darkMode = useDarkMode;
        BackColor = darkMode ? DarkBackground : LightBackground;
        scrollViewport.BackColor = BackColor;
        pageTitle.ForeColor = darkMode ? DarkText : LightText;
        pageDescription.ForeColor = darkMode ? DarkMuted : LightMuted;
        foreach (SettingsCardLayout card in cards)
        {
            card.Card.BackColor = darkMode ? DarkCard : Color.White;
            card.Heading.ForeColor = darkMode ? DarkText : LightText;
            card.Description.ForeColor = darkMode ? DarkMuted : LightMuted;
            card.Card.Invalidate();
        }

        activeTheme.Text = darkMode
            ? "Tema atual: escuro"
            : "Tema atual: claro";
        activeTheme.ForeColor = darkMode ? DarkMuted : LightMuted;
        UpdateThemeButton(lightThemeButton, !darkMode);
        UpdateThemeButton(darkThemeButton, darkMode);
        ResizePage();
    }

    private void RaiseThemeSelected(bool useDarkMode)
    {
        if (darkMode == useDarkMode)
        {
            return;
        }

        EventHandler<ThemeSelectionEventArgs> handler = ThemeSelected;
        if (handler != null)
        {
            handler(this, new ThemeSelectionEventArgs(useDarkMode));
        }
    }
    private void CreateCard(
        string title,
        string description,
        Control additionalContent)
    {
        Panel card = new Panel
        {
            BackColor = DarkCard,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(16, 14, 16, 12),
            Tag = "settings-card"
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

        TableLayoutPanel cardContent = new TableLayoutPanel
        {
            AutoSize = false,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        cardContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        Label heading = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5),
            Text = title,
            Tag = "settings-heading"
        };
        Label descriptionLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 0, 0, 10),
            Text = description,
            Tag = "settings-muted"
        };
        cardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cardContent.Controls.Add(heading, 0, 0);
        cardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cardContent.Controls.Add(descriptionLabel, 0, 1);
        if (additionalContent != null)
        {
            cardContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardContent.Controls.Add(additionalContent, 0, 2);
        }
        card.Controls.Add(cardContent);
        pageContent.Controls.Add(card);
        cards.Add(new SettingsCardLayout
        {
            Card = card,
            Content = cardContent,
            Heading = heading,
            Description = descriptionLabel,
            AdditionalContent = additionalContent
        });
    }

    private Button CreateThemeButton(string text, string description)
    {
        return new Button
        {
            AutoSize = true,
            BackColor = DarkCard,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = DarkText,
            Margin = new Padding(0, 0, 8, 4),
            Padding = new Padding(12, 5, 12, 5),
            Text = text,
            Tag = text,
            UseVisualStyleBackColor = false,
            AccessibleDescription = description,
            AccessibleRole = AccessibleRole.PushButton
        };
    }

    private void UpdateThemeButton(Button button, bool selected)
    {
        button.Text = selected
            ? (string)button.Tag + " (ativo)"
            : (string)button.Tag;
        button.AccessibleName = selected
            ? button.Text + ", tema atual"
            : button.AccessibleDescription;
        button.BackColor = selected
            ? Brand
            : (darkMode ? DarkCard : Color.White);
        button.ForeColor = selected
            ? Color.White
            : (darkMode ? DarkText : LightText);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = darkMode ? DarkBorder : LightBorder;
        button.FlatAppearance.MouseOverBackColor = selected
            ? Color.FromArgb(19, 91, 68)
            : (darkMode ? Color.FromArgb(43, 56, 62) : Color.FromArgb(242, 247, 243));
    }

    private void ResizePage()
    {
        if (scrollViewport == null || pageContent == null)
        {
            return;
        }

        int width = Math.Max(220, scrollViewport.ClientSize.Width -
            scrollViewport.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
        pageContent.Width = width;
        pageTitle.MaximumSize = new Size(width, 0);
        pageDescription.MaximumSize = new Size(width, 0);
        foreach (SettingsCardLayout card in cards)
        {
            card.Card.Width = width;
            int contentWidth = Math.Max(180, width - card.Card.Padding.Horizontal);
            card.Content.Width = contentWidth;
            foreach (Control child in card.Content.Controls)
            {
                Label label = child as Label;
                if (label != null)
                {
                    label.MaximumSize = new Size(contentWidth, 0);
                }
            }
            if (card.AdditionalContent != null)
            {
                card.AdditionalContent.MaximumSize = new Size(contentWidth, 0);
                card.AdditionalContent.Width = contentWidth;
            }
            card.Content.PerformLayout();
            card.Card.Height = card.Content.GetPreferredSize(new Size(contentWidth, 0)).Height +
                card.Card.Padding.Vertical + 2;
        }
        pageContent.PerformLayout();
    }
}

internal sealed class AboutControl : SecurityContentPage
{
    public AboutControl()
    {
        AccessibleName = "Sobre a Rede Segura";
        AddCard("Rede Segura", new[]
        {
            new[] { "Objetivo", "Apoiar a identificação de sinais de risco em mensagens e endereços, com explicações e recomendações claras." },
            new[] { "Como funciona", "A análise de mensagens é baseada em regras locais e indicadores contextuais. Não é uma verificação definitiva nem substitui a confirmação através de canais oficiais." }
        });
        AddCard("Privacidade", new[]
        {
            new[] { "Sem histórico", "A aplicação não mantém um histórico das análises." },
            new[] { "Sem guardar mensagens", "As mensagens e os endereços introduzidos não são persistidos." },
            new[] { "Sem enviar dados", "As análises são realizadas no dispositivo e não enviam os conteúdos para serviços externos." }
        });
        AddCard("Verificação de links", new[]
        {
            new[] { "Análise estrutural", "A ferramenta examina características do endereço sem o abrir, consultar DNS ou contactar o site." },
            new[] { "Limitações", "As listas locais são parciais. A ausência de alertas não garante que um endereço ou site seja seguro ou legítimo." }
        });
    }
}
