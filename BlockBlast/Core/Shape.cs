using System.Collections.Generic;
using System.Linq;

namespace BlockBlast.Core;

public sealed class Shape
{
    public IReadOnlyList<ShapeCell> Cells { get; }
    public int Width { get; }
    public int Height { get; }
    public RgbColor Color { get; }

    public Shape(IEnumerable<ShapeCell> cells, RgbColor color)
    {
        Cells = cells.ToList();
        Width = Cells.Max(c => c.Col) + 1;
        Height = Cells.Max(c => c.Row) + 1;
        Color = color;
    }
}
