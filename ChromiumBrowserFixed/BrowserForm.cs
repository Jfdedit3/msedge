using CefSharp;
using CefSharp.WinForms;
using System.Drawing;

namespace ChromiumBrowserFixed;

public sealed class BrowserForm : Form
{
    private readonly ToolStrip toolStrip;
    private readonly ToolStripButton backButton;
    private readonly ToolStripButton forwardButton;
    private readonly ToolStripButton refreshButton;
    private readonly ToolStripButton homeButton;
    private readonly ToolStripButton newTabButton;
    private readonly ToolStripButton closeTabButton;
    private readonly ToolStripButton devToolsButton;
    private readonly ToolStripTextBox addressBar;
    private readonly TabControl tabControl;
    private readonly string homeUrl = "https://www.google.com";

    public BrowserForm()
    {
        Text = "Chromium Browser Fixed";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 700);
        KeyPreview = true;

        toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        backButton = new ToolStripButton("←");
        forwardButton = new ToolStripButton("→");
        refreshButton = new ToolStripButton("⟳");
        homeButton = new ToolStripButton("⌂");
        newTabButton = new ToolStripButton("+");
        closeTabButton = new ToolStripButton("×");
        devToolsButton = new ToolStripButton("DevTools");
        addressBar = new ToolStripTextBox { AutoSize = false, Width = 850 };
        tabControl = new TabControl { Dock = DockStyle.Fill };

        backButton.Click += (_, _) => CurrentBrowser?.Back();
        forwardButton.Click += (_, _) => CurrentBrowser?.Forward();
        refreshButton.Click += (_, _) =>
        {
            if (CurrentBrowser is null) return;
            if (CurrentBrowser.IsLoading) CurrentBrowser.Stop(); else CurrentBrowser.Reload();
        };
        homeButton.Click += (_, _) => NavigateTo(homeUrl);
        newTabButton.Click += (_, _) => AddTab(homeUrl);
        closeTabButton.Click += (_, _) => CloseCurrentTab();
        devToolsButton.Click += (_, _) => CurrentBrowser?.ShowDevTools();
        addressBar.KeyDown += AddressBar_KeyDown;
        tabControl.SelectedIndexChanged += (_, _) => SyncUi();

        toolStrip.Items.Add(backButton);
        toolStrip.Items.Add(forwardButton);
        toolStrip.Items.Add(refreshButton);
        toolStrip.Items.Add(homeButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(addressBar);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(newTabButton);
        toolStrip.Items.Add(closeTabButton);
        toolStrip.Items.Add(devToolsButton);

        Controls.Add(tabControl);
        Controls.Add(toolStrip);
        toolStrip.Dock = DockStyle.Top;

        AddTab(homeUrl);
    }

    private ChromiumWebBrowser? CurrentBrowser => tabControl.SelectedTab?.Tag as ChromiumWebBrowser;

    private void AddTab(string url)
    {
        var browser = new ChromiumWebBrowser(url)
        {
            Dock = DockStyle.Fill
        };

        var page = new TabPage("New Tab")
        {
            Tag = browser
        };

        browser.TitleChanged += (_, e) =>
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                page.Text = Trim(string.IsNullOrWhiteSpace(e.NewValue) ? "New Tab" : e.NewValue, 24);
            }));
        };

        browser.AddressChanged += (_, e) =>
        {
            if (IsDisposed) return;
            if (tabControl.SelectedTab != page) return;
            BeginInvoke(new Action(() => addressBar.Text = e.Address));
        };

        browser.LoadingStateChanged += (_, _) =>
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(SyncUi));
        };

        page.Controls.Add(browser);
        tabControl.TabPages.Add(page);
        tabControl.SelectedTab = page;
        SyncUi();
    }

    private void CloseCurrentTab()
    {
        if (tabControl.SelectedTab is null)
            return;

        var page = tabControl.SelectedTab;
        if (page.Tag is ChromiumWebBrowser browser)
        {
            browser.Stop();
            browser.Dispose();
        }

        tabControl.TabPages.Remove(page);
        page.Dispose();

        if (tabControl.TabCount == 0)
            AddTab(homeUrl);

        SyncUi();
    }

    private void NavigateTo(string raw)
    {
        if (CurrentBrowser is null)
            return;

        CurrentBrowser.Load(NormalizeAddress(raw));
    }

    private void AddressBar_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            NavigateTo(addressBar.Text);
        }
    }

    private void SyncUi()
    {
        var browser = CurrentBrowser;
        if (browser is null)
            return;

        addressBar.Text = browser.Address ?? string.Empty;
        backButton.Enabled = browser.CanGoBack;
        forwardButton.Enabled = browser.CanGoForward;
        refreshButton.Text = browser.IsLoading ? "✕" : "⟳";
    }

    private static string NormalizeAddress(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "https://www.google.com";

        raw = raw.Trim();

        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (raw.Contains(' ') || !raw.Contains('.'))
            return $"https://www.google.com/search?q={Uri.EscapeDataString(raw)}";

        return $"https://{raw}";
    }

    private static string Trim(string text, int max)
    {
        if (text.Length <= max)
            return text;
        return text[..max] + "...";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.L))
        {
            addressBar.Focus();
            addressBar.SelectAll();
            return true;
        }

        if (keyData == (Keys.Control | Keys.T))
        {
            AddTab(homeUrl);
            return true;
        }

        if (keyData == (Keys.Control | Keys.W))
        {
            CloseCurrentTab();
            return true;
        }

        if (keyData == Keys.F5)
        {
            CurrentBrowser?.Reload();
            return true;
        }

        if (keyData == Keys.F12)
        {
            CurrentBrowser?.ShowDevTools();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
