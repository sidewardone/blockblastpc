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
    private const double BoardCellSize = 52;
    private const double BoardGap = 4;
    private const double TrayCellSize = 26;
    private const double TrayGap = 3;
    private const double DragThreshold = 8.0;

    private static readonly Brush ValidPreviewBrush = new SolidColorBrush(Color.FromArgb(150, 76, 230, 140));
    private static readonly Brush InvalidPreviewBrush = new SolidColorBrush(Color.FromArgb(150, 255, 80, 80));

    private enum PointerMode
    {
        None,
        Pending,
        Dragging,
        Armed,
    }

    private readonly GameEngine _engine;

    private Border[,] _previewOverlays = new Border[0, 0];
    private Border?[,] _cellBlocks = new Border?[0, 0];
    private readonly List<(int Row, int Col)> _activePreviewCells = new();

    private readonly Border[] _traySlotContainers = new Border[3];
    private readonly Canvas?[] _trayVisuals = new Canvas[3];

    private PointerMode _pointerMode = PointerMode.None;
    private int _activeIndex = -1;
    private Shape? _activeShape;
    private Canvas? _ghost;
    private ScaleTransform? _ghostScale;
    private int _grabRow;
    private int _grabCol;
    private Point _grabOffset;
    private Point _pointerDownPos;
    private bool _resolvedValid;
    private int _resolvedRow;
    private int _resolvedCol;

    public MainWindow()
    {
        InitializeComponent();

        int best = SaveManager.LoadBestScore();
        _engine = new GameEngine(GameBoard.DefaultSize, best);

        Loc.LanguageChanged += UpdateTexts;

        RebuildBoardUI();
        BuildTray();
        UpdateScoreTexts();
        UpdateTexts();
        UpdateBoardSizeButtonText();

        MouseMove += Window_MouseMove;
        MouseLeftButtonDown += Window_MouseLeftButtonDown;
        PreviewKeyDown += Window_PreviewKeyDown;
    }

    private void RebuildBoardUI()
    {
        int size = _engine.Board.Size;

        BoardGrid.Children.Clear();
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.Width = size * BoardCellSize;
        BoardGrid.Height = size * BoardCellSize;

        for (int i = 0; i < size; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition());
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        _previewOverlays = new Border[size, size];
        _cellBlocks = new Border?[size, size];
        _activePreviewCells.Clear();

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
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

    private void BuildTray()
    {
        TrayPanel.Children.Clear();
        for (int i = 0; i < 3; i++)
        {
            var container = new Border
            {
                Width = 150,
                Height = 150,
                Margin = new Thickness(4),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
            };
            int index = i;
            container.MouseLeftButtonDown += (_, e) => TrayContainer_MouseLeftButtonDown(index, e);
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
        canvas.IsHitTestVisible = false;
        container.Child = canvas;
        _trayVisuals[index] = canvas;

        canvas.RenderTransformOrigin = new Point(0.5, 0.5);
        var scaleT = new ScaleTransform(0.4, 0.4);
        canvas.RenderTransform = scaleT;
        canvas.Opacity = 0;

        canvas.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        var spawnEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
        scaleT.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = spawnEase });
        scaleT.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = spawnEase });
    }

    private void TrayContainer_MouseLeftButtonDown(int index, MouseButtonEventArgs e)
    {
        var shape = _engine.Tray[index];
        if (shape == null)
        {
            return;
        }

        if (_pointerMode == PointerMode.Dragging)
        {
            return;
        }

        if (_pointerMode != PointerMode.None)
        {
            CancelActive();
        }

        var container = _traySlotContainers[index];
        var canvas = _trayVisuals[index]!;

        _activeIndex = index;
        _activeShape = shape;
        _pointerMode = PointerMode.Pending;

        var posInCanvas = e.GetPosition(canvas);
        _grabCol = Math.Clamp((int)(posInCanvas.X / TrayCellSize), 0, shape.Width - 1);
        _grabRow = Math.Clamp((int)(posInCanvas.Y / TrayCellSize), 0, shape.Height - 1);

        double offsetXInCell = posInCanvas.X - _grabCol * TrayCellSize;
        double offsetYInCell = posInCanvas.Y - _grabRow * TrayCellSize;
        double scaleFactor = BoardCellSize / TrayCellSize;
        _grabOffset = new Point(offsetXInCell * scaleFactor, offsetYInCell * scaleFactor);

        CreateGhost(shape);
        canvas.Opacity = 0.15;

        _pointerDownPos = e.GetPosition(OverlayCanvas);
        UpdatePointerVisuals(_pointerDownPos, e.GetPosition(BoardGrid));

        container.CaptureMouse();
        container.MouseMove += Container_MouseMove;
        container.MouseLeftButtonUp += Container_MouseLeftButtonUp;
        container.LostMouseCapture += Container_LostMouseCapture;

        e.Handled = true;
    }

    private void Container_MouseMove(object sender, MouseEventArgs e)
    {
        if (_pointerMode != PointerMode.Pending && _pointerMode != PointerMode.Dragging)
        {
            return;
        }

        var overlayPos = e.GetPosition(OverlayCanvas);
        UpdatePointerVisuals(overlayPos, e.GetPosition(BoardGrid));

        if (_pointerMode == PointerMode.Pending)
        {
            double dx = overlayPos.X - _pointerDownPos.X;
            double dy = overlayPos.Y - _pointerDownPos.Y;
            if (dx * dx + dy * dy > DragThreshold * DragThreshold)
            {
                _pointerMode = PointerMode.Dragging;
            }
        }
    }

    private void Container_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        FinishPointerGesture();
    }

    private void Container_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_pointerMode == PointerMode.Pending || _pointerMode == PointerMode.Dragging)
        {
            CancelActive();
        }
    }

    private void FinishPointerGesture()
    {
        var priorMode = _pointerMode;
        var index = _activeIndex;

        _pointerMode = PointerMode.None;

        var container = index >= 0 ? _traySlotContainers[index] : null;
        if (container != null)
        {
            container.MouseMove -= Container_MouseMove;
            container.MouseLeftButtonUp -= Container_MouseLeftButtonUp;
            container.LostMouseCapture -= Container_LostMouseCapture;
            if (container.IsMouseCaptured)
            {
                container.ReleaseMouseCapture();
            }
        }

        if (priorMode == PointerMode.Dragging)
        {
            if (_resolvedValid)
            {
                CommitPlacement(index, _resolvedRow, _resolvedCol);
            }
            else
            {
                ReturnActiveToTray();
            }
        }
        else if (priorMode == PointerMode.Pending)
        {
            _pointerMode = PointerMode.Armed;
        }
    }

    private void CancelActive()
    {
        if (_activeIndex < 0)
        {
            return;
        }

        var container = _traySlotContainers[_activeIndex];
        if (container != null)
        {
            container.MouseMove -= Container_MouseMove;
            container.MouseLeftButtonUp -= Container_MouseLeftButtonUp;
            container.LostMouseCapture -= Container_LostMouseCapture;
            if (container.IsMouseCaptured)
            {
                container.ReleaseMouseCapture();
            }
        }

        ReturnActiveToTray();
    }

    private void ReturnActiveToTray()
    {
        if (_activeIndex >= 0)
        {
            var canvas = _trayVisuals[_activeIndex];
            if (canvas != null)
            {
                canvas.Opacity = 1;
            }
        }

        ClearPreviewHighlights();

        if (_ghost != null)
        {
            OverlayCanvas.Children.Remove(_ghost);
            _ghost = null;
            _ghostScale = null;
        }

        _pointerMode = PointerMode.None;
        _activeIndex = -1;
        _activeShape = null;
    }

    private void CreateGhost(Shape shape)
    {
        _ghost = ShapeVisualBuilder.Build(shape, BoardCellSize, BoardGap);
        _ghost.Opacity = 0.95;
        _ghost.IsHitTestVisible = false;
        _ghost.RenderTransformOrigin = new Point(0, 0);
        _ghostScale = new ScaleTransform(1, 1);
        _ghost.RenderTransform = _ghostScale;
        Panel.SetZIndex(_ghost, 100);
        OverlayCanvas.Children.Add(_ghost);
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_pointerMode != PointerMode.Armed)
        {
            return;
        }

        UpdatePointerVisuals(e.GetPosition(OverlayCanvas), e.GetPosition(BoardGrid));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_pointerMode != PointerMode.Armed)
        {
            return;
        }

        if (_resolvedValid)
        {
            CommitPlacement(_activeIndex, _resolvedRow, _resolvedCol);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        int? index = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            _ => null,
        };

        if (index.HasValue)
        {
            SelectViaKeyboard(index.Value);
            e.Handled = true;
        }
    }

    private void SelectViaKeyboard(int index)
    {
        var shape = _engine.Tray[index];
        if (shape == null)
        {
            return;
        }

        if (_pointerMode == PointerMode.Dragging || _pointerMode == PointerMode.Pending)
        {
            return;
        }

        if (_activeIndex == index && _pointerMode == PointerMode.Armed)
        {
            return;
        }

        if (_pointerMode == PointerMode.Armed)
        {
            CancelActive();
        }

        _activeIndex = index;
        _activeShape = shape;
        _pointerMode = PointerMode.Armed;

        var anchor = GetAnchorCell(shape);
        _grabRow = anchor.Row;
        _grabCol = anchor.Col;
        _grabOffset = new Point(BoardCellSize / 2.0, BoardCellSize / 2.0);

        var canvas = _trayVisuals[index];
        if (canvas != null)
        {
            canvas.Opacity = 0.15;
        }

        CreateGhost(shape);
        UpdatePointerVisuals(Mouse.GetPosition(OverlayCanvas), Mouse.GetPosition(BoardGrid));
    }

    private static ShapeCell GetAnchorCell(Shape shape)
    {
        double centerRow = (shape.Height - 1) / 2.0;
        double centerCol = (shape.Width - 1) / 2.0;
        var best = shape.Cells[0];
        double bestDist = double.MaxValue;
        foreach (var cell in shape.Cells)
        {
            double dist = ((cell.Row - centerRow) * (cell.Row - centerRow)) + ((cell.Col - centerCol) * (cell.Col - centerCol));
            if (dist < bestDist)
            {
                bestDist = dist;
                best = cell;
            }
        }
        return best;
    }

    private void UpdatePointerVisuals(Point overlayPos, Point boardPos)
    {
        if (_activeShape == null || _ghost == null || _ghostScale == null)
        {
            return;
        }

        int hoverCol = (int)Math.Floor(boardPos.X / BoardCellSize);
        int hoverRow = (int)Math.Floor(boardPos.Y / BoardCellSize);
        int rawRow = hoverRow - _grabRow;
        int rawCol = hoverCol - _grabCol;

        var (row, col, valid, snapped) = ResolvePlacement(_activeShape, rawRow, rawCol);
        _resolvedRow = row;
        _resolvedCol = col;
        _resolvedValid = valid;

        ClearPreviewHighlights();
        ApplyPreviewHighlights(_activeShape, row, col, valid);

        double effectiveCellSize = GetEffectiveCellSize();
        double scale = effectiveCellSize / BoardCellSize;
        _ghostScale.ScaleX = scale;
        _ghostScale.ScaleY = scale;

        if (snapped)
        {
            var topLeft = BoardGrid.TranslatePoint(new Point(col * BoardCellSize, row * BoardCellSize), OverlayCanvas);
            Canvas.SetLeft(_ghost, topLeft.X);
            Canvas.SetTop(_ghost, topLeft.Y);
        }
        else
        {
            double left = overlayPos.X - (_grabCol * effectiveCellSize) - (_grabOffset.X * scale);
            double top = overlayPos.Y - (_grabRow * effectiveCellSize) - (_grabOffset.Y * scale);
            Canvas.SetLeft(_ghost, left);
            Canvas.SetTop(_ghost, top);
        }
    }

    private double GetEffectiveCellSize()
    {
        var p0 = BoardGrid.TranslatePoint(new Point(0, 0), OverlayCanvas);
        var p1 = BoardGrid.TranslatePoint(new Point(BoardCellSize, 0), OverlayCanvas);
        return p1.X - p0.X;
    }

    private (int Row, int Col, bool Valid, bool Snapped) ResolvePlacement(Shape shape, int rawRow, int rawCol)
    {
        if (_engine.Board.CanPlace(shape, rawRow, rawCol))
        {
            return (rawRow, rawCol, true, false);
        }

        (int Row, int Col)? best = null;
        int bestDist = int.MaxValue;
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0)
                {
                    continue;
                }
                int r = rawRow + dr;
                int c = rawCol + dc;
                if (_engine.Board.CanPlace(shape, r, c))
                {
                    int dist = (dr * dr) + (dc * dc);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = (r, c);
                    }
                }
            }
        }

        if (best.HasValue)
        {
            return (best.Value.Row, best.Value.Col, true, true);
        }

        return (rawRow, rawCol, false, false);
    }

    private void ApplyPreviewHighlights(Shape shape, int originRow, int originCol, bool valid)
    {
        foreach (var cell in shape.Cells)
        {
            int r = originRow + cell.Row;
            int c = originCol + cell.Col;
            if (!_engine.Board.IsInside(r, c))
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

    private void CommitPlacement(int trayIndex, int row, int col)
    {
        var shape = _engine.Tray[trayIndex]!;

        ClearPreviewHighlights();
        if (_ghost != null)
        {
            OverlayCanvas.Children.Remove(_ghost);
            _ghost = null;
            _ghostScale = null;
        }
        _pointerMode = PointerMode.None;
        _activeIndex = -1;
        _activeShape = null;

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
        int size = _engine.Board.Size;
        var cellsToClear = new HashSet<(int Row, int Col)>();
        foreach (var r in rows)
        {
            for (int c = 0; c < size; c++)
            {
                cellsToClear.Add((r, c));
            }
        }
        foreach (var c in cols)
        {
            for (int r = 0; r < size; r++)
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
        BoardSizeTitleText.Text = Loc.Get("BoardSize");
        BoardSizeHintText.Text = Loc.Get("BoardSizeHint");
        BoardSizeCancelButton.Content = Loc.Get("Cancel");
        LangButton.Content = Loc.Current == Localization.Language.English ? "RU" : "EN";
    }

    private void LangButton_Click(object sender, RoutedEventArgs e)
    {
        Loc.Toggle();
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        GameOverOverlay.Visibility = Visibility.Collapsed;
        _engine.Restart();
        RebuildBoardUI();
        BuildTray();
        UpdateScoreTexts();
    }

    private void BoardSizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pointerMode != PointerMode.None)
        {
            CancelActive();
        }

        BoardSizeOverlay.Opacity = 0;
        BoardSizeOverlay.Visibility = Visibility.Visible;
        BoardSizeOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
    }

    private void BoardSizeOptionButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        int newSize = int.Parse((string)button.Tag);
        BoardSizeOverlay.Visibility = Visibility.Collapsed;

        if (newSize != _engine.Board.Size)
        {
            _engine.Restart(newSize);
            RebuildBoardUI();
            BuildTray();
            UpdateScoreTexts();
        }

        UpdateBoardSizeButtonText();
    }

    private void BoardSizeCancelButton_Click(object sender, RoutedEventArgs e)
    {
        BoardSizeOverlay.Visibility = Visibility.Collapsed;
    }

    private void UpdateBoardSizeButtonText()
    {
        BoardSizeButton.Content = $"{_engine.Board.Size}x{_engine.Board.Size}";
    }
}
