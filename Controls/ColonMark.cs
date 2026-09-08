using System.Windows;
using System.Windows.Media;

namespace TimerWidget.Controls;

public class ColonMark : System.Windows.Controls.Control
{
    public static readonly DependencyProperty OnBrushProperty = DependencyProperty.Register(
        nameof(OnBrush), typeof(Brush), typeof(ColonMark),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OffBrushProperty = DependencyProperty.Register(
        nameof(OffBrush), typeof(Brush), typeof(ColonMark),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsLitProperty = DependencyProperty.Register(
        nameof(IsLit), typeof(bool), typeof(ColonMark),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

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

    public bool IsLit
    {
        get => (bool)GetValue(IsLitProperty);
        set => SetValue(IsLitProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var h = double.IsInfinity(constraint.Height) ? 48 : constraint.Height;
        return new Size(Math.Max(h * 0.38, 10), h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;

        var brush = IsLit ? OnBrush : OffBrush;
        if (brush == null)
            return;

        var blobW = Math.Max(w * 0.34, 3.75);
        var blobH = Math.Max(h * 0.12, 4.5);
        var x = (w - blobW) / 2;
        var r = Math.Min(blobW, blobH) * 0.25;

        dc.DrawRoundedRectangle(brush, null, new Rect(x, h * 0.20, blobW, blobH), r, r);
        dc.DrawRoundedRectangle(brush, null, new Rect(x, h * 0.62, blobW, blobH), r, r);
    }
}
