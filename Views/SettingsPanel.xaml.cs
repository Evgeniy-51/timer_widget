using System.Windows;
using System.Windows.Controls;
using TimerWidget.Models;
using TimerWidget.Services;

namespace TimerWidget.Views;

public partial class SettingsPanel : UserControl
{
    private bool _syncing;

    public SettingsPanel()
    {
        InitializeComponent();
    }

    public AppSettings Settings { get; set; } = null!;
    public Action? Persist { get; set; }
    public Action<string>? ThemeChangedByUser { get; set; }
    public Action<string>? SizeChangedByUser { get; set; }
    public Action<bool>? SecondsChangedByUser { get; set; }
    public Action<bool>? SoundChangedByUser { get; set; }

    public void SyncFromSettings()
    {
        _syncing = true;
        var lang = Loc.Resolve(Settings.Language);
        LangRu.IsChecked = lang == Loc.Ru;
        LangEn.IsChecked = lang == Loc.En;
        DarkBtn.IsChecked = !string.Equals(Settings.Theme, "light", StringComparison.OrdinalIgnoreCase);
        LightBtn.IsChecked = !DarkBtn.IsChecked;
        SizeS.IsChecked = Settings.SizeId == "S";
        SizeM.IsChecked = Settings.SizeId == "M";
        SizeL.IsChecked = Settings.SizeId == "L";
        SizeXl.IsChecked = Settings.SizeId == "XL";
        if (SizeS.IsChecked != true && SizeM.IsChecked != true && SizeL.IsChecked != true && SizeXl.IsChecked != true)
            SizeM.IsChecked = true;
        SecondsChk.IsChecked = Settings.ShowSeconds;
        SoundChk.IsChecked = Settings.SoundEnabled;
        AutostartChk.IsChecked = AutostartService.IsEnabled();
        Settings.Autostart = AutostartChk.IsChecked == true;
        _syncing = false;
    }

    private void LangChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || sender is not RadioButton { IsChecked: true, Tag: string id }) return;
        Settings.Language = id;
        Loc.Set(id);
        Persist?.Invoke();
    }

    private void ThemeChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        var theme = LightBtn.IsChecked == true ? "light" : "dark";
        Settings.Theme = theme;
        ThemeService.Apply(theme);
        Persist?.Invoke();
        ThemeChangedByUser?.Invoke(theme);
    }

    private void OnSizePicked(object sender, RoutedEventArgs e)
    {
        if (_syncing || sender is not RadioButton { IsChecked: true, Tag: string id }) return;
        Settings.SizeId = id;
        Persist?.Invoke();
        SizeChangedByUser?.Invoke(id);
    }

    private void SecondsChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        Settings.ShowSeconds = SecondsChk.IsChecked == true;
        Persist?.Invoke();
        SecondsChangedByUser?.Invoke(Settings.ShowSeconds);
    }

    private void SoundChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        Settings.SoundEnabled = SoundChk.IsChecked == true;
        Persist?.Invoke();
        SoundChangedByUser?.Invoke(Settings.SoundEnabled);
    }

    private void AutostartChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        var on = AutostartChk.IsChecked == true;
        AutostartService.SetEnabled(on);
        Settings.Autostart = on;
        Persist?.Invoke();
    }
}
