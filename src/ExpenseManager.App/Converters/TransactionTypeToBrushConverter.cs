using ExpenseManager.Core.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ExpenseManager.App.Converters;

public class TransactionTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isIncome = value is TransactionType.Income;
        var key = isIncome ? "AppIncomeBrush" : "AppExpenseBrush";
        return (Brush)Application.Current.Resources[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
