using System.Linq;

namespace BlockBlast.Core;

public sealed class GameEngine
{
    public GameBoard Board { get; private set; }
    public Shape?[] Tray { get; private set; }
    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public int ComboStreak { get; private set; }

    public GameEngine(int boardSize = GameBoard.DefaultSize, int bestScore = 0)
    {
        Board = new GameBoard(boardSize);
        BestScore = bestScore;
        Tray = TrayGenerator.Generate(Board);
    }

    public bool CanPlace(int trayIndex, int row, int col)
    {
        var shape = Tray[trayIndex];
        return shape != null && Board.CanPlace(shape, row, col);
    }

    public PlacementResult PlaceShape(int trayIndex, int row, int col)
    {
        var shape = Tray[trayIndex]!;
        Board.Place(shape, row, col);
        int points = shape.Cells.Count;
        Tray[trayIndex] = null;

        var fullRows = Board.GetFullRows();
        var fullCols = Board.GetFullCols();
        int linesCleared = fullRows.Count + fullCols.Count;

        if (linesCleared > 0)
        {
            Board.ClearRowsAndCols(fullRows, fullCols);
            ComboStreak++;
            points += linesCleared * 10 * ComboStreak;
        }
        else
        {
            ComboStreak = 0;
        }

        Score += points;

        bool refilled = false;
        if (Tray.All(s => s == null))
        {
            Tray = TrayGenerator.Generate(Board);
            refilled = true;
        }

        bool gameOver = !Board.HasAnyValidPlacement(Tray);

        bool isNewBest = false;
        if (Score > BestScore)
        {
            BestScore = Score;
            isNewBest = true;
        }

        return new PlacementResult
        {
            PointsGained = points,
            LinesCleared = linesCleared,
            ClearedRows = fullRows,
            ClearedCols = fullCols,
            ComboStreak = ComboStreak,
            TrayRefilled = refilled,
            IsGameOver = gameOver,
            IsNewBest = isNewBest,
        };
    }

    public void Restart(int? boardSize = null)
    {
        Board = new GameBoard(boardSize ?? Board.Size);
        Score = 0;
        ComboStreak = 0;
        Tray = TrayGenerator.Generate(Board);
    }
}
