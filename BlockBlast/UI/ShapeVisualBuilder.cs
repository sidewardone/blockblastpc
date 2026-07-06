using System.Windows.Controls;
using BlockBlast.Core;

namespace BlockBlast.UI;

public static class ShapeVisualBuilder
{
    public static Canvas Build(Shape shape, double cellSize, double gap)
    {
        var canvas = new Canvas
        {
            Width = shape.Width * cellSize,
            Height = shape.Height * cellSize,
        };

        foreach (var cell in shape.Cells)
        {
            var block = BlockVisualFactory.CreateBlock(shape.Color, cellSize - gap);
            Canvas.SetLeft(block, cell.Col * cellSize + gap / 2);
            Canvas.SetTop(block, cell.Row * cellSize + gap / 2);
            canvas.Children.Add(block);
        }

        return canvas;
    }
}
