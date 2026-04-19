using CefSharp;
using CefSharp.WinForms;

namespace ChromiumBrowserFixed;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromiumBrowserFixed");
        var cachePath = Path.Combine(dataRoot, "Cache");

        Directory.CreateDirectory(dataRoot);
        Directory.CreateDirectory(cachePath);

        var settings = new CefSettings
        {
            CachePath = cachePath,
            Locale = "fr-FR",
            PersistSessionCookies = true,
            LogSeverity = LogSeverity.Disable
        };

        Cef.Initialize(settings);

        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserForm());

        Cef.Shutdown();
    }
}
