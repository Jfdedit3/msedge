using CefSharp;
using CefSharp.WinForms;

namespace ChromiumBrowserPatched;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var settings = new CefSettings
        {
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromiumBrowserPatched", "Cache"),
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
