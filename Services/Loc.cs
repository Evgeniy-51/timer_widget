using System.Globalization;
using System.Windows;

namespace TimerWidget.Services;

internal static class Loc
{
    public const string Ru = "ru";
    public const string En = "en";

    public static string Current { get; private set; } = Ru;

    public static event Action? Changed;

    public static string Resolve(string? stored)
    {
        if (string.Equals(stored, En, StringComparison.OrdinalIgnoreCase))
            return En;
        if (string.Equals(stored, Ru, StringComparison.OrdinalIgnoreCase))
            return Ru;
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? Ru
            : En;
    }

    public static void Set(string? stored)
    {
        Current = Resolve(stored);
        Remerge();
        Changed?.Invoke();
    }

    public static void Remerge()
    {
        var app = Application.Current;
        if (app == null)
            return;

        var merged = app.Resources.MergedDictionaries;
        for (var i = merged.Count - 1; i >= 0; i--)
        {
            var src = merged[i].Source?.OriginalString ?? "";
            if (src.Contains("Strings.", StringComparison.OrdinalIgnoreCase))
                merged.RemoveAt(i);
        }

        merged.Add(new ResourceDictionary
        {
            Source = new Uri($"Resources/Strings.{Current}.xaml", UriKind.Relative)
        });
    }

    public static string Get(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;
}
