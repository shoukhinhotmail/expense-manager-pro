using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

public class NullableDateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is DateTime date ? $"Target: {date:MMM d, yyyy}" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
