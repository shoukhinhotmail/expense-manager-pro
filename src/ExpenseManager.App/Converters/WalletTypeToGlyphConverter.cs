using ExpenseManager.Core.Entities;
using Microsoft.UI.Xaml.Data;

namespace ExpenseManager.App.Converters;

public class WalletTypeToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        WalletType.Cash => "",
        WalletType.Bank => "",
        WalletType.CreditCard => "",
        WalletType.MobileBanking => "",
        _ => ""
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
