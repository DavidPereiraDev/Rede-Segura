using System;
using System.Drawing;
using System.Windows.Forms;

internal enum AppSection
{
    Analyzer,
    LinkChecker,
    Learning,
    SafetyGuide,
    Settings,
    About
}

internal sealed class AppSectionEventArgs : EventArgs
{
    public AppSectionEventArgs(AppSection section)
    {
        Section = section;
    }

    public AppSection Section { get; private set; }
}

internal sealed class SidebarControl : UserControl
{
    private readonly PictureBox logo;
    private readonly Label brandName;
    private readonly Label brandDescription;
    private readonly Label privacyIcon;
    private readonly Label privacyText;
    private readonly Button[] navigationButtons;
    private readonly string[] navigationIcons = { "\u2315", "\u2197", "\u25c7", "\u25a4", "\u2699", "\u24d8" };
    private readonly string[] navigationLabels =
    {
        "Analisador", "Verificar links", "Aprender",
        "Guia de segurança", "Definições", "Sobre"
    };
    private readonly AppSection[] sections =
    {
        AppSection.Analyzer, AppSection.LinkChecker, AppSection.Learning,
        AppSection.SafetyGuide, AppSection.Settings, AppSection.About
    };
    private readonly Label privacyHeading;
    private AppSection activeSection;
    private bool collapsed;
    private bool darkMode = true;

    public SidebarControl(Image logoImage)
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(0);
        Padding = new Padding(12);
        BackColor = Color.FromArgb(13, 27, 28);
        AccessibleName = "Navegação principal";

        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        Controls.Add(layout);

        Panel brandPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        logo = new PictureBox
        {
            Image = logoImage,
            Location = new Point(0, 13),
            Size = new Size(52, 52),
            SizeMode = PictureBoxSizeMode.Zoom,
            AccessibleName = "Logótipo Rede Segura"
        };
        brandName = new Label
        {
            AutoSize = true,
            Text = "Rede Segura",
            Location = new Point(62, 15),
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = Color.White,
            Tag = "sidebar-text"
        };
        brandDescription = new Label
        {
            AutoSize = true,
            Text = "Proteção contra burlas,\ncom mais confiança",
            Location = new Point(63, 41),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(174, 195, 188),
            Tag = "sidebar-muted"
        };
        brandPanel.Controls.Add(brandDescription);
        brandPanel.Controls.Add(brandName);
        brandPanel.Controls.Add(logo);
        layout.Controls.Add(brandPanel, 0, 0);

