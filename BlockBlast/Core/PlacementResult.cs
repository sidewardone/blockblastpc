using System.Collections.Generic;

namespace BlockBlast.Core;

public sealed class PlacementResult
{
    public int PointsGained { get; init; }
    public int LinesCleared { get; init; }
    public IReadOnlyList<int> ClearedRows { get; init; } = System.Array.Empty<int>();
    public IReadOnlyList<int> ClearedCols { get; init; } = System.Array.Empty<int>();
    public int ComboStreak { get; init; }
    public bool TrayRefilled { get; init; }
    public bool IsGameOver { get; init; }
    public bool IsNewBest { get; init; }
}
