using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

internal static class RedeSeguraUiTests
{
    private static int failures;
    private static int checks;

    private static void Check(string name, bool condition)
    {
        checks++;
        if (!condition)
        {
            failures++;
            Console.Error.WriteLine("FAILED [{0}]", name);
        }
    }

    private static object GetField(object target, string name)
    {
        Type targetType = target.GetType();
        FieldInfo field = null;
        while (targetType != null && field == null)
        {
            field = targetType.GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            targetType = targetType.BaseType;
        }
        if (field == null)
        {
            throw new InvalidOperationException("Campo de teste não encontrado: " + name);
        }
        return field.GetValue(target);
    }

    private static RedeSeguraApp CreateApplication()
    {
        ConstructorInfo constructor = typeof(RedeSeguraApp).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);
        if (constructor == null)
        {
            throw new InvalidOperationException("Construtor da aplicação não encontrado.");
        }
        return (RedeSeguraApp)constructor.Invoke(new object[0]);
    }

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        RedeSeguraApp app = CreateApplication();
        try
        {
            app.Show();
            Application.DoEvents();

            string privateMarker = "PRIVATE-MARKER-731";
            ScamAnalysisResult privateMessageAnalysis = ScamAnalyzer.Analyze(
                "A sua conta foi bloqueada. Envie o código de autenticação " +
                privateMarker + " para recuperar o acesso agora.");
            Check("privacy test has source evidence",
                privateMessageAnalysis.Signals
                    .SelectMany(signal => signal.Evidence)
                    .Any(evidence => evidence.Excerpt.Contains(privateMarker)));
            typeof(RedeSeguraApp)
                .GetField("displayedAnalysis", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(app, privateMessageAnalysis);
            string copiedSummary = (string)typeof(RedeSeguraApp)
                .GetMethod("BuildAnalysisSummary", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(app, null);
            Check("copied summary excludes submitted message text",
                !copiedSummary.Contains(privateMarker));

            SidebarControl sidebar = (SidebarControl)GetField(app, "sidebar");
            Button[] navigationButtons = (Button[])GetField(sidebar, "navigationButtons");
            string[] pageFields =
            {
                null,
                "linkCheckerControl",
                "learningControl",
                "safetyGuideControl",
                "settingsControl",
                "aboutControl"
            };
            Check("six navigation entries", navigationButtons.Length == 6);
            for (int index = 0; index < navigationButtons.Length; index++)
            {
                navigationButtons[index].PerformClick();
                Application.DoEvents();
                Check("navigation selection " + index,
                    (AppSection)GetField(app, "activeSection") == (AppSection)index);

                if (index == 0)
                {
                    Check("analyzer page is visible",
                        ((TableLayoutPanel)GetField(app, "workspace")).Visible &&
                        app.AcceptButton == (Button)GetField(app, "checkButton"));
                }
                else
                {
                    Control page = (Control)GetField(app, pageFields[index]);
                    Check("page visible " + pageFields[index], page.Visible);
                    Check("page has local scroll area " + pageFields[index],
                        ((Panel)GetField(page, "scrollViewport")).AutoScroll);
                    Check("non-analyzer has no stale default action " + index,
                        index == 1
                            ? app.AcceptButton == (Button)GetField(
                                GetField(app, "linkCheckerControl"), "analyzeButton")
                            : app.AcceptButton == null);
                }
            }

            Button headerThemeButton = (Button)GetField(app, "themeButton");
            headerThemeButton.PerformClick();
            Application.DoEvents();
            Check("header toggles to light theme",
                !(bool)GetField(app, "darkMode") &&
                ((Control)GetField(app, "settingsControl")).BackColor == Color.FromArgb(244, 247, 242));

            navigationButtons[(int)AppSection.Settings].PerformClick();
            Application.DoEvents();
            SettingsControl settings = (SettingsControl)GetField(app, "settingsControl");
            Button darkThemeButton = (Button)GetField(settings, "darkThemeButton");
            darkThemeButton.PerformClick();
            Application.DoEvents();
            Check("settings applies dark theme",
                (bool)GetField(app, "darkMode") &&
                settings.BackColor == Color.FromArgb(23, 31, 36));

            Button lightThemeButton = (Button)GetField(settings, "lightThemeButton");
            lightThemeButton.PerformClick();
            Application.DoEvents();
            Check("settings applies light theme",
                !(bool)GetField(app, "darkMode") &&
                settings.BackColor == Color.FromArgb(244, 247, 242));

            navigationButtons[(int)AppSection.About].PerformClick();
            Application.DoEvents();
            AboutControl about = (AboutControl)GetField(app, "aboutControl");
            Check("about page follows current theme",
                about.Visible && about.BackColor == Color.FromArgb(244, 247, 242));

            app.MinimumSize = new Size(320, 300);
            app.ClientSize = new Size(1000, 700);
            Application.DoEvents();
            Check("narrow layout collapses sidebar",
                (bool)GetField(app, "sidebarCollapsed"));
            Check("secondary pages keep their own scroll handling",
                !((Panel)GetField(app, "body")).AutoScroll);
            navigationButtons[(int)AppSection.Analyzer].PerformClick();
            Application.DoEvents();
            Check("narrow analyzer layout enables scrolling",
                ((Panel)GetField(app, "body")).AutoScroll);

            app.ClientSize = new Size(1500, 900);
            Application.DoEvents();
            Check("wide layout expands sidebar",
                !(bool)GetField(app, "sidebarCollapsed"));
            Check("wide layout has usable workspace",
                ((TableLayoutPanel)GetField(app, "workspace")).ClientSize.Width >= 1060);
            Check("wide analyzer layout avoids outer scrolling",
                !((Panel)GetField(app, "body")).AutoScroll);
        }
        finally
        {
            app.Close();
            app.Dispose();
            Application.DoEvents();
        }

        Console.WriteLine(
            "UI smoke tests: {0} verificações; falhas: {1}.", checks, failures);
        return failures == 0 ? 0 : 1;
    }
}
