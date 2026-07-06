using System.Collections.Generic;

namespace BlockBlast.Core;

public sealed class GameBoard
{
    public const int MinSize = 6;
    public const int MaxSize = 10;
    public const int DefaultSize = 8;

    public int Size { get; }

    private readonly RgbColor?[,] _cells;

    public GameBoard(int size)
    {
        Size = size;
        _cells = new RgbColor?[size, size];
    }

    public RgbColor? CellAt(int row, int col) => _cells[row, col];

    public bool IsInside(int row, int col) => row >= 0 && row < Size && col >= 0 && col < Size;

    public bool CanPlace(Shape shape, int originRow, int originCol)
    {
        foreach (var cell in shape.Cells)
        {
            int r = originRow + cell.Row;
            int c = originCol + cell.Col;
            if (!IsInside(r, c))
            {
                return false;
            }
            if (_cells[r, c].HasValue)
            {
                return false;
            }
        }
        return true;
    }

    public void Place(Shape shape, int originRow, int originCol)
    {
        foreach (var cell in shape.Cells)
        {
            _cells[originRow + cell.Row, originCol + cell.Col] = shape.Color;
        }
    }

    public List<int> GetFullRows()
    {
        var rows = new List<int>();
        for (int r = 0; r < Size; r++)
        {
            bool full = true;
            for (int c = 0; c < Size; c++)
            {
                if (!_cells[r, c].HasValue)
                {
                    full = false;
                    break;
                }
            }
            if (full)
            {
                rows.Add(r);
            }
        }
        return rows;
    }

    public List<int> GetFullCols()
    {
        var cols = new List<int>();
        for (int c = 0; c < Size; c++)
        {
            bool full = true;
            for (int r = 0; r < Size; r++)
            {
                if (!_cells[r, c].HasValue)
                {
                    full = false;
                    break;
                }
            }
            if (full)
            {
                cols.Add(c);
            }
        }
        return cols;
    }

    public void ClearRowsAndCols(IEnumerable<int> rows, IEnumerable<int> cols)
    {
        foreach (var r in rows)
        {
            for (int c = 0; c < Size; c++)
            {
                _cells[r, c] = null;
            }
        }
        foreach (var c in cols)
        {
            for (int r = 0; r < Size; r++)
            {
                _cells[r, c] = null;
            }
        }
    }

    public bool HasAnyValidPlacement(IEnumerable<Shape?> shapes)
    {
        foreach (var shape in shapes)
        {
            if (shape == null)
            {
                continue;
            }
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if (CanPlace(shape, r, c))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool HasEmptyCell()
    {
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                if (!_cells[r, c].HasValue)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public GameBoard Clone()
    {
        var clone = new GameBoard(Size);
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                clone._cells[r, c] = _cells[r, c];
            }
        }
        return clone;
    }
}
