using System.Collections.Generic;

namespace BlockBlast.Core;

public static class ShapeTemplates
{
    public static IReadOnlyList<ShapeCell[]> All { get; } = Build();

    private static List<ShapeCell[]> Build()
    {
        var list = new List<ShapeCell[]>();

        for (int len = 1; len <= 5; len++)
        {
            list.Add(Line(len, horizontal: true));
            if (len > 1)
            {
                list.Add(Line(len, horizontal: false));
            }
        }

        list.Add(Square(2));
        list.Add(Square(3));

        list.AddRange(LFamily());
        list.AddRange(TFamily());
        list.AddRange(SZFamily());
        list.AddRange(Corner3Family());
        list.AddRange(Corner5Family());
        list.Add(Plus());

        return list;
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

    private static IEnumerable<ShapeCell[]> LFamily()
    {
        yield return Cells((0, 0), (1, 0), (2, 0), (2, 1));
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 0));
        yield return Cells((0, 0), (0, 1), (1, 1), (2, 1));
        yield return Cells((1, 0), (1, 1), (1, 2), (0, 2));

        yield return Cells((0, 1), (1, 1), (2, 0), (2, 1));
        yield return Cells((0, 0), (1, 0), (1, 1), (1, 2));
        yield return Cells((0, 0), (0, 1), (1, 0), (2, 0));
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 2));
    }

    private static IEnumerable<ShapeCell[]> TFamily()
    {
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 1));
        yield return Cells((0, 1), (1, 0), (1, 1), (2, 1));
        yield return Cells((1, 0), (1, 1), (1, 2), (0, 1));
        yield return Cells((0, 0), (1, 0), (2, 0), (1, 1));
    }

    private static IEnumerable<ShapeCell[]> SZFamily()
    {
        yield return Cells((0, 1), (0, 2), (1, 0), (1, 1));
        yield return Cells((0, 0), (0, 1), (1, 1), (1, 2));
        yield return Cells((0, 0), (1, 0), (1, 1), (2, 1));
        yield return Cells((0, 1), (1, 0), (1, 1), (2, 0));
    }

    private static IEnumerable<ShapeCell[]> Corner3Family()
    {
        yield return Cells((0, 0), (1, 0), (1, 1));
        yield return Cells((0, 0), (0, 1), (1, 0));
        yield return Cells((0, 0), (0, 1), (1, 1));
        yield return Cells((0, 1), (1, 0), (1, 1));
    }

    private static IEnumerable<ShapeCell[]> Corner5Family()
    {
        yield return Cells((0, 0), (1, 0), (2, 0), (2, 1), (2, 2));
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 0), (2, 0));
        yield return Cells((0, 0), (0, 1), (0, 2), (1, 2), (2, 2));
        yield return Cells((0, 2), (1, 2), (2, 0), (2, 1), (2, 2));
    }

    private static ShapeCell[] Plus()
    {
        return Cells((0, 1), (1, 0), (1, 1), (1, 2), (2, 1));
    }
}
