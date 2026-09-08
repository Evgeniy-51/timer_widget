using System.Windows;

namespace TimerWidget.Services;

internal static class ThemeService
{
    public static void Apply(string theme)
    {
        var app = Application.Current;
        var uri = string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase)
            ? "Themes/Light.xaml"
            : "Themes/Dark.xaml";

        var skin = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };
        var controls = new ResourceDictionary { Source = new Uri("Themes/Controls.xaml", UriKind.Relative) };

        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(skin);
        app.Resources.MergedDictionaries.Add(controls);
        Loc.Remerge();
    }
}
