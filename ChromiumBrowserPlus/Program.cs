using CefSharp;
using CefSharp.WinForms;

namespace ChromiumBrowserPlus;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromiumBrowserPlus");
        var cachePath = Path.Combine(dataRoot, "Cache");
        var extensionsRoot = Path.Combine(dataRoot, "Extensions");

        Directory.CreateDirectory(dataRoot);
        Directory.CreateDirectory(cachePath);
        Directory.CreateDirectory(extensionsRoot);

        var settings = new CefSettings
        {
            CachePath = cachePath,
            Locale = "fr-FR",
            PersistSessionCookies = true,
            LogSeverity = LogSeverity.Disable
        };

        var extensionDirs = Directory.GetDirectories(extensionsRoot)
            .Where(Directory.Exists)
            .ToArray();

        if (extensionDirs.Length > 0)
        {
            settings.CefCommandLineArgs.Add("load-extension", string.Join(',', extensionDirs));
        }

        Cef.Initialize(settings);

        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserForm(dataRoot));

        Cef.Shutdown();
    }
}
