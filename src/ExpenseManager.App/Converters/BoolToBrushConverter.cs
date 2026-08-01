using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ExpenseManager.App.Converters;

/// <summary>ConverterParameter is "ResourceKeyWhenTrue|ResourceKeyWhenFalse", e.g.
/// "AppWarningBrush|AppAccentBrush" — resolved against the app's resource dictionary so it
/// stays theme-aware.</summary>
public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var keys = (parameter as string)?.Split('|');
        if (keys is not { Length: 2 }) return new SolidColorBrush(Microsoft.UI.Colors.Gray);

        var isTrue = value is true;
        var key = isTrue ? keys[0] : keys[1];
        return Application.Current.Resources.TryGetValue(key, out var resource) && resource is Brush brush
            ? brush
            : new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
