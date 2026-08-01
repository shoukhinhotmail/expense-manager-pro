using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

/// <summary>Visible when the bound value is non-null (works for nullable value types like
/// DateTime? as well as reference types). Pass ConverterParameter="invert" to flip.</summary>
public class NullableToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hasValue = value is not null;
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);
        if (invert) hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
