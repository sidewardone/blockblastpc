using System;
using System.Linq;

namespace BlockBlast.Core;

public static class ShapeFactory
{
    private static readonly Random Rng = new();

    public static Shape CreateRandom()
    {
        var template = ShapeTemplates.All[Rng.Next(ShapeTemplates.All.Count)];
        var color = Palette.Colors[Rng.Next(Palette.Colors.Length)];
        return new Shape(template, color);
    }

    public static Shape?[] CreateTray(int count = 3)
    {
        return Enumerable.Range(0, count).Select(_ => (Shape?)CreateRandom()).ToArray();
    }
}
