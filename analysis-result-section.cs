using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

internal sealed class AnalysisResultSection : Panel
{
    private const int HeaderHeight = 46;
    private readonly Button toggleButton;
    private readonly Panel contentPanel;
    private readonly FlowLayoutPanel contentItems;
    private readonly string heading;
    private readonly string emptyMessage;
    private bool expanded;
    private bool darkMode = true;
    private int previousWidth;

    public AnalysisResultSection(string sectionHeading, string noResultsMessage)
    {
        heading = sectionHeading;
        emptyMessage = noResultsMessage;
        AutoSize = false;
        BackColor = Color.FromArgb(32, 42, 48);
        Margin = new Padding(0, 0, 0, 6);
        Padding = new Padding(0);
        Height = HeaderHeight;
        Tag = "result-section";
        AccessibleRole = AccessibleRole.Grouping;

        toggleButton = new Button
        {
            Dock = DockStyle.Top,
            Height = HeaderHeight - 2,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 8, 0),
            UseVisualStyleBackColor = false,
            Tag = "result-section-header",
            AccessibleRole = AccessibleRole.PushButton
        };
        toggleButton.FlatAppearance.BorderSize = 0;
        toggleButton.Text = "\u203a  " + heading;
        toggleButton.AccessibleName = heading;
        toggleButton.AccessibleDescription = "Expandir esta secção.";
        toggleButton.Click += delegate { SetExpanded(!expanded); };

        contentPanel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Padding = new Padding(14, 10, 14, 12),
            Visible = false,
            Tag = "result-section-content"
        };
        contentItems = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0),
            Padding = new Padding(0),
            WrapContents = false,
            Tag = "result-section-content"
        };
        contentPanel.Controls.Add(contentItems);
        Controls.Add(contentPanel);
        Controls.Add(toggleButton);
        SizeChanged += delegate
        {
            if (Width != previousWidth)
            {
                previousWidth = Width;
                UpdateSectionLayout();
            }
        };
        SetItems(new string[0]);
        UpdateSectionLayout();
    }

    public void SetItems(IEnumerable<string> values)
    {
        contentItems.SuspendLayout();
        while (contentItems.Controls.Count > 0)
        {
            Control previousItem = contentItems.Controls[0];
            contentItems.Controls.RemoveAt(0);
            previousItem.Dispose();
        }
        bool hasItems = false;
        foreach (string value in values)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            Label item = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(239, 244, 241),
                MaximumSize = new Size(Math.Max(120, Width - 34), 0),
                Margin = new Padding(0, 0, 0, 8),
                Tag = "text",
                Text = value
            };
            contentItems.Controls.Add(item);
            hasItems = true;
        }

        if (!hasItems)
        {
            contentItems.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(174, 188, 181),
                MaximumSize = new Size(Math.Max(120, Width - 34), 0),
                Margin = new Padding(0),
                Tag = "muted",
                Text = emptyMessage
            });
        }
        contentItems.ResumeLayout(true);
        UpdateSectionLayout();
    }

    public void ApplyTheme(bool useDarkMode)
    {
        darkMode = useDarkMode;
        BackColor = darkMode ? Color.FromArgb(32, 42, 48) : Color.White;
        Invalidate();
        UpdateSectionLayout();
    }

    public void SetExpanded(bool value)
    {
        expanded = value;
        contentPanel.Visible = expanded;
        toggleButton.Text = (expanded ? "\u2304  " : "\u203a  ") + heading;
        toggleButton.AccessibleName = heading;
        toggleButton.AccessibleDescription = expanded
            ? "Recolher esta secção."
            : "Expandir esta secção.";
        UpdateSectionLayout();
        if (Parent != null)
        {
            Parent.PerformLayout();
        }
    }

    private void UpdateSectionLayout()
    {
        if (toggleButton == null || contentPanel == null || contentItems == null)
        {
            return;
        }

        int clientWidth = Math.Max(140, ClientSize.Width - 2);
        toggleButton.Width = clientWidth;
        contentPanel.Width = clientWidth;
        contentPanel.Top = HeaderHeight - 1;
        contentItems.MaximumSize = new Size(Math.Max(120, clientWidth - contentPanel.Padding.Horizontal), 0);
        contentItems.Width = Math.Max(120, clientWidth - contentPanel.Padding.Horizontal);
        foreach (Control control in contentItems.Controls)
        {
            Label label = control as Label;
            if (label != null)
            {
                label.MaximumSize = new Size(Math.Max(120, contentItems.Width - 8), 0);
            }
        }
        contentItems.PerformLayout();
        contentPanel.Height = contentItems.PreferredSize.Height + contentPanel.Padding.Vertical;
        Height = HeaderHeight + (expanded ? contentPanel.Height : 0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (Pen border = new Pen(darkMode
            ? Color.FromArgb(57, 72, 78)
            : Color.FromArgb(224, 233, 226)))
        {
            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;
            e.Graphics.DrawRectangle(border, bounds);
        }
    }
}
