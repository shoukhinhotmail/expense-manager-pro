using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ExpenseManager.App.Converters;

/// <summary>Same color as HexToBrushConverter but at low opacity — used for the soft
/// tinted background behind a category icon glyph.</summary>
public class HexToTintBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex && TryParseHex(hex, out var color))
            return new SolidColorBrush(Color.FromArgb(38, color.R, color.G, color.B));
        return new SolidColorBrush(Color.FromArgb(38, 128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static bool TryParseHex(string hex, out Color color)
    {
        color = Colors.Gray;
        hex = hex.TrimStart('#');
        if (hex.Length < 6) return false;

        try
        {
            var r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            var g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            var b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            color = Color.FromArgb(255, r, g, b);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