        FlowLayoutPanel navigation = new FlowLayoutPanel
        {
            AutoScroll = true,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0),
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false,
            AccessibleName = "Secções da aplicação"
        };
        navigationButtons = new Button[navigationLabels.Length];
        for (int index = 0; index < navigationLabels.Length; index++)
        {
            int selectedIndex = index;
            Button button = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Height = 46,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(10, 0, 4, 0),
                Text = navigationIcons[index] + "   " + navigationLabels[index],
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = false,
                Width = 208,
                Cursor = Cursors.Hand,
                Tag = "sidebar-navigation",
                AccessibleName = navigationLabels[index],
                AccessibleRole = AccessibleRole.PageTab
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += delegate
            {
                EventHandler<AppSectionEventArgs> handler = SectionSelected;
                if (handler != null)
                {
                    handler(this, new AppSectionEventArgs(sections[selectedIndex]));
                }
            };
            navigationButtons[index] = button;
            navigation.Controls.Add(button);
        }
        layout.Controls.Add(navigation, 0, 1);

        Panel privacyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0),
            Padding = new Padding(10),
            BackColor = Color.FromArgb(20, 42, 40),
            Tag = "sidebar-privacy"
        };
        privacyIcon = new Label
        {
            AutoSize = true,
            Text = "\u26e8",
            Location = new Point(8, 9),
            Font = new Font("Segoe UI Symbol", 15F),
            ForeColor = Color.FromArgb(139, 221, 190),
            Tag = "sidebar-accent"
        };
        privacyHeading = new Label
        {
            AutoSize = true,
            Text = "Privacidade em primeiro lugar",
            Location = new Point(34, 10),
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Tag = "sidebar-text"
        };
        privacyText = new Label
        {
            AutoSize = true,
            Text = "As suas mensagens não são enviadas nem guardadas. Tudo funciona localmente.",
            Location = new Point(34, 34),
            MaximumSize = new Size(154, 0),
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(174, 195, 188),
            Tag = "sidebar-muted"
        };
        privacyPanel.Controls.Add(privacyText);
        privacyPanel.Controls.Add(privacyHeading);
        privacyPanel.Controls.Add(privacyIcon);
        layout.Controls.Add(privacyPanel, 0, 2);

        SetActiveSection(AppSection.Analyzer);
        ApplyTheme(true);
    }

    public event EventHandler<AppSectionEventArgs> SectionSelected;

    public void SetActiveSection(AppSection section)
    {
        activeSection = section;
        UpdateNavigationButtons();
    }

    public void SetCollapsed(bool value)
    {
        if (collapsed == value)
        {
            return;
        }

        collapsed = value;
        Padding = collapsed ? new Padding(8, 12, 8, 12) : new Padding(12);
        logo.Location = collapsed ? new Point(0, 13) : new Point(0, 13);
        brandName.Visible = !collapsed;
        brandDescription.Visible = !collapsed;
        privacyHeading.Visible = !collapsed;
        privacyText.Visible = !collapsed;
        privacyIcon.Location = collapsed ? new Point(11, 22) : new Point(8, 9);
        foreach (Button button in navigationButtons)
        {
            int index = Array.IndexOf(navigationButtons, button);
            button.Width = collapsed ? 48 : 208;
            button.Padding = collapsed ? new Padding(0) : new Padding(10, 0, 4, 0);
            button.Text = collapsed
                ? navigationIcons[index]
                : navigationIcons[index] + "   " + navigationLabels[index];
            button.TextAlign = collapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
        }
        UpdateNavigationButtons();
    }

    public void ApplyTheme(bool useDarkMode)
    {
        darkMode = useDarkMode;
        BackColor = darkMode ? Color.FromArgb(13, 27, 28) : Color.FromArgb(247, 250, 248);
        brandName.ForeColor = darkMode ? Color.White : Color.FromArgb(35, 48, 42);
        brandDescription.ForeColor = darkMode
            ? Color.FromArgb(174, 195, 188)
            : Color.FromArgb(98, 111, 103);
        privacyHeading.ForeColor = darkMode ? Color.White : Color.FromArgb(35, 48, 42);
        privacyText.ForeColor = darkMode
            ? Color.FromArgb(174, 195, 188)
            : Color.FromArgb(98, 111, 103);
        privacyIcon.ForeColor = darkMode
            ? Color.FromArgb(139, 221, 190)
            : Color.FromArgb(23, 107, 80);
        Panel privacyPanel = privacyHeading.Parent as Panel;
        if (privacyPanel != null)
        {
            privacyPanel.BackColor = darkMode
                ? Color.FromArgb(20, 42, 40)
                : Color.FromArgb(232, 243, 237);
        }
        UpdateNavigationButtons();
    }

    private void UpdateNavigationButtons()
    {
        for (int index = 0; index < navigationButtons.Length; index++)
        {
            bool isActive = sections[index] == activeSection;
            navigationButtons[index].BackColor = isActive
                ? (darkMode ? Color.FromArgb(16, 73, 61) : Color.FromArgb(220, 240, 230))
                : BackColor;
            navigationButtons[index].ForeColor = isActive
                ? (darkMode ? Color.FromArgb(147, 232, 200) : Color.FromArgb(16, 83, 62))
                : (darkMode ? Color.FromArgb(223, 233, 229) : Color.FromArgb(48, 64, 56));
            navigationButtons[index].FlatAppearance.MouseOverBackColor = darkMode
                ? Color.FromArgb(31, 55, 52)
                : Color.FromArgb(232, 243, 237);
            navigationButtons[index].FlatAppearance.BorderColor = darkMode
                ? Color.FromArgb(68, 147, 119)
                : Color.FromArgb(23, 107, 80);
        }
    }
}
