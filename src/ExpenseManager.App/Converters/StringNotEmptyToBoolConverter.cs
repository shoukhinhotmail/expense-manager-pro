using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

/// <summary>True when the bound string is non-empty. Pass ConverterParameter="invert" to flip the result.</summary>
public class StringNotEmptyToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hasValue = value is string s && !string.IsNullOrWhiteSpace(s);
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);
        return invert ? !hasValue : hasValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
