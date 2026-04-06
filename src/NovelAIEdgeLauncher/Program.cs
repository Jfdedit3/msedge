using System.Diagnostics;
using System.Windows.Forms;

namespace NovelAIEdgeLauncher;

internal static class Program
{
    private const string TargetUrl = "https://novelai.net/";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            LaunchEdgeAppMode(TargetUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Impossible de lancer Microsoft Edge en mode application.\n\n" + ex.Message,
                "NovelAI Edge Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private static void LaunchEdgeAppMode(string url)
    {
        string[] candidates =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe"),
            "msedge.exe"
        };

        string executable = candidates.FirstOrDefault(File.Exists) ?? "msedge.exe";

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"--app={url}",
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }
}
