using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

/// <summary>Visible when the bound count is zero (an "empty state" message). Pass ConverterParameter="invert" to flip.</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isZero = value is int count && count == 0;
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);
        if (invert) isZero = !isZero;
        return isZero ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
