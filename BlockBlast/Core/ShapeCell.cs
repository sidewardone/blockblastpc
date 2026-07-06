namespace BlockBlast.Core;

public readonly struct ShapeCell
{
    public int Row { get; }
    public int Col { get; }

    public ShapeCell(int row, int col)
    {
        Row = row;
        Col = col;
    }
}
