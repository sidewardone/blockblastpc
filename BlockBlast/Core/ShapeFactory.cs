using System;
using System.Collections.Generic;

namespace BlockBlast.Core;

public static class ShapeFactory
{
    private static readonly Random Rng = new();

    public static RgbColor RandomColor() => Palette.Colors[Rng.Next(Palette.Colors.Length)];

    public static Shape CreateSingleCell()
    {
        return new Shape(new[] { new ShapeCell(0, 0) }, RandomColor());
    }

    public static Shape CreateFromFamily(IReadOnlyList<ShapeCell[]> family)
    {
        var variant = family[Rng.Next(family.Count)];
        return new Shape(variant, RandomColor());
    }
}
