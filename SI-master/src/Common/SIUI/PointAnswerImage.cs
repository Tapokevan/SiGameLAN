using SIUI.ViewModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SIUI;

/// <summary>
/// Sends a normalized click on the image and draws the revealed answer.
/// </summary>
public sealed class PointAnswerImage : Image
{
    public static readonly DependencyProperty CorrectPointProperty = DependencyProperty.Register(
        nameof(CorrectPoint), typeof((double X, double Y)?), typeof(PointAnswerImage),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public (double X, double Y)? CorrectPoint
    {
        get => ((double X, double Y)?)GetValue(CorrectPointProperty);
        set => SetValue(CorrectPointProperty, value);
    }

    private TableInfoViewModel? GetTable()
    {
        for (DependencyObject? parent = this; parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is Table table)
            {
                return table.DataContext as TableInfoViewModel;
            }
        }

        return null;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var table = GetTable();
        if (table?.LayoutMode != LayoutMode.PointOnImage || !table.SelectAnswer.CanBeExecuted
            || !ReferenceEquals(DataContext, table.PointAnswerImage) || Source is null
            || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var position = e.GetPosition(this);
        var x = position.X / ActualWidth;
        var y = position.Y / ActualHeight;
        if (x is < 0 or > 1 || y is < 0 or > 1)
        {
            return;
        }

        e.Handled = true;
        table.SelectAnswer.Execute(new ItemViewModel { Label = FormattableString.Invariant($"{x:R},{y:R}") });
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var table = GetTable();
        if (CorrectPoint is not { } point || table?.PointAnswerImage is null
            || !ReferenceEquals(DataContext, table.PointAnswerImage))
        {
            return;
        }

        var center = new Point(point.X * ActualWidth, point.Y * ActualHeight);
        drawingContext.DrawEllipse(null, new Pen(Brushes.Black, 6), center, 10, 10);
        drawingContext.DrawEllipse(null, new Pen(Brushes.Orange, 3), center, 10, 10);
    }
}
