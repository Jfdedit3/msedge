using CefSharp;
using CefSharp.Handler;

namespace ChromiumBrowserFixed;

internal sealed class AdBlockRequestHandler : RequestHandler
{
    private static readonly AdBlockResourceRequestHandler ResourceRequestHandler = new();

    protected override IResourceRequestHandler? GetResourceRequestHandler(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        bool isNavigation,
        bool isDownload,
        string requestInitiator,
        ref bool disableDefaultHandling)
    {
        return ResourceRequestHandler;
    }
}

internal sealed class AdBlockResourceRequestHandler : ResourceRequestHandler
{
    protected override CefReturnValue OnBeforeResourceLoad(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        IRequestCallback callback)
    {
        return AdBlockPolicy.ShouldBlock(request)
            ? CefReturnValue.Cancel
            : CefReturnValue.Continue;
    }
}

internal static class AdBlockPolicy
{
    private static readonly HashSet<string> BlockedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "doubleclick.net",
        "googlesyndication.com",
        "googleadservices.com",
        "googletagmanager.com",
        "googletagservices.com",
        "adservice.google.com",
        "pagead2.googlesyndication.com",
        "securepubads.g.doubleclick.net",
        "adnxs.com",
        "adsrvr.org",
        "taboola.com",
        "outbrain.com",
        "criteo.com",
        "criteo.net",
        "amazon-adsystem.com",
        "ads.yahoo.com",
        "zedo.com",
        "scorecardresearch.com",
        "hotjar.com"
    };

    private static readonly string[] BlockedPathFragments =
    {
        "/ads/",
        "/ads?",
        "/advert",
        "/banner",
        "prebid",
        "googlesyndication",
        "doubleclick",
        "adservice",
        "analytics",
        "pixel"
    };

    public static bool ShouldBlock(IRequest request)
    {
        if (request.ResourceType == ResourceType.MainFrame)
        {
            return false;
        }

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsBlockedHost(uri.Host))
        {
            return true;
        }

        var target = uri.PathAndQuery;
        return BlockedPathFragments.Any(fragment =>
            target.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBlockedHost(string host)
    {
        return BlockedHosts.Any(blockedHost =>
            host.Equals(blockedHost, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith($".{blockedHost}", StringComparison.OrdinalIgnoreCase));
    }
}
