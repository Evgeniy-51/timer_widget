using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace TimerWidget.Services;

internal sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Window _window;
    private readonly ToolStripMenuItem _show;
    private readonly ToolStripMenuItem _start;
    private readonly ToolStripMenuItem _pause;
    private readonly ToolStripMenuItem _exit;

    public event Action? StartRequested;
    public event Action? PauseRequested;

    public TrayService(Window window)
    {
        _window = window;
        _icon = new NotifyIcon
        {
            Visible = true,
            Text = "Timer Widget",
            Icon = LoadIcon()
        };

        _show = new ToolStripMenuItem("", null, (_, _) => ShowWindow());
        _start = new ToolStripMenuItem("", null, (_, _) => StartRequested?.Invoke());
        _pause = new ToolStripMenuItem("", null, (_, _) => PauseRequested?.Invoke());
        _exit = new ToolStripMenuItem("", null, (_, _) => Application.Current.Shutdown());

        var menu = new ContextMenuStrip();
        menu.Items.Add(_show);
        menu.Items.Add(_start);
        menu.Items.Add(_pause);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exit);
        _icon.ContextMenuStrip = menu;
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowWindow();
        };

        Loc.Changed += ApplyStrings;
        ApplyStrings();
    }

    public void ShowWindow()
    {
        if (_window is MainWindow main)
            main.Restore();
        else
        {
            _window.Show();
            WindowPlacement.ClampVisible(_window);
        }
    }

    public void Dispose()
    {
        Loc.Changed -= ApplyStrings;
        _icon.Visible = false;
        _icon.Dispose();
    }

    private void ApplyStrings()
    {
        _show.Text = Loc.Get("TrayShow");
        _start.Text = Loc.Get("TrayStart");
        _pause.Text = Loc.Get("TrayPause");
        _exit.Text = Loc.Get("TrayExit");
    }

    private static Icon LoadIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
        var streamInfo = Application.GetResourceStream(uri);
        if (streamInfo != null)
            return new Icon(streamInfo.Stream);

        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(path))
            return new Icon(path);

        return SystemIcons.Application;
    }
}
