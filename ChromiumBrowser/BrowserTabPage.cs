using CefSharp;
using CefSharp.WinForms;

namespace ChromiumBrowser;

public sealed class BrowserTabPage : TabPage
{
    public ChromiumWebBrowser Browser { get; }
    public string PageTitle { get; private set; } = "New Tab";
    public bool IsLoading { get; private set; }

    public event EventHandler? TitleChanged;
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? StatusMessageChanged;

    public BrowserTabPage(string url)
    {
        Browser = new ChromiumWebBrowser(url)
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(Browser);

        Browser.TitleChanged += Browser_TitleChanged;
        Browser.AddressChanged += Browser_AddressChanged;
        Browser.LoadingStateChanged += Browser_LoadingStateChanged;
        Browser.StatusMessage += Browser_StatusMessage;
        Browser.MenuHandler = new CustomMenuHandler();
        Browser.DownloadHandler = new DownloadHandler();
        Browser.KeyboardHandler = new CustomKeyboardHandler();
    }

    private void Browser_TitleChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        PageTitle = e.NewValue?.ToString() ?? "New Tab";
        TitleChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Browser_AddressChanged(object? sender, AddressChangedEventArgs e)
    {
        AddressChanged?.Invoke(this, e.Address);
    }

    private void Browser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
    {
        IsLoading = e.IsLoading;
        LoadingStateChanged?.Invoke(this, e.IsLoading);
    }

    private void Browser_StatusMessage(object? sender, StatusMessageEventArgs e)
    {
        StatusMessageChanged?.Invoke(this, e.Value);
    }

    public void DisposeBrowser()
    {
        Browser.Stop();
        Browser.Dispose();
    }
}

public sealed class CustomMenuHandler : IContextMenuHandler
{
    public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
    {
        model.Clear();
        model.AddItem((CefMenuCommand)26501, "Back");
        model.AddItem((CefMenuCommand)26502, "Forward");
        model.AddItem((CefMenuCommand)26503, "Reload");
        model.AddSeparator();
        model.AddItem((CefMenuCommand)26504, "Open DevTools");
        model.AddItem((CefMenuCommand)26505, "View Source");
    }

    public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
    {
        switch ((int)commandId)
        {
            case 26501:
                if (browser.CanGoBack) browser.GoBack();
                return true;
            case 26502:
                if (browser.CanGoForward) browser.GoForward();
                return true;
            case 26503:
                browser.Reload();
                return true;
            case 26504:
                chromiumWebBrowser.ShowDevTools();
                return true;
            case 26505:
                frame.ViewSource();
                return true;
            default:
                return false;
        }
    }

    public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
    {
    }

    public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
    {
        return false;
    }
}

public sealed class DownloadHandler : IDownloadHandler
{
    public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
    {
        return true;
    }

    public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
    {
        if (callback.IsDisposed)
            return false;

        var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var fileName = string.IsNullOrWhiteSpace(downloadItem.SuggestedFileName) ? "download.bin" : downloadItem.SuggestedFileName;
        var target = Path.Combine(downloadsPath, fileName);

        callback.Continue(target, true);
        return true;
    }

    public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
    }
}

public sealed class CustomKeyboardHandler : IKeyboardHandler
{
    public bool OnKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
    {
        return false;
    }

    public bool OnPreKeyEvent(IWebBrowser chromiumWebBrowser, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
    {
        isKeyboardShortcut = false;
        return false;
    }
}
