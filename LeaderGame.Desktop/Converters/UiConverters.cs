using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LeaderGame.Desktop.Converters;

public sealed class CategoryBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.ToUpperInvariant() ?? string.Empty;

        return text switch
        {
            "ECONOMY" => Brush.Parse("#4FAE78"),
            "MILITARY" => Brush.Parse("#D96C6C"),
            "POLITICS" => Brush.Parse("#A77BD7"),
            "DIPLOMACY" => Brush.Parse("#5E9DD9"),
            "PERSONAL" => Brush.Parse("#B778C7"),
            "ORDER" => Brush.Parse("#D8A657"),
            "ADVISER REPORT" => Brush.Parse("#55B8C7"),
            "SYSTEM" => Brush.Parse("#7F8A98"),
            "BRIEFING" => Brush.Parse("#55B8C7"),
            _ => Brush.Parse("#7F8A98")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}

public sealed class MetricAccentBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var label = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

        if (label.Contains("army"))
            return Brush.Parse("#D96C6C");

        if (label.Contains("treasury") || label.Contains("debt"))
            return Brush.Parse("#4FAE78");

        if (label.Contains("stability") || label.Contains("backing"))
            return Brush.Parse("#A77BD7");

        return Brush.Parse("#55B8C7");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}

public sealed class RiskBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

        return text switch
        {
            "severe" => Brush.Parse("#F06D6D"),
            "high" => Brush.Parse("#E58A61"),
            "moderate" => Brush.Parse("#D8A657"),
            "some" => Brush.Parse("#70A3D9"),
            "low" => Brush.Parse("#63B887"),
            _ => Brush.Parse("#9AA6B4")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}

public sealed class SemanticStatusBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

        if (text.Contains("hostile") ||
            text.Contains("very low") ||
            text.Contains("very weak") ||
            text.Contains("reluctant"))
        {
            return Brush.Parse("#D96C6C");
        }

        if (text.Contains("uncertain") ||
            text.Contains("mixed") ||
            text.Contains("weak"))
        {
            return Brush.Parse("#D8A657");
        }

        if (text.Contains("very high") ||
            text.Contains("exceptional") ||
            text.Contains("strong") ||
            text.Contains("very likely") ||
            text.Contains("likely") ||
            text.Contains("solid"))
        {
            return Brush.Parse("#63B887");
        }

        return Brush.Parse("#AEB8C5");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}

public sealed class StaleBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? Brush.Parse("#D8A657")
            : Brush.Parse("#3A4654");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}

public sealed class AttentionBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? Brush.Parse("#211B14")
            : Brush.Parse("#121922");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}

public sealed class BoolOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 1.0 : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        AvaloniaProperty.UnsetValue;
}
