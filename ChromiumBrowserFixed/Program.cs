using CefSharp;
using CefSharp.WinForms;

namespace ChromiumBrowserFixed;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var settings = new CefSettings
        {
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromiumBrowserFixed", "Cache"),
            UserDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromiumBrowserFixed", "UserData"),
            Locale = "fr-FR",
            PersistSessionCookies = true,
            PersistUserPreferences = true,
            LogSeverity = LogSeverity.Disable
        };

        Cef.EnableHighDPISupport();
        Cef.Initialize(settings);

        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserForm());

        Cef.Shutdown();
    }
}
