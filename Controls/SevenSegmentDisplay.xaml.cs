using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimerWidget.Controls;

public partial class SevenSegmentDisplay : UserControl
{
    public SevenSegmentDisplay()
    {
        InitializeComponent();
        Loaded += (_, _) => PushAll();
    }

    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
        nameof(Time), typeof(TimeSpan), typeof(SevenSegmentDisplay),
        new PropertyMetadata(TimeSpan.Zero, (d, _) => ((SevenSegmentDisplay)d).PushTime()));

    public static readonly DependencyProperty ShowSecondsProperty = DependencyProperty.Register(
        nameof(ShowSeconds), typeof(bool), typeof(SevenSegmentDisplay),
        new PropertyMetadata(false, (d, _) =>
        {
            var c = (SevenSegmentDisplay)d;
            c.PushSeconds();
            c.PushTime();
        }));

    public static readonly DependencyProperty RoundUpPartialMinuteProperty = DependencyProperty.Register(
        nameof(RoundUpPartialMinute), typeof(bool), typeof(SevenSegmentDisplay),
        new PropertyMetadata(true, (d, _) => ((SevenSegmentDisplay)d).PushTime()));

    public static readonly DependencyProperty ColonOnProperty = DependencyProperty.Register(
        nameof(ColonOn), typeof(bool), typeof(SevenSegmentDisplay),
        new PropertyMetadata(true, (d, _) => ((SevenSegmentDisplay)d).PushColon()));

    public static readonly DependencyProperty OnBrushProperty = DependencyProperty.Register(
        nameof(OnBrush), typeof(Brush), typeof(SevenSegmentDisplay),
        new PropertyMetadata(Brushes.White, (d, _) => ((SevenSegmentDisplay)d).PushBrushes()));

    public static readonly DependencyProperty OffBrushProperty = DependencyProperty.Register(
        nameof(OffBrush), typeof(Brush), typeof(SevenSegmentDisplay),
        new PropertyMetadata(Brushes.Gray, (d, _) => ((SevenSegmentDisplay)d).PushBrushes()));

    public TimeSpan Time
    {
        get => (TimeSpan)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public bool ShowSeconds
    {
        get => (bool)GetValue(ShowSecondsProperty);
        set => SetValue(ShowSecondsProperty, value);
    }

    public bool RoundUpPartialMinute
    {
        get => (bool)GetValue(RoundUpPartialMinuteProperty);
        set => SetValue(RoundUpPartialMinuteProperty, value);
    }

    public bool ColonOn
    {
        get => (bool)GetValue(ColonOnProperty);
        set => SetValue(ColonOnProperty, value);
    }

    public Brush OnBrush
    {
        get => (Brush)GetValue(OnBrushProperty);
        set => SetValue(OnBrushProperty, value);
    }

    public Brush OffBrush
    {
        get => (Brush)GetValue(OffBrushProperty);
        set => SetValue(OffBrushProperty, value);
    }

    private void PushAll()
    {
        PushBrushes();
        PushTime();
        PushColon();
        PushSeconds();
    }

    private void PushTime()
    {
        if (H1 == null) return;
        var t = VisualTime();
        H1.Value = t.Hours / 10;
        H0.Value = t.Hours % 10;
        M1.Value = t.Minutes / 10;
        M0.Value = t.Minutes % 10;
        S1.Value = t.Seconds / 10;
        S0.Value = t.Seconds % 10;
    }

    /// <summary>
    /// Без секунд: неполная минута остаётся на табло.
    /// Countdown — ceil (01:00 → 00:59 всё ещё 01:00). Count-up — floor.
    /// </summary>
    private TimeSpan VisualTime()
    {
        var t = Time < TimeSpan.Zero ? TimeSpan.Zero : Time;
        if (ShowSeconds)
            return t;
        if (t <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var minutes = t.TotalSeconds / 60.0;
        var whole = RoundUpPartialMinute
            ? (int)Math.Ceiling(minutes - 1e-9)
            : (int)Math.Floor(minutes + 1e-9);
        whole = Math.Clamp(whole, 0, 23 * 60 + 59);
        return TimeSpan.FromMinutes(whole);
    }

    private void PushColon()
    {
        if (C1 == null) return;
        C1.IsLit = ColonOn;
    }

    private void PushSeconds()
    {
        if (SecHost == null) return;
        SecHost.Visibility = ShowSeconds ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PushBrushes()
    {
        if (H1 == null) return;
        foreach (var d in new[] { H1, H0, M1, M0, S1, S0 })
        {
            d.OnBrush = OnBrush;
            d.OffBrush = OffBrush;
        }

        C1.OnBrush = OnBrush;
        C1.OffBrush = OffBrush;
    }
}
