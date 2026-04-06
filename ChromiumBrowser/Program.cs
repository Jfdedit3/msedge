using CefSharp;
using CefSharp.WinForms;

namespace ChromiumBrowser;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var settings = new CefSettings
        {
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromiumBrowser", "Cache"),
            UserDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChromiumBrowser", "UserData"),
            Locale = "fr-FR",
            PersistSessionCookies = true,
            PersistUserPreferences = true,
            LogSeverity = LogSeverity.Disable
        };

        settings.CefCommandLineArgs.Add("disable-features", "WinUseBrowserSpellChecker");
        settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");

        Cef.EnableHighDPISupport();
        Cef.Initialize(settings);

        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserForm());

        Cef.Shutdown();
    }
}
