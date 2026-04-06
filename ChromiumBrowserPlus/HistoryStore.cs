using System.Text.Encodings.Web;
using System.Text.Json;

namespace ChromiumBrowserPlus;

public sealed record HistoryEntry(string Url, string Title, DateTime VisitedAtUtc);

public sealed class HistoryStore
{
    private readonly string filePath;
    private readonly List<HistoryEntry> entries = new();
    private readonly object sync = new();

    public HistoryStore(string dataRoot)
    {
        filePath = Path.Combine(dataRoot, "history.json");
        Load();
    }

    public void Add(string url, string title)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return;

        lock (sync)
        {
            entries.Add(new HistoryEntry(url, string.IsNullOrWhiteSpace(title) ? url : title, DateTime.UtcNow));

            if (entries.Count > 1000)
                entries.RemoveRange(0, entries.Count - 1000);

            Save();
        }
    }

    public IReadOnlyList<HistoryEntry> GetAll()
    {
        lock (sync)
        {
            return entries.OrderByDescending(x => x.VisitedAtUtc).ToList();
        }
    }

    private void Load()
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            var json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
            if (loaded is not null)
                entries.AddRange(loaded);
        }
        catch
        {
        }
    }

    private void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(filePath, JsonSerializer.Serialize(entries, options));
        }
        catch
        {
        }
    }
}
