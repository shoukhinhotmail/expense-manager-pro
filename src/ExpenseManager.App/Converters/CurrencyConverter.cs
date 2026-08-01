using ExpenseManager.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var amount = value switch
        {
            decimal d => d,
            double d2 => (decimal)d2,
            _ => 0m
        };
        var currencyService = App.Host.Services.GetRequiredService<CurrencyService>();
        return currencyService.Format(amount);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
