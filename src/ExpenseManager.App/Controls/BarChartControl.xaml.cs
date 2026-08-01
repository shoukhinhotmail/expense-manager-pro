using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace ExpenseManager.App.Controls;

public sealed partial class BarChartControl : UserControl
{
    public static readonly DependencyProperty IncomeValueProperty = DependencyProperty.Register(
        nameof(IncomeValue), typeof(decimal), typeof(BarChartControl),
        new PropertyMetadata(0m, OnValueChanged));

    public static readonly DependencyProperty ExpenseValueProperty = DependencyProperty.Register(
        nameof(ExpenseValue), typeof(decimal), typeof(BarChartControl),
        new PropertyMetadata(0m, OnValueChanged));

    public decimal IncomeValue
    {
        get => (decimal)GetValue(IncomeValueProperty);
        set => SetValue(IncomeValueProperty, value);
    }

    public decimal ExpenseValue
    {
        get => (decimal)GetValue(ExpenseValueProperty);
        set => SetValue(ExpenseValueProperty, value);
    }

    public BarChartControl()
    {
        InitializeComponent();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((BarChartControl)d).Redraw();

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        ChartCanvas.Children.Clear();

        var width = ChartCanvas.ActualWidth;
        var height = ChartCanvas.ActualHeight;
        if (width <= 0 || height <= 0) return;

        var maxValue = (double)Math.Max(Math.Max(IncomeValue, ExpenseValue), 1m);
        const double barWidth = 48;
        const double gap = 40;
        const double labelHeight = 24;
        var plotHeight = height - labelHeight;

        DrawBar(gap, plotHeight, barWidth, (double)IncomeValue / maxValue * plotHeight, Color.FromArgb(255, 0x22, 0xC5, 0x5E), "Income");
        DrawBar(gap * 2 + barWidth, plotHeight, barWidth, (double)ExpenseValue / maxValue * plotHeight, Color.FromArgb(255, 0xEF, 0x44, 0x44), "Expense");
    }

    private void DrawBar(double x, double plotHeight, double barWidth, double barHeight, Color color, string label)
    {
        barHeight = Math.Max(barHeight, 2);

        var rect = new Rectangle
        {
            Width = barWidth,
            Height = barHeight,
            Fill = new SolidColorBrush(color),
            RadiusX = 4,
            RadiusY = 4
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, plotHeight - barHeight);
        ChartCanvas.Children.Add(rect);

        var text = new TextBlock { Text = label, Opacity = 0.7 };
        Canvas.SetLeft(text, x);
        Canvas.SetTop(text, plotHeight + 4);
        ChartCanvas.Children.Add(text);
    }
}
