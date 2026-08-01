using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ExpenseManager.App.Converters;

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex && TryParseHex(hex, out var color))
            return new SolidColorBrush(color);
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static bool TryParseHex(string hex, out Color color)
    {
        color = Colors.Gray;
        hex = hex.TrimStart('#');
        if (hex.Length != 6 && hex.Length != 8) return false;

        try
        {
            byte a = 255, r, g, b;
            int offset = 0;
            if (hex.Length == 8)
            {
                a = System.Convert.ToByte(hex.Substring(0, 2), 16);
                offset = 2;
            }
            r = System.Convert.ToByte(hex.Substring(offset, 2), 16);
            g = System.Convert.ToByte(hex.Substring(offset + 2, 2), 16);
            b = System.Convert.ToByte(hex.Substring(offset + 4, 2), 16);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
