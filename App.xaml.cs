using System.Windows;
using TimerWidget.Models;
using TimerWidget.Services;

namespace TimerWidget;

public partial class App : Application
{
    public AppSettings Settings { get; private set; } = null!;
    internal TimerEngine Engine { get; private set; } = null!;
    public new static App Current => (App)Application.Current;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        if (!SingleInstance.TryOwn())
        {
            SingleInstance.RequestActivate();
            Shutdown();
            return;
        }

        Settings = SettingsStore.Load();
        Settings.Autostart = AutostartService.IsEnabled();
        Loc.Set(Settings.Language);
        ThemeService.Apply(Settings.Theme);
        Engine = new TimerEngine(Settings);
        var window = new MainWindow();
        MainWindow = window;
        SingleInstance.Listen(() => window.Restore());
        window.Show();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        if (Settings != null)
            SettingsStore.Save(Settings);
        SingleInstance.Release();
    }
}
