using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockBlast.Core;

public static class TrayGenerator
{
    private static readonly Random Rng = new();

    public static Shape?[] Generate(GameBoard board, int count = 3)
    {
        var result = new Shape?[count];
        var simulation = board.Clone();
        var remainingFamilies = Enumerable.Range(0, ShapeTemplates.Families.Count).ToList();

        for (int slot = 0; slot < count; slot++)
        {
            Shuffle(remainingFamilies);
            bool placed = false;

            foreach (var familyIndex in remainingFamilies)
            {
                var family = ShapeTemplates.Families[familyIndex];
                Shape? candidate = null;
                (int Row, int Col) position = default;

                foreach (var variantCells in family)
                {
                    var shape = new Shape(variantCells, ShapeFactory.RandomColor());
                    var spot = FindAnyValidPosition(simulation, shape);
                    if (spot.HasValue)
                    {
                        candidate = shape;
                        position = spot.Value;
                        break;
                    }
                }

                if (candidate != null)
                {
                    result[slot] = candidate;
                    ApplyTentativePlacement(simulation, candidate, position.Row, position.Col);
                    remainingFamilies.Remove(familyIndex);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                var single = ShapeFactory.CreateSingleCell();
                result[slot] = single;
                var spot = FindAnyValidPosition(simulation, single);
                if (spot.HasValue)
                {
                    ApplyTentativePlacement(simulation, single, spot.Value.Row, spot.Value.Col);
                }
            }
        }

        return result;
    }

    private static void ApplyTentativePlacement(GameBoard simulation, Shape shape, int row, int col)
    {
        simulation.Place(shape, row, col);
        var fullRows = simulation.GetFullRows();
        var fullCols = simulation.GetFullCols();
        if (fullRows.Count > 0 || fullCols.Count > 0)
        {
            simulation.ClearRowsAndCols(fullRows, fullCols);
        }
    }

    private static (int Row, int Col)? FindAnyValidPosition(GameBoard board, Shape shape)
    {
        for (int r = 0; r <= board.Size - shape.Height; r++)
        {
            for (int c = 0; c <= board.Size - shape.Width; c++)
            {
                if (board.CanPlace(shape, r, c))
                {
                    return (r, c);
                }
            }
        }
        return null;
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
