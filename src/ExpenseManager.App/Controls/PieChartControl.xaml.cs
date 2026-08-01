using System.Collections;
using System.Collections.Specialized;
using ExpenseManager.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using Windows.Foundation;
using Windows.UI;

namespace ExpenseManager.App.Controls;

public sealed partial class PieChartControl : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(PieChartControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public PieChartControl()
    {
        InitializeComponent();
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PieChartControl)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
            oldIncc.CollectionChanged -= control.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newIncc)
            newIncc.CollectionChanged += control.OnCollectionChanged;

        control.Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        ChartCanvas.Children.Clear();

        var width = ChartCanvas.ActualWidth;
        var height = ChartCanvas.ActualHeight;
        if (width <= 0 || height <= 0 || ItemsSource is null) return;

        var items = ItemsSource.Cast<CategoryTotal>().Where(i => i.Total > 0).ToList();
        var total = items.Sum(i => i.Total);
        if (items.Count == 0 || total <= 0)
        {
            var placeholder = new TextBlock
            {
                Text = "No data yet",
                Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(placeholder, width / 2 - 35);
            Canvas.SetTop(placeholder, height / 2 - 10);
            ChartCanvas.Children.Add(placeholder);
            return;
        }

        var size = Math.Min(width, height);
        var radius = size / 2 - 4;
        var center = new Point(width / 2, height / 2);

        double startAngle = 0;
        foreach (var item in items)
        {
            var sweep = (double)(item.Total / total) * 360.0;
            var endAngle = startAngle + sweep;

            var path = BuildSlice(center, radius, startAngle, endAngle, item.Total == total);
            path.Fill = new SolidColorBrush(ParseHexColor(item.Color));
            ChartCanvas.Children.Add(path);

            startAngle = endAngle;
        }
    }

    private static Path BuildSlice(Point center, double radius, double startAngle, double endAngle, bool isFullCircle)
    {
        if (isFullCircle)
        {
            var ellipse = new EllipseGeometry { Center = center, RadiusX = radius, RadiusY = radius };
            return new Path { Data = ellipse };
        }

        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, endAngle);
        var isLargeArc = (endAngle - startAngle) > 180.0;

        var figure = new PathFigure { StartPoint = center, IsClosed = true };
        figure.Segments.Add(new LineSegment { Point = start });
        figure.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new Size(radius, radius),
            IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Clockwise
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return new Path { Data = geometry };
    }

    private static Point PointOnCircle(Point center, double radius, double angleDegrees)
    {
        var radians = (Math.PI / 180.0) * (angleDegrees - 90);
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        try
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }
        catch
        {
            return Colors.Gray;
        }
    }
}
