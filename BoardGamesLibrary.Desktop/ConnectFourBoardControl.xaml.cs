/**
 * @file: ConnectFourBoardControl.xaml.cs
 * @description: Інтерактивна дошка для Connect Four з кліками мишкою
 * @dependencies: ConnectFourGame, ConnectFourMove
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.ConnectFour;
using BoardGamesLibrary.Desktop.Services;
using static BoardGamesLibrary.Core.GameState;

namespace BoardGamesLibrary.Desktop;

public partial class ConnectFourBoardControl : UserControl
{
    private ConnectFourGame? _game;
    private Action? _onMoveMade;
    private Player? _humanPlayer;
    private readonly Dictionary<Position, ConnectFourCellRefs> _cells = new();
    private int? _hoveredColumn;
    private Position? _hoveredDropPosition;
    private HashSet<Position> _winningPositions = new();
    private bool _isProcessingClick = false;
    public ConnectFourBoardControl() => InitializeComponent();

    public void Initialize(ConnectFourGame game, Action onMoveMade, Player? humanPlayer = null)
    {
        _game = game;
        _onMoveMade = onMoveMade;
        _humanPlayer = humanPlayer;
        _hoveredColumn = null;
        _hoveredDropPosition = null;
        _winningPositions.Clear();
        _cells.Clear();
        BoardGrid.Children.Clear();
        AnimationCanvas.Children.Clear();

        CreateBoard();
        UpdateBoard();
    }

    private void CreateBoard()
    {
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var position = new Position(row, col);
                var refs = CreateCell(position);
                _cells[position] = refs;
                BoardGrid.Children.Add(refs.Root);
            }
        }
    }

    private ConnectFourCellRefs CreateCell(Position position)
    {
        var root = new Grid
        {
            Margin = new Thickness(4),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = position.Column
        };
        root.MouseEnter += Cell_MouseEnter;
        root.MouseLeave += Cell_MouseLeave;
        root.MouseLeftButtonDown += Cell_MouseLeftButtonDown;

        var slotBg = new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(59, 130, 246), 0),
                    new GradientStop(Color.FromRgb(37, 99, 235), 1)
                }
            },
            CornerRadius = new CornerRadius(8)
        };
        root.Children.Add(slotBg);

        var columnHoverOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(45, 147, 197, 253)),
            CornerRadius = new CornerRadius(8),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        root.Children.Add(columnHoverOverlay);

        var landingHint = new Ellipse
        {
            Width = 80,
            Height = 80,
            Fill = new SolidColorBrush(Color.FromArgb(70, 148, 163, 184)),
            Stroke = new SolidColorBrush(Color.FromArgb(160, 226, 232, 240)),
            StrokeThickness = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        root.Children.Add(landingHint);

        var ellipse = new Ellipse
        {
            Width = 80,
            Height = 80,
            Fill = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            StrokeThickness = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 4,
                ShadowDepth = 2,
                Opacity = 0.3
            }
        };

        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = ellipse
        };

        var viewboxContainer = new Grid();
        viewboxContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        viewboxContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Star) });
        viewboxContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        viewboxContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        viewboxContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8, GridUnitType.Star) });
        viewboxContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        viewboxContainer.Children.Add(viewbox);
        Grid.SetRow(viewbox, 1);
        Grid.SetColumn(viewbox, 1);

        root.Children.Add(viewboxContainer);

        return new ConnectFourCellRefs(root, slotBg, ellipse, landingHint, columnHoverOverlay);
    }

    private void Cell_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement { Tag: int column })
        {
            SetHoveredColumn(column);
        }
    }

    private void Cell_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        SetHoveredColumn(null);
    }

    private void Cell_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: int column })
        {
            _ = ExecuteMoveWithAnimationAsync(column, enforceHumanTurn: true);
        }
    }

    public async Task<bool> ExecuteMoveWithAnimationAsync(int column, bool enforceHumanTurn)
    {
        try
        {
            if (_isProcessingClick) return false;
            _isProcessingClick = true;

            SoundService.Instance.PlayClickSound();

            try
            {
                if (_game?.Board is not ConnectFourBoard board) return false;
                if (_game.IsGameOver() || _game.State != GameState.InProgress) return false;
                if (enforceHumanTurn && _humanPlayer.HasValue && _game.CurrentPlayer != _humanPlayer.Value) return false;

                var dropPosition = board.GetLowestEmptyPosition(column);
                if (!dropPosition.HasValue) return false;

                var movingPlayer = _game.CurrentPlayer;

                var move = new ConnectFourMove(column, movingPlayer);
                if (_game.MakeMove(move))
                {
                    await AnimateDropAsync(column, dropPosition.Value, movingPlayer);
                    SetHoveredColumn(null);
                    UpdateBoard();
                    _onMoveMade?.Invoke();
                    return true;
                }

                return false;
            }
            finally
            {
                _isProcessingClick = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка в ExecuteMoveWithAnimationAsync: {ex.Message}\n{ex.StackTrace}");
            _isProcessingClick = false;
            return false;
        }
    }

    public void UpdateBoard()
    {
        try
        {
            if (_game?.Board is not ConnectFourBoard board) return;
            _winningPositions = FindWinningPositions(board);

            foreach (var (position, refs) in _cells)
            {
                try
                {
                    var piece = board.GetPiece(position);

                    if (piece != null)
                    {
                        var gradient = new RadialGradientBrush();
                        if (piece.Owner == Player.Player1)
                        {
                            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(239, 68, 68), 0));
                            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(220, 38, 38), 1));
                        }
                        else
                        {
                            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(251, 191, 36), 0));
                            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(245, 158, 11), 1));
                        }
                        refs.Disc.Fill = gradient;
                        refs.Disc.Effect = new DropShadowEffect
                        {
                            Color = Colors.Black,
                            BlurRadius = 5,
                            ShadowDepth = 3,
                            Direction = 315,
                            Opacity = 0.6
                        };

                        if (_winningPositions.Contains(position))
                        {
                            refs.Disc.Stroke = new SolidColorBrush(Color.FromRgb(167, 243, 208));
                            refs.Disc.StrokeThickness = 4;
                            refs.SlotBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                            refs.SlotBorder.BorderThickness = new Thickness(2);
                        }
                        else
                        {
                            refs.Disc.Stroke = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                            refs.Disc.StrokeThickness = 2;
                            refs.SlotBorder.BorderThickness = new Thickness(0);
                        }

                        refs.LandingHint.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        refs.Disc.Fill = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                        refs.Disc.Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                        refs.Disc.StrokeThickness = 2;
                        refs.Disc.Effect = new DropShadowEffect
                        {
                            Color = Colors.Black,
                            BlurRadius = 4,
                            ShadowDepth = 2,
                            Opacity = 0.3
                        };
                        refs.SlotBorder.BorderThickness = new Thickness(0);

                        refs.LandingHint.Visibility = Visibility.Collapsed;
                    }

                    refs.ColumnHoverOverlay.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка при оновленні клітинки {position}: {ex.Message}");
                }
            }

            UpdateHoverState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка в UpdateBoard: {ex.Message}");
        }
    }

    private bool CanInteract()
    {
        return _game?.Board is ConnectFourBoard &&
               _game.State == GameState.InProgress &&
               !_game.IsGameOver() &&
               (!_humanPlayer.HasValue || _game.CurrentPlayer == _humanPlayer.Value);
    }

    private void SetHoveredColumn(int? column)
    {
        _hoveredColumn = column;
        _hoveredDropPosition = null;

        if (_hoveredColumn.HasValue &&
            _game?.Board is ConnectFourBoard board &&
            CanInteract())
        {
            _hoveredDropPosition = board.GetLowestEmptyPosition(_hoveredColumn.Value);
        }

        UpdateHoverState();
    }

    private void UpdateHoverState()
    {
        if (_game?.Board is not ConnectFourBoard board)
            return;

        foreach (var (position, refs) in _cells)
        {
            if (!CanInteract() || !_hoveredColumn.HasValue || position.Column != _hoveredColumn.Value)
            {
                refs.ColumnHoverOverlay.Visibility = Visibility.Collapsed;
                refs.LandingHint.Visibility = Visibility.Collapsed;
                continue;
            }

            refs.ColumnHoverOverlay.Visibility = Visibility.Visible;

            if (_hoveredDropPosition == position && board.GetPiece(position) == null)
            {
                refs.LandingHint.Visibility = Visibility.Visible;
                refs.LandingHint.Fill = CreatePreviewBrush(_game.CurrentPlayer);
            }
            else
            {
                refs.LandingHint.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static Brush CreatePreviewBrush(Player player)
    {
        var gradient = new RadialGradientBrush();
        if (player == Player.Player1)
        {
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(210, 248, 113, 113), 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(210, 220, 38, 38), 1));
        }
        else
        {
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(210, 252, 211, 77), 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(210, 245, 158, 11), 1));
        }

        return gradient;
    }

    private async Task AnimateDropAsync(int column, Position targetPosition, Player player)
    {
        if (!_cells.TryGetValue(new Position(0, column), out var sourceCell) ||
            !_cells.TryGetValue(targetPosition, out var targetCell))
        {
            return;
        }

        UpdateLayout();

        var sourcePoint = sourceCell.Root.TranslatePoint(
            new Point(sourceCell.Root.ActualWidth / 2, sourceCell.Root.ActualHeight / 2),
            BoardSurfaceGrid);
        var targetPoint = targetCell.Root.TranslatePoint(
            new Point(targetCell.Root.ActualWidth / 2, targetCell.Root.ActualHeight / 2),
            BoardSurfaceGrid);

        var animatedDisc = new Ellipse
        {
            Width = 80,
            Height = 80,
            Fill = player == Player.Player1
                ? new RadialGradientBrush(Color.FromRgb(239, 68, 68), Color.FromRgb(220, 38, 38))
                : new RadialGradientBrush(Color.FromRgb(251, 191, 36), Color.FromRgb(245, 158, 11)),
            Stroke = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            StrokeThickness = 2,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 6,
                ShadowDepth = 3,
                Opacity = 0.45
            },
            IsHitTestVisible = false
        };

        AnimationCanvas.Children.Add(animatedDisc);
        Canvas.SetLeft(animatedDisc, sourcePoint.X - 40);
        Canvas.SetTop(animatedDisc, sourcePoint.Y - 40);

        var tcs = new TaskCompletionSource<bool>();
        var duration = TimeSpan.FromMilliseconds(210 + Math.Abs(targetPosition.Row) * 40);

        var dropAnimation = new DoubleAnimation
        {
            From = sourcePoint.Y - 40,
            To = targetPoint.Y - 40,
            Duration = duration,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        dropAnimation.Completed += (_, _) =>
        {
            AnimationCanvas.Children.Remove(animatedDisc);
            tcs.TrySetResult(true);
        };

        animatedDisc.BeginAnimation(Canvas.TopProperty, dropAnimation);
        await tcs.Task;
    }

    private static HashSet<Position> FindWinningPositions(ConnectFourBoard board)
    {
        (int rowDir, int colDir)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };
        var result = new HashSet<Position>();

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var start = new Position(row, col);
                var piece = board.GetPiece(start);
                if (piece == null) continue;

                foreach (var (rowDir, colDir) in directions)
                {
                    var line = new List<Position> { start };
                    bool isWinningLine = true;

                    for (int i = 1; i < 4; i++)
                    {
                        var next = new Position(row + rowDir * i, col + colDir * i);
                        if (!board.IsValidPosition(next))
                        {
                            isWinningLine = false;
                            break;
                        }

                        var nextPiece = board.GetPiece(next);
                        if (nextPiece == null || nextPiece.Owner != piece.Owner)
                        {
                            isWinningLine = false;
                            break;
                        }

                        line.Add(next);
                    }

                    if (isWinningLine)
                    {
                        foreach (var pos in line)
                        {
                            result.Add(pos);
                        }
                    }
                }
            }
        }

        return result;
    }

    private sealed record ConnectFourCellRefs(
        Grid Root,
        Border SlotBorder,
        Ellipse Disc,
        Ellipse LandingHint,
        Border ColumnHoverOverlay
    );
}
