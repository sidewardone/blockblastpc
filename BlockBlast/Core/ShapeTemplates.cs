using System.Collections.Generic;
using System.Linq;

namespace BlockBlast.Core;

public static class ShapeTemplates
{
    public static IReadOnlyList<IReadOnlyList<ShapeCell[]>> Families { get; } = Build();

    private static List<IReadOnlyList<ShapeCell[]>> Build()
    {
        var smallCorner = SmallCornerFamily().ToArray();

        return new List<IReadOnlyList<ShapeCell[]>>
        {
            new[] { Line(1, true) },
            new[] { Line(2, true), Line(2, false) },
            new[] { Line(3, true), Line(3, false) },
            new[] { Line(4, true), Line(4, false) },
            new[] { Line(5, true), Line(5, false) },
            new[] { Square(2) },
            new[] { Rectangle(2, 3), Rectangle(3, 2) },
            new[] { Square(3) },
            BigCornerFamily().ToArray(),
            LFamily().ToArray(),
            SZFamily().ToArray(),
            TFamily().ToArray(),
            smallCorner,
            smallCorner,
        };
    }

    private static ShapeCell[] Cells(params (int Row, int Col)[] points)
    {
        var result = new ShapeCell[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            result[i] = new ShapeCell(points[i].Row, points[i].Col);
        }
        return result;
    }

    private static ShapeCell[] Line(int length, bool horizontal)
    {
        var points = new (int, int)[length];
        for (int i = 0; i < length; i++)
        {
            points[i] = horizontal ? (0, i) : (i, 0);
        }
        return Cells(points);
    }

    private static ShapeCell[] Square(int size)
    {
        var points = new List<(int, int)>();
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                points.Add((r, c));
            }
        }
        return Cells(points.ToArray());
    }

    private static ShapeCell[] Rectangle(int height, int width)
    {
        var points = new List<(int, int)>();
        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < width; c++)
            {
                points.Add((r, c));
            }
        }
        return Cells(points.ToArray());
    }

    private static IEnumerable<ShapeCell[]> BigCornerFamily()
    {
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 0), (2, 0));
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 2), (2, 2));
        yield return Cells((2, 0), (2, 1), (2, 2), (0, 0), (1, 0));
        yield return Cells((2, 0), (2, 1), (2, 2), (0, 2), (1, 2));
    }

    private static IEnumerable<ShapeCell[]> LFamily()
    {
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 0));
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 2));
        yield return Cells((0, 2), (1, 0), (1, 1), (1, 2));
        yield return Cells((0, 0), (1, 0), (1, 1), (1, 2));
        yield return Cells((0, 0), (1, 0), (2, 0), (2, 1));
        yield return Cells((0, 1), (1, 1), (2, 0), (2, 1));
        yield return Cells((0, 0), (0, 1), (1, 1), (2, 1));
        yield return Cells((0, 0), (0, 1), (1, 0), (2, 0));
    }

    private static IEnumerable<ShapeCell[]> SZFamily()
    {
        yield return Cells((0, 1), (0, 2), (1, 0), (1, 1));
        yield return Cells((0, 0), (0, 1), (1, 1), (1, 2));
        yield return Cells((0, 0), (1, 0), (1, 1), (2, 1));
        yield return Cells((0, 1), (1, 0), (1, 1), (2, 0));
    }

    private static IEnumerable<ShapeCell[]> TFamily()
    {
        yield return Cells((0, 1), (1, 0), (1, 1), (1, 2));
        yield return Cells((0, 0), (1, 0), (1, 1), (2, 0));
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 1));
        yield return Cells((0, 1), (1, 0), (1, 1), (2, 1));
    }

    private static IEnumerable<ShapeCell[]> SmallCornerFamily()
    {
        yield return Cells((0, 0), (1, 0), (1, 1));
        yield return Cells((0, 0), (0, 1), (1, 1));
        yield return Cells((0, 0), (0, 1), (1, 0));
        yield return Cells((0, 1), (1, 0), (1, 1));
    }
}
