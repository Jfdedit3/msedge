using CefSharp.WinForms;
using System.Diagnostics;

namespace ChromiumBrowser;

public sealed class BrowserForm : Form
{
    private readonly MenuStrip menuStrip;
    private readonly ToolStrip toolStrip;
    private readonly ToolStripButton backButton;
    private readonly ToolStripButton forwardButton;
    private readonly ToolStripButton refreshButton;
    private readonly ToolStripButton homeButton;
    private readonly ToolStripButton newTabButton;
    private readonly ToolStripButton devToolsButton;
    private readonly ToolStripButton menuButton;
    private readonly ToolStripTextBox addressBar;
    private readonly StatusStrip statusStrip;
    private readonly ToolStripStatusLabel statusLabel;
    private readonly TabControl tabControl;
    private readonly ContextMenuStrip browserMenu;
    private readonly string homeUrl = "https://www.google.com";
    private bool isFullscreen;
    private FormBorderStyle previousBorderStyle;
    private FormWindowState previousWindowState;
    private Rectangle previousBounds;

    public BrowserForm()
    {
        Text = "Chromium Browser";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 700);
        KeyPreview = true;

        menuStrip = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("New Tab", null, (_, _) => AddTab(homeUrl));
        fileMenu.DropDownItems.Add("Close Tab", null, (_, _) => CloseCurrentTab());
        fileMenu.DropDownItems.Add("Exit", null, (_, _) => Close());

        var viewMenu = new ToolStripMenuItem("View");
        viewMenu.DropDownItems.Add("Developer Tools", null, (_, _) => CurrentBrowser?.ShowDevTools());
        viewMenu.DropDownItems.Add("Zoom In", null, async (_, _) => await ChangeZoomAsync(0.5));
        viewMenu.DropDownItems.Add("Zoom Out", null, async (_, _) => await ChangeZoomAsync(-0.5));
        viewMenu.DropDownItems.Add("Reset Zoom", null, async (_, _) => await SetZoomAsync(0));
        viewMenu.DropDownItems.Add("Toggle Fullscreen", null, (_, _) => ToggleFullscreen());

        var helpMenu = new ToolStripMenuItem("Help");
        helpMenu.DropDownItems.Add("CEF Website", null, (_, _) => Process.Start(new ProcessStartInfo
        {
            FileName = "https://bitbucket.org/chromiumembedded/cef",
            UseShellExecute = true
        }));

        menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, viewMenu, helpMenu });

        toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(20, 20) };
        backButton = new ToolStripButton("←");
        forwardButton = new ToolStripButton("→");
        refreshButton = new ToolStripButton("⟳");
        homeButton = new ToolStripButton("⌂");
        newTabButton = new ToolStripButton("+");
        devToolsButton = new ToolStripButton("DevTools");
        menuButton = new ToolStripButton("⋮");
        addressBar = new ToolStripTextBox { AutoSize = false, Width = 780 };

        backButton.Click += (_, _) => CurrentBrowser?.Back();
        forwardButton.Click += (_, _) => CurrentBrowser?.Forward();
        refreshButton.Click += RefreshButton_Click;
        homeButton.Click += (_, _) => NavigateTo(homeUrl);
        newTabButton.Click += (_, _) => AddTab(homeUrl);
        devToolsButton.Click += (_, _) => CurrentBrowser?.ShowDevTools();
        menuButton.Click += (_, _) => browserMenu.Show(Cursor.Position);
        addressBar.KeyDown += AddressBar_KeyDown;

        toolStrip.Items.Add(backButton);
        toolStrip.Items.Add(forwardButton);
        toolStrip.Items.Add(refreshButton);
        toolStrip.Items.Add(homeButton);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(addressBar);
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(newTabButton);
        toolStrip.Items.Add(devToolsButton);
        toolStrip.Items.Add(menuButton);

        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel("Ready");
        statusStrip.Items.Add(statusLabel);

        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            Padding = new Point(20, 4)
        };
        tabControl.SelectedIndexChanged += (_, _) => SyncUiFromCurrentTab();
        tabControl.DrawItem += TabControl_DrawItem;
        tabControl.MouseDown += TabControl_MouseDown;

        browserMenu = new ContextMenuStrip();
        browserMenu.Items.Add("New Tab", null, (_, _) => AddTab(homeUrl));
        browserMenu.Items.Add("Duplicate Tab", null, (_, _) => AddTab(CurrentBrowser?.Address ?? homeUrl));
        browserMenu.Items.Add("Close Tab", null, (_, _) => CloseCurrentTab());
        browserMenu.Items.Add(new ToolStripSeparator());
        browserMenu.Items.Add("Back", null, (_, _) => CurrentBrowser?.Back());
        browserMenu.Items.Add("Forward", null, (_, _) => CurrentBrowser?.Forward());
        browserMenu.Items.Add("Reload", null, (_, _) => CurrentBrowser?.Reload());
        browserMenu.Items.Add(new ToolStripSeparator());
        browserMenu.Items.Add("Open DevTools", null, (_, _) => CurrentBrowser?.ShowDevTools());

        Controls.Add(tabControl);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        Controls.Add(menuStrip);

        MainMenuStrip = menuStrip;
        menuStrip.Dock = DockStyle.Top;
        toolStrip.Dock = DockStyle.Top;
        statusStrip.Dock = DockStyle.Bottom;

        AddTab(homeUrl);
    }

    private ChromiumWebBrowser? CurrentBrowser => tabControl.SelectedTab is BrowserTabPage page ? page.Browser : null;

    private void AddTab(string url)
    {
        var page = new BrowserTabPage(url);
        page.TitleChanged += (_, _) => UpdateTabTitle(page);
        page.AddressChanged += (_, address) =>
        {
            if (tabControl.SelectedTab == page)
                addressBar.Text = address;
        };
        page.LoadingStateChanged += (_, isLoading) =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => ApplyLoadingState(page, isLoading));
                return;
            }
            ApplyLoadingState(page, isLoading);
        };
        page.StatusMessageChanged += (_, message) =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => statusLabel.Text = string.IsNullOrWhiteSpace(message) ? "Ready" : message);
                return;
            }
            statusLabel.Text = string.IsNullOrWhiteSpace(message) ? "Ready" : message;
        };

        tabControl.TabPages.Add(page);
        tabControl.SelectedTab = page;
        UpdateTabTitle(page);
        SyncUiFromCurrentTab();
    }

    private void ApplyLoadingState(BrowserTabPage page, bool isLoading)
    {
        if (tabControl.SelectedTab == page)
        {
            refreshButton.Text = isLoading ? "✕" : "⟳";
            backButton.Enabled = page.Browser.CanGoBack;
            forwardButton.Enabled = page.Browser.CanGoForward;
        }

        UpdateTabTitle(page, isLoading);
    }

    private void RefreshButton_Click(object? sender, EventArgs e)
    {
        if (CurrentBrowser is null)
            return;

        if (CurrentBrowser.IsLoading)
            CurrentBrowser.Stop();
        else
            CurrentBrowser.Reload();
    }

    private void UpdateTabTitle(BrowserTabPage page, bool? forceLoading = null)
    {
        var title = string.IsNullOrWhiteSpace(page.PageTitle) ? "New Tab" : page.PageTitle;
        var loading = forceLoading ?? page.IsLoading;
        page.Text = loading ? $"● {Trim(title, 24)}" : Trim(title, 24);
    }

    private void NavigateTo(string raw)
    {
        if (CurrentBrowser is null)
            return;

        CurrentBrowser.Load(NormalizeAddress(raw));
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

    private void AddressBar_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            NavigateTo(addressBar.Text);
        }
    }

    private void SyncUiFromCurrentTab()
    {
        var browser = CurrentBrowser;
        if (browser is null)
            return;

        addressBar.Text = browser.Address ?? string.Empty;
        backButton.Enabled = browser.CanGoBack;
        forwardButton.Enabled = browser.CanGoForward;
        refreshButton.Text = browser.IsLoading ? "✕" : "⟳";
        statusLabel.Text = "Ready";
    }

    private void CloseCurrentTab()
    {
        if (tabControl.SelectedTab is not BrowserTabPage page)
            return;

        page.DisposeBrowser();
        tabControl.TabPages.Remove(page);
        page.Dispose();

        if (tabControl.TabCount == 0)
            AddTab(homeUrl);
    }

    private async Task ChangeZoomAsync(double delta)
    {
        if (CurrentBrowser is null)
            return;

        var level = await CurrentBrowser.GetZoomLevelAsync();
        await CurrentBrowser.SetZoomLevelAsync(level + delta);
    }

    private async Task SetZoomAsync(double value)
    {
        if (CurrentBrowser is null)
            return;

        await CurrentBrowser.SetZoomLevelAsync(value);
    }

    private void ToggleFullscreen()
    {
        if (!isFullscreen)
        {
            previousBorderStyle = FormBorderStyle;
            previousWindowState = WindowState;
            previousBounds = Bounds;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Bounds = Screen.FromHandle(Handle).Bounds;
            isFullscreen = true;
            return;
        }

        FormBorderStyle = previousBorderStyle;
        WindowState = previousWindowState;
        Bounds = previousBounds;
        isFullscreen = false;
    }

    private static string Trim(string text, int max)
    {
        if (text.Length <= max)
            return text;
        return text[..max] + "...";
    }

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= tabControl.TabPages.Count)
            return;

        var tab = tabControl.TabPages[e.Index];
        var rect = e.Bounds;
        var closeRect = new Rectangle(rect.Right - 18, rect.Top + 6, 12, 12);

        using var textBrush = new SolidBrush(Color.Black);
        e.Graphics.DrawString(tab.Text, Font, textBrush, rect.X + 8, rect.Y + 6);
        e.Graphics.DrawString("x", Font, textBrush, closeRect.X, closeRect.Y - 1);
        e.DrawFocusRectangle();
    }

    private void TabControl_MouseDown(object? sender, MouseEventArgs e)
    {
        for (int i = 0; i < tabControl.TabPages.Count; i++)
        {
            var rect = tabControl.GetTabRect(i);
            var closeRect = new Rectangle(rect.Right - 20, rect.Top + 4, 16, 16);
            if (closeRect.Contains(e.Location))
            {
                if (tabControl.TabPages[i] is BrowserTabPage page)
                {
                    page.DisposeBrowser();
                    tabControl.TabPages.RemoveAt(i);
                    page.Dispose();
                    if (tabControl.TabCount == 0)
                        AddTab(homeUrl);
                }
                break;
            }
        }
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

        if (keyData == Keys.F11)
        {
            ToggleFullscreen();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
