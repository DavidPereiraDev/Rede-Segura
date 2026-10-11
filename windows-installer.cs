using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class WindowsInstaller
{
    private static readonly string[] Files =
    {
        "RedeSegura.exe",
        "install-windows.ps1",
    };

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--verify")
        {
            return VerifyResources();
        }

        string stagingDirectory = Path.Combine(
            Path.GetTempPath(),
            "RedeSegura-" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(stagingDirectory);
            ExtractFiles(stagingDirectory);
            return RunInstallScript(stagingDirectory);
        }
        catch (Exception error)
        {
            MessageBox.Show(
                "Não foi possível preparar a instalação da Rede Segura.\r\n\r\n" + error.Message,
                "Erro de instalação",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
        finally
        {
            try
            {
                if (Directory.Exists(stagingDirectory))
                {
                    Directory.Delete(stagingDirectory, true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static int VerifyResources()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (string file in Files)
        {
            if (assembly.GetManifestResourceStream("RedeSegura." + file) == null)
            {
                return 1;
            }
        }
        return 0;
    }

    private static void ExtractFiles(string destination)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (string file in Files)
        {
            using (Stream resource = assembly.GetManifestResourceStream("RedeSegura." + file))
            {
                if (resource == null)
                {
                    throw new InvalidOperationException("Ficheiro em falta no instalador: " + file);
                }

                using (FileStream output = File.Create(Path.Combine(destination, file)))
                {
                    resource.CopyTo(output);
                }
            }
        }
    }

    private static int RunInstallScript(string stagingDirectory)
    {
        string powershell = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            @"WindowsPowerShell\v1.0\powershell.exe");
        string script = Path.Combine(stagingDirectory, "install-windows.ps1");
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = powershell,
            Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + script + "\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };

        using (Process process = Process.Start(startInfo))
        {
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
