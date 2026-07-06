using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using BlockBlast.Core;

namespace BlockBlast.UI;

public static class BlockVisualFactory
{
    public static Border CreateBlock(RgbColor color, double size)
    {
        var baseColor = Color.FromRgb(color.R, color.G, color.B);
        var light = Lighten(baseColor, 0.4);
        var dark = Darken(baseColor, 0.2);

        var gradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        gradient.GradientStops.Add(new GradientStop(light, 0));
        gradient.GradientStops.Add(new GradientStop(baseColor, 0.55));
        gradient.GradientStops.Add(new GradientStop(dark, 1));

        var border = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size * 0.22),
            Background = gradient,
            BorderBrush = new SolidColorBrush(Darken(baseColor, 0.32)),
            BorderThickness = new Thickness(Math.Max(1.2, size * 0.045)),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
        };

        var innerHighlight = new Border
        {
            CornerRadius = new CornerRadius(size * 0.16),
            Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
            Margin = new Thickness(size * 0.12, size * 0.1, size * 0.35, size * 0.55),
        };
        border.Child = innerHighlight;

        return border;
    }

    public static Color Lighten(Color c, double amount)
    {
        return Color.FromRgb(
            (byte)Math.Min(255, c.R + (255 - c.R) * amount),
            (byte)Math.Min(255, c.G + (255 - c.G) * amount),
            (byte)Math.Min(255, c.B + (255 - c.B) * amount));
    }

    public static Color Darken(Color c, double amount)
    {
        return Color.FromRgb(
            (byte)(c.R * (1 - amount)),
            (byte)(c.G * (1 - amount)),
            (byte)(c.B * (1 - amount)));
    }
}
