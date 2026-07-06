using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BlockBlast.Core;
using BlockBlast.Localization;
using BlockBlast.Persistence;
using BlockBlast.UI;

namespace BlockBlast;

public partial class MainWindow : Window
{
    private const int Size = GameBoard.Size;
    private const double BoardCellSize = 52;
    private const double BoardGap = 4;
    private const double TrayCellSize = 26;
    private const double TrayGap = 3;

    private static readonly Brush ValidPreviewBrush = new SolidColorBrush(Color.FromArgb(150, 76, 230, 140));
    private static readonly Brush InvalidPreviewBrush = new SolidColorBrush(Color.FromArgb(150, 255, 80, 80));

    private readonly GameEngine _engine;

    private readonly Border[,] _previewOverlays = new Border[Size, Size];
    private readonly Border?[,] _cellBlocks = new Border[Size, Size];
    private readonly List<(int Row, int Col)> _activePreviewCells = new();

    private readonly Border[] _traySlotContainers = new Border[3];
    private readonly Canvas?[] _trayVisuals = new Canvas[3];

    private bool _dragging;
    private int _dragIndex = -1;
    private Shape? _dragShape;
    private Canvas? _dragGhost;
    private int _grabRow;
    private int _grabCol;
    private Point _grabOffset;
    private bool _dragValid;
    private int _dragOriginRow;
    private int _dragOriginCol;

    public MainWindow()
    {
        InitializeComponent();

        int best = SaveManager.LoadBestScore();
        _engine = new GameEngine(best);

        Loc.LanguageChanged += UpdateTexts;

        BuildBoard();
        BuildTray();
        UpdateScoreTexts();
        UpdateTexts();
    }

    private void BuildBoard()
    {
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        for (int i = 0; i < Size; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition());
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                var slot = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    CornerRadius = new CornerRadius(BoardCellSize * 0.18),
                    Margin = new Thickness(BoardGap / 2),
                };
                Grid.SetRow(slot, r);
                Grid.SetColumn(slot, c);
                BoardGrid.Children.Add(slot);

