using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hasValue = value is string s && !string.IsNullOrWhiteSpace(s);
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
