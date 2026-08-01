using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is not true;
}