                var overlay = new Border
                {
                    CornerRadius = new CornerRadius(BoardCellSize * 0.18),
                    Margin = new Thickness(BoardGap / 2),
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false,
                };
                Grid.SetRow(overlay, r);
                Grid.SetColumn(overlay, c);
                Panel.SetZIndex(overlay, 10);
                BoardGrid.Children.Add(overlay);
                _previewOverlays[r, c] = overlay;
            }
        }
    }

    private void RefreshBoardVisuals()
    {
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                if (_cellBlocks[r, c] != null)
                {
                    BoardGrid.Children.Remove(_cellBlocks[r, c]);
                    _cellBlocks[r, c] = null;
                }
            }
        }
    }

    private void BuildTray()
    {
        TrayPanel.Children.Clear();
        for (int i = 0; i < 3; i++)
        {
            var container = new Border { Width = 150, Height = 150, Margin = new Thickness(4) };
            _traySlotContainers[i] = container;
            TrayPanel.Children.Add(container);
            RenderTrayShape(i);
        }
    }

    private void RenderTrayShape(int index)
    {
        var shape = _engine.Tray[index];
        var container = _traySlotContainers[index];
        container.Child = null;

        if (shape == null)
        {
            _trayVisuals[index] = null;
            return;
        }

        var canvas = ShapeVisualBuilder.Build(shape, TrayCellSize, TrayGap);
        canvas.HorizontalAlignment = HorizontalAlignment.Center;
        canvas.VerticalAlignment = VerticalAlignment.Center;
        canvas.Cursor = Cursors.Hand;
        container.Child = canvas;
        _trayVisuals[index] = canvas;

        canvas.MouseLeftButtonDown += (_, e) => StartDrag(index, e);

        canvas.RenderTransformOrigin = new Point(0.5, 0.5);
        var scaleT = new ScaleTransform(0.4, 0.4);
        canvas.RenderTransform = scaleT;
        canvas.Opacity = 0;

        canvas.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        var spawnEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
        scaleT.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = spawnEase });
        scaleT.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = spawnEase });
    }

    private void StartDrag(int trayIndex, MouseButtonEventArgs e)
    {
        if (_dragging)
        {
            return;
        }

        var shape = _engine.Tray[trayIndex];
        var canvas = _trayVisuals[trayIndex];
        if (shape == null || canvas == null)
        {
            return;
        }

        _dragging = true;
        _dragIndex = trayIndex;
        _dragShape = shape;

        var posInCanvas = e.GetPosition(canvas);
        _grabCol = Math.Clamp((int)(posInCanvas.X / TrayCellSize), 0, shape.Width - 1);
        _grabRow = Math.Clamp((int)(posInCanvas.Y / TrayCellSize), 0, shape.Height - 1);

        double offsetXInCell = posInCanvas.X - _grabCol * TrayCellSize;
        double offsetYInCell = posInCanvas.Y - _grabRow * TrayCellSize;
        double scaleFactor = BoardCellSize / TrayCellSize;
        _grabOffset = new Point(offsetXInCell * scaleFactor, offsetYInCell * scaleFactor);

        _dragGhost = ShapeVisualBuilder.Build(shape, BoardCellSize, BoardGap);
        _dragGhost.Opacity = 0.95;
        _dragGhost.IsHitTestVisible = false;
        Panel.SetZIndex(_dragGhost, 100);
        OverlayCanvas.Children.Add(_dragGhost);

        canvas.Opacity = 0.2;

        UpdateGhostPosition(e.GetPosition(OverlayCanvas));
        UpdateHover(e.GetPosition(BoardGrid));

        canvas.CaptureMouse();
        canvas.MouseMove += DragMouseMove;
        canvas.MouseLeftButtonUp += DragMouseUp;
        canvas.LostMouseCapture += DragLostCapture;

        e.Handled = true;
    }

    private void DragMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        UpdateGhostPosition(e.GetPosition(OverlayCanvas));
        UpdateHover(e.GetPosition(BoardGrid));
    }

    private void DragMouseUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag(commit: true);
    }

    private void DragLostCapture(object sender, MouseEventArgs e)
    {
        EndDrag(commit: false);
    }

    private void UpdateGhostPosition(Point overlayPos)
    {
        if (_dragGhost == null)
        {
            return;
        }

        double left = overlayPos.X - _grabCol * BoardCellSize - _grabOffset.X;
        double top = overlayPos.Y - _grabRow * BoardCellSize - _grabOffset.Y;
        Canvas.SetLeft(_dragGhost, left);
        Canvas.SetTop(_dragGhost, top);
    }

    private void UpdateHover(Point boardPos)
    {
        if (_dragShape == null)
        {
            return;
        }

        int hoverCol = (int)Math.Floor(boardPos.X / BoardCellSize);
        int hoverRow = (int)Math.Floor(boardPos.Y / BoardCellSize);
        int originRow = hoverRow - _grabRow;
        int originCol = hoverCol - _grabCol;

        _dragOriginRow = originRow;
        _dragOriginCol = originCol;
        _dragValid = _engine.CanPlace(_dragIndex, originRow, originCol);

        ClearPreviewHighlights();
        ApplyPreviewHighlights(_dragShape, originRow, originCol, _dragValid);
    }

    private void ApplyPreviewHighlights(Shape shape, int originRow, int originCol, bool valid)
    {
        foreach (var cell in shape.Cells)
        {
            int r = originRow + cell.Row;
            int c = originCol + cell.Col;
            if (!GameBoard.IsInside(r, c))
            {
                continue;
            }

            var overlay = _previewOverlays[r, c];
            overlay.Background = valid ? ValidPreviewBrush : InvalidPreviewBrush;
            overlay.Visibility = Visibility.Visible;
            _activePreviewCells.Add((r, c));
        }
    }

    private void ClearPreviewHighlights()
    {
        foreach (var (r, c) in _activePreviewCells)
        {
            _previewOverlays[r, c].Visibility = Visibility.Collapsed;
        }
        _activePreviewCells.Clear();
    }

    private void EndDrag(bool commit)
    {
        if (!_dragging)
        {
            return;
        }
        _dragging = false;

        var canvas = _trayVisuals[_dragIndex];
        if (canvas != null)
        {
            canvas.MouseMove -= DragMouseMove;
            canvas.MouseLeftButtonUp -= DragMouseUp;
            canvas.LostMouseCapture -= DragLostCapture;
            if (canvas.IsMouseCaptured)
            {
                canvas.ReleaseMouseCapture();
            }
            canvas.Opacity = 1;
        }

        ClearPreviewHighlights();

        if (_dragGhost != null)
        {
            OverlayCanvas.Children.Remove(_dragGhost);
            _dragGhost = null;
        }

        if (commit && _dragValid && _dragShape != null)
        {
            CommitPlacement(_dragIndex, _dragOriginRow, _dragOriginCol);
        }

        _dragIndex = -1;
        _dragShape = null;
    }

    private void CommitPlacement(int trayIndex, int row, int col)
    {
        var shape = _engine.Tray[trayIndex]!;

        var container = _traySlotContainers[trayIndex];
        container.Child = null;
        _trayVisuals[trayIndex] = null;

        var result = _engine.PlaceShape(trayIndex, row, col);

        AnimatePlacement(shape, row, col, () =>
        {
            if (result.LinesCleared > 0)
            {
                AnimateLineClear(result.ClearedRows, result.ClearedCols, () => FinishMove(result));
            }
            else
            {
                FinishMove(result);
            }
        });
    }

    private void AnimatePlacement(Shape shape, int originRow, int originCol, Action onComplete)
    {
        foreach (var cell in shape.Cells)
        {
            int r = originRow + cell.Row;
            int c = originCol + cell.Col;

            var block = BlockVisualFactory.CreateBlock(shape.Color, BoardCellSize - BoardGap);
            Grid.SetRow(block, r);
            Grid.SetColumn(block, c);
            Panel.SetZIndex(block, 5);
            block.Opacity = 0;

            var scale = (ScaleTransform)block.RenderTransform;
            scale.ScaleX = 0.35;
            scale.ScaleY = 0.35;

            BoardGrid.Children.Add(block);
            _cellBlocks[r, c] = block;

            block.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
            var placeEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = placeEase });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = placeEase });
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(190) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            onComplete();
        };
        timer.Start();
    }

    private void AnimateLineClear(IReadOnlyList<int> rows, IReadOnlyList<int> cols, Action onComplete)
    {
        var cellsToClear = new HashSet<(int Row, int Col)>();
        foreach (var r in rows)
        {
            for (int c = 0; c < Size; c++)
            {
                cellsToClear.Add((r, c));
            }
        }
        foreach (var c in cols)
        {
            for (int r = 0; r < Size; r++)
            {
                cellsToClear.Add((r, c));
            }
        }

        var flashes = new List<Border>();

        foreach (var (r, c) in cellsToClear)
        {
            var block = _cellBlocks[r, c];
            if (block == null)
            {
                continue;
            }

            var flash = new Border
            {
                Width = block.Width,
                Height = block.Height,
                CornerRadius = block.CornerRadius,
                Background = Brushes.White,
                Opacity = 0.85,
            };
            Grid.SetRow(flash, r);
            Grid.SetColumn(flash, c);
            Panel.SetZIndex(flash, 6);
            BoardGrid.Children.Add(flash);
            flashes.Add(flash);
            flash.BeginAnimation(OpacityProperty, new DoubleAnimation(0.85, 0, TimeSpan.FromMilliseconds(260)));

            var scale = (ScaleTransform)block.RenderTransform;
            var clearEase = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.3 };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 0.15, TimeSpan.FromMilliseconds(300)) { EasingFunction = clearEase });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 0.15, TimeSpan.FromMilliseconds(300)) { EasingFunction = clearEase });
            block.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)));
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(310) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            foreach (var (r, c) in cellsToClear)
            {
                var block = _cellBlocks[r, c];
                if (block != null)
                {
                    BoardGrid.Children.Remove(block);
                    _cellBlocks[r, c] = null;
                }
            }
            foreach (var flash in flashes)
            {
                BoardGrid.Children.Remove(flash);
            }
            onComplete();
        };
        timer.Start();
    }

    private void ShowComboText(int comboStreak)
    {
        var tb = new TextBlock
        {
            Text = Loc.Format("Combo", comboStreak),
            FontSize = 36,
            FontWeight = FontWeights.Black,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.4,
            },
        };
        tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var topLeft = BoardBorder.TranslatePoint(new Point(0, 0), OverlayCanvas);
        double centerX = topLeft.X + BoardBorder.ActualWidth / 2;
        double centerY = topLeft.Y + BoardBorder.ActualHeight / 2;

        Canvas.SetLeft(tb, centerX - tb.DesiredSize.Width / 2);
        Canvas.SetTop(tb, centerY - tb.DesiredSize.Height / 2);
        Panel.SetZIndex(tb, 200);

        var group = new TransformGroup();
        var scaleT = new ScaleTransform(0.4, 0.4);
        var translateT = new TranslateTransform(0, 0);
        group.Children.Add(scaleT);
        group.Children.Add(translateT);
        tb.RenderTransformOrigin = new Point(0.5, 0.5);
        tb.RenderTransform = group;
        tb.Opacity = 0;

        OverlayCanvas.Children.Add(tb);

        var popEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 };
        scaleT.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = popEase });
        scaleT.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = popEase });
        translateT.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -46, TimeSpan.FromMilliseconds(950)));

        var opacityFrames = new DoubleAnimationUsingKeyFrames();
        opacityFrames.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))));
        opacityFrames.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600))));
        opacityFrames.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(950))));
        tb.BeginAnimation(OpacityProperty, opacityFrames);

        var removeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(970) };
        removeTimer.Tick += (_, _) =>
        {
            removeTimer.Stop();
            OverlayCanvas.Children.Remove(tb);
        };
        removeTimer.Start();
    }

    private void FinishMove(PlacementResult result)
    {
        if (result.ComboStreak >= 2)
        {
            ShowComboText(result.ComboStreak);
        }

        if (result.TrayRefilled)
        {
            for (int i = 0; i < 3; i++)
            {
                RenderTrayShape(i);
            }
        }

        SaveManager.SaveBestScore(_engine.BestScore);
        UpdateScoreTexts();

        if (result.IsGameOver)
        {
            ShowGameOver(result.IsNewBest);
        }
    }

    private void ShowGameOver(bool isNewBest)
    {
        GameOverScoreText.Text = _engine.Score.ToString();
        GameOverBestText.Text = _engine.BestScore.ToString();
        GameOverNewBestText.Visibility = isNewBest ? Visibility.Visible : Visibility.Collapsed;
        GameOverOverlay.Opacity = 0;
        GameOverOverlay.Visibility = Visibility.Visible;
        GameOverOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280)));
    }

    private void UpdateScoreTexts()
    {
        ScoreValueText.Text = _engine.Score.ToString();
        BestValueText.Text = _engine.BestScore.ToString();
    }

    private void UpdateTexts()
    {
        ScoreLabel.Text = Loc.Get("Score");
        BestLabel.Text = Loc.Get("Best");
        GameOverTitleText.Text = Loc.Get("GameOver");
        GameOverScoreLabelText.Text = Loc.Get("YourScore");
        GameOverBestLabelText.Text = Loc.Get("BestScoreLabel");
        GameOverNewBestText.Text = Loc.Get("NewBest");
        RestartButton.Content = Loc.Get("Restart");
        LangButton.Content = Loc.Current == BlockBlast.Localization.Language.English ? "RU" : "EN";
    }

    private void LangButton_Click(object sender, RoutedEventArgs e)
    {
        Loc.Toggle();
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        GameOverOverlay.Visibility = Visibility.Collapsed;
        _engine.Restart();
        RefreshBoardVisuals();
        for (int i = 0; i < 3; i++)
        {
            RenderTrayShape(i);
        }
        UpdateScoreTexts();
    }
}
