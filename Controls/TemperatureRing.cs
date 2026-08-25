using System.Windows;
using System.Windows.Media;

namespace WatercoolerTemp.Controls;

public sealed class TemperatureRing : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(TemperatureRing),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IndicatorBrushProperty =
        DependencyProperty.Register(
            nameof(IndicatorBrush),
            typeof(Brush),
            typeof(TemperatureRing),
            new FrameworkPropertyMetadata(Brushes.LimeGreen, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Brush IndicatorBrush
    {
        get => (Brush)GetValue(IndicatorBrushProperty);
        set => SetValue(IndicatorBrushProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;
        double radius = Math.Max(0, Math.Min(ActualWidth, ActualHeight) / 2 - 18);
        double dotRadius = 2.2;
        int dotCount = 72;
        int activeDots = (int)Math.Round(Math.Clamp(Progress, 0, 1) * dotCount);

        for (int index = 0; index < dotCount; index++)
        {
            double angle = -Math.PI / 2 + index * (Math.PI * 2 / dotCount);
            double x = centerX + Math.Cos(angle) * radius;
            double y = centerY + Math.Sin(angle) * radius;
            bool isActive = index < activeDots;
            Brush brush = isActive ? IndicatorBrush : new SolidColorBrush(Color.FromArgb(38, 210, 220, 220));
            double currentRadius = isActive ? dotRadius : 1.6;

            drawingContext.DrawEllipse(brush, null, new Point(x, y), currentRadius, currentRadius);
        }
    }
}