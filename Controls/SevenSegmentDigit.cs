using System.Windows;
using System.Windows.Media;

namespace TimerWidget.Controls;

public class SevenSegmentDigit : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(SevenSegmentDigit),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OnBrushProperty = DependencyProperty.Register(
        nameof(OnBrush), typeof(Brush), typeof(SevenSegmentDigit),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OffBrushProperty = DependencyProperty.Register(
        nameof(OffBrush), typeof(Brush), typeof(SevenSegmentDigit),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly int[] Masks =
    [
        0b0111111, // 0
        0b0000110, // 1
        0b1011011, // 2
        0b1001111, // 3
        0b1100110, // 4
        0b1101101, // 5
        0b1111101, // 6
        0b0000111, // 7
        0b1111111, // 8
        0b1101111  // 9
    ];

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
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

    protected override Size MeasureOverride(Size availableSize)
    {
        var h = double.IsInfinity(availableSize.Height) ? 48 : availableSize.Height;
        var w = double.IsInfinity(availableSize.Width) ? h * 0.62 : availableSize.Width;
        if (double.IsNaN(h) || h <= 0) h = 48;
        if (double.IsNaN(w) || w <= 0) w = h * 0.62;
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;

        var v = Math.Clamp(Value, 0, 9);
        var mask = Masks[v];
        for (var i = 0; i < 7; i++)
        {
            var brush = (mask & (1 << i)) != 0 ? OnBrush : OffBrush;
            if (brush == null || brush == Brushes.Transparent)
                continue;
            dc.DrawGeometry(brush, null, Segment(i, w, h));
        }
    }

    private static StreamGeometry Segment(int index, double w, double h)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            Point[] pts = index switch
            {
                0 => H(w, h, 0.06),          // A
                1 => Vr(w, h, 0.08, 0.48),   // B
                2 => Vr(w, h, 0.52, 0.92),   // C
                3 => H(w, h, 0.94),          // D
                4 => Vl(w, h, 0.52, 0.92),   // E
                5 => Vl(w, h, 0.08, 0.48),   // F
                _ => G(w, h)                 // G
            };
            ctx.BeginFigure(pts[0], true, true);
            ctx.PolyLineTo(pts.Skip(1).ToArray(), true, false);
        }

        g.Freeze();
        return g;
    }

    private static Point[] H(double w, double h, double y)
    {
        var t = h * 0.08;
        var x0 = w * 0.18;
        var x1 = w * 0.82;
        var yy = h * y;
        return
        [
            new Point(x0 + t * 0.4, yy),
            new Point(x1 - t * 0.4, yy),
            new Point(x1 - t, yy + t * (y < 0.5 ? 1 : -1)),
            new Point(x0 + t, yy + t * (y < 0.5 ? 1 : -1))
        ];
    }

    private static Point[] G(double w, double h)
    {
        var t = h * 0.07;
        var y = h * 0.50;
        var x0 = w * 0.20;
        var x1 = w * 0.80;
        return
        [
            new Point(x0, y),
            new Point(x0 + t, y - t),
            new Point(x1 - t, y - t),
            new Point(x1, y),
            new Point(x1 - t, y + t),
            new Point(x0 + t, y + t)
        ];
    }

    private static Point[] Vl(double w, double h, double y0, double y1)
    {
        var t = w * 0.14;
        var x = w * 0.14;
        var top = h * y0;
        var bot = h * y1;
        return
        [
            new Point(x, top + t * 0.3),
            new Point(x + t, top + t),
            new Point(x + t, bot - t),
            new Point(x, bot - t * 0.3),
            new Point(x - t * 0.35, bot - t),
            new Point(x - t * 0.35, top + t)
        ];
    }

    private static Point[] Vr(double w, double h, double y0, double y1)
    {
        var t = w * 0.14;
        var x = w * 0.86;
        var top = h * y0;
        var bot = h * y1;
        return
        [
            new Point(x, top + t * 0.3),
            new Point(x + t * 0.35, top + t),
            new Point(x + t * 0.35, bot - t),
            new Point(x, bot - t * 0.3),
            new Point(x - t, bot - t),
            new Point(x - t, top + t)
        ];
    }
}
