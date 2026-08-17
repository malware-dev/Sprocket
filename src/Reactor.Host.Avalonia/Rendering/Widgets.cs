using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace Reactor.Host.Rendering;

/// <summary>Small building blocks shared by the renderers.</summary>
static class Widgets
{
    public static TextBlock Label(string? text, IBrush brush, double size = 13, bool bold = false) => new()
    {
        Text = text ?? "",
        Foreground = brush,
        FontSize = size,
        FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center
    };

    /// <summary>A state light. Returns the lit half so it can be flipped later.</summary>
    public static Panel Dot(out Ellipse lit)
    {
        lit = new Ellipse { Fill = Palette.Live, IsVisible = false };
        var panel = new Panel
        {
            Width = 8,
            Height = 8,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(new Ellipse { Fill = Palette.Dim });
        panel.Children.Add(lit);
        return panel;
    }

    /// <summary>A row that drills into something.</summary>
    public static Button Row(Control content) => new()
    {
        Content = content,
        Background = Palette.Panel,
        Foreground = Palette.Ink,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(12, 9),
        CornerRadius = new CornerRadius(6),
        Margin = new Thickness(0, 0, 0, 4),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Stretch
    };

    /// <summary>
    /// A fillable part of a sentence. The only thing on screen that changes what a
    /// sequence means, so nothing else may borrow this look.
    /// </summary>
    public static Button Slot(string? text) => new()
    {
        Content = text ?? "",
        Background = Palette.SlotFill,
        Foreground = Palette.Ink,
        BorderBrush = Palette.SlotEdge,
        BorderThickness = new Thickness(0, 0, 0, 2),
        Padding = new Thickness(9, 4),
        CornerRadius = new CornerRadius(4),
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(0, 0, 6, 4)
    };

    public static Button Ghost(string text) => new()
    {
        Content = text,
        Background = Brushes.Transparent,
        Foreground = Palette.Muted,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(8, 3),
        CornerRadius = new CornerRadius(4),
        FontSize = 12
    };

    public static Button Chip(string text, bool on) => new()
    {
        Content = text,
        Background = on ? Palette.Accent : Palette.Panel,
        Foreground = on ? Brushes.White : Palette.Muted,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(10, 3),
        CornerRadius = new CornerRadius(12),
        FontSize = 12,
        Margin = new Thickness(0, 0, 6, 6)
    };

    public static Border Wash(Control content, IBrush background) => new()
    {
        Child = content,
        Background = background,
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(10, 6)
    };

    public static StackPanel Stack(Orientation orientation = Orientation.Vertical) => new()
    {
        Orientation = orientation
    };
}
