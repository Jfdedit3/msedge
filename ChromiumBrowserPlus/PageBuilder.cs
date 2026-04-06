using System.Net;
using System.Text;

namespace ChromiumBrowserPlus;

internal static class PageBuilder
{
    public static string BuildDownloadsPage()
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(downloads);

        var files = new DirectoryInfo(downloads)
            .GetFiles()
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Take(200)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'><title>Downloads</title><style>body{font-family:Segoe UI,Arial;padding:24px;background:#111;color:#eee}a{color:#7ab7ff;text-decoration:none}a:hover{text-decoration:underline}.item{padding:12px 0;border-bottom:1px solid #333}.meta{color:#aaa;font-size:12px}</style></head><body>");
        sb.Append("<h1>Downloads</h1><p>Ctrl+J opens this page.</p>");

        foreach (var file in files)
        {
            sb.Append("<div class='item'>");
            sb.Append($"<div><a href='{new Uri(file.FullName)}'>{WebUtility.HtmlEncode(file.Name)}</a></div>");
            sb.Append($"<div class='meta'>{WebUtility.HtmlEncode(file.FullName)} • {file.Length / 1024.0:F1} KB • {file.LastWriteTime}</div>");
            sb.Append("</div>");
        }

        if (files.Count == 0)
            sb.Append("<p>No downloads found.</p>");

        sb.Append("</body></html>");
        return WriteTempPage("downloads", sb.ToString());
    }

    public static string BuildHistoryPage(IReadOnlyList<HistoryEntry> entries)
    {
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'><title>History</title><style>body{font-family:Segoe UI,Arial;padding:24px;background:#111;color:#eee}a{color:#7ab7ff;text-decoration:none}a:hover{text-decoration:underline}.item{padding:12px 0;border-bottom:1px solid #333}.meta{color:#aaa;font-size:12px}</style></head><body>");
        sb.Append("<h1>History</h1><p>Ctrl+H opens this page.</p>");

        foreach (var entry in entries.Take(500))
        {
            sb.Append("<div class='item'>");
            sb.Append($"<div><a href='{WebUtility.HtmlEncode(entry.Url)}'>{WebUtility.HtmlEncode(entry.Title)}</a></div>");
            sb.Append($"<div class='meta'>{WebUtility.HtmlEncode(entry.Url)} • {entry.VisitedAtUtc.ToLocalTime()}</div>");
            sb.Append("</div>");
        }

        if (entries.Count == 0)
            sb.Append("<p>No history yet.</p>");

        sb.Append("</body></html>");
        return WriteTempPage("history", sb.ToString());
    }

    public static string BuildExtensionsPage(string dataRoot)
    {
        var extensionsRoot = Path.Combine(dataRoot, "Extensions");
        Directory.CreateDirectory(extensionsRoot);
        var directories = Directory.GetDirectories(extensionsRoot).OrderBy(x => x).ToList();

        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'><title>Extensions</title><style>body{font-family:Segoe UI,Arial;padding:24px;background:#111;color:#eee}code{background:#222;padding:2px 6px;border-radius:4px}.item{padding:12px 0;border-bottom:1px solid #333}</style></head><body>");
        sb.Append("<h1>Extensions</h1>");
        sb.Append("<p>This build supports loading unpacked local extensions from:</p>");
        sb.Append($"<p><code>{WebUtility.HtmlEncode(extensionsRoot)}</code></p>");
        sb.Append("<p>Put each unpacked extension in its own subfolder, then restart the browser.</p>");
        sb.Append("<p>Direct Chrome Web Store installation is not implemented in this embedded build.</p>");

        foreach (var dir in directories)
        {
            sb.Append($"<div class='item'>{WebUtility.HtmlEncode(Path.GetFileName(dir))}<br><code>{WebUtility.HtmlEncode(dir)}</code></div>");
        }

        if (directories.Count == 0)
            sb.Append("<p>No local extensions detected.</p>");

        sb.Append("</body></html>");
        return WriteTempPage("extensions", sb.ToString());
    }

    private static string WriteTempPage(string prefix, string html)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}.html");
        File.WriteAllText(filePath, html, Encoding.UTF8);
        return new Uri(filePath).AbsoluteUri;
    }
}
