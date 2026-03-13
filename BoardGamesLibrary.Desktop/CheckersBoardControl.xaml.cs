/**
 * @file: CheckersBoardControl.xaml.cs
 * @description: Інтерактивна дошка для шашок з кліками мишкою
 * @dependencies: CheckersGame, CheckersMove, Position
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Checkers;
using BoardGamesLibrary.Desktop.Services;

namespace BoardGamesLibrary.Desktop;

public partial class CheckersBoardControl : UserControl
{
    private CheckersGame? _game;
    private Action? _onMoveMade;
    private Position? _selectedPosition;
    private (Position From, Position To)? _lastMove;
    private readonly Dictionary<Position, CheckersCellRefs> _cells = new();
    private bool _isProcessingClick = false;

    private static readonly Color SquareLight = Color.FromRgb(255, 255, 221);
    private static readonly Color SquareDark = Color.FromRgb(118, 150, 86);
    private static readonly Color SquareSelected = Color.FromRgb(246, 246, 105);
    private static readonly Color SquareLastMove = Color.FromRgb(205, 210, 106);

    public CheckersBoardControl()
    {
        InitializeComponent();
    }

    public void Initialize(CheckersGame game, Action onMoveMade)
    {
        _game = game;
        _onMoveMade = onMoveMade;
        _selectedPosition = null;
        _lastMove = null;
        _cells.Clear();
        BoardGrid.Children.Clear();

        CreateBoard();
        UpdateBoard();
    }

    private void CreateBoard()
    {
        var board = _game?.Board as CheckersBoard;
        if (board == null) return;

        for (int row = 7; row >= 0; row--)
        {
            for (int col = 0; col < 8; col++)
            {
                var position = new Position(row, col);
                var refs = CreateCell(position, board.IsDarkSquare(position));
                _cells[position] = refs;
                BoardGrid.Children.Add(refs.Root);
            }
        }
    }

    private CheckersCellRefs CreateCell(Position position, bool isDark)
    {
        var baseColor = isDark ? SquareDark : SquareLight;

        var root = new Grid
        {
            Cursor = isDark ? Cursors.Hand : Cursors.Arrow
        };

        var baseBg = new Border
        {
            Background = new SolidColorBrush(baseColor)
        };
        root.Children.Add(baseBg);

        var selectedOverlay = new Border
        {
            Background = new SolidColorBrush(SquareSelected),
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(selectedOverlay);

        var lastMoveOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(120, 205, 210, 106)),
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(lastMoveOverlay);

        var validMoveEllipse = new Ellipse
        {
            Width = 25,
            Height = 25,
            Fill = new SolidColorBrush(Color.FromArgb(76, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(validMoveEllipse);

        var hoverOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)),
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(hoverOverlay);

        Grid? pieceContainer = null;
        Ellipse? pieceDisc = null;
        TextBlock? kingMark = null;
        if (isDark)
        {
            pieceDisc = new Ellipse
            {
                Width = 66,
                Height = 66,
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 5,
                    ShadowDepth = 3,
                    Direction = 315,
                    Opacity = 0.6
                }
            };

            kingMark = new TextBlock
            {
                Text = "K",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 64)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };

            pieceContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 72,
                Height = 72,
                Visibility = Visibility.Collapsed
            };
            pieceContainer.Children.Add(pieceDisc);
            pieceContainer.Children.Add(kingMark);

            var viewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = pieceContainer
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

            root.MouseLeftButtonDown += (s, e) => OnCellClicked(position);
        }

        root.MouseEnter += (s, e) => { if (hoverOverlay != null) hoverOverlay.Visibility = Visibility.Visible; };
        root.MouseLeave += (s, e) => { if (hoverOverlay != null) hoverOverlay.Visibility = Visibility.Collapsed; };

        return new CheckersCellRefs(root, baseBg, selectedOverlay, lastMoveOverlay, validMoveEllipse, pieceContainer, pieceDisc, kingMark, baseColor, isDark);
    }

    private void OnCellClicked(Position position)
    {
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        SoundService.Instance.PlayClickSound();

        try
        {
            if (_game == null)
            {
                _isProcessingClick = false;
                return;
            }

            if (_game.IsGameOver() || _game.State != GameState.InProgress)
            {
                _isProcessingClick = false;
                return;
            }

            var board = _game.Board as CheckersBoard;
            if (board == null || !board.IsDarkSquare(position))
            {
                _isProcessingClick = false;
                return;
            }

            if (_selectedPosition == null)
            {
                try
                {
                    var piece = board.GetPiece(position);
                    if (piece != null && piece.Owner == _game.CurrentPlayer)
                    {
                        _selectedPosition = position;
                        HighlightCell(position, true);
                        HighlightValidMoves(position);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка при отриманні фігури: {ex.Message}");
                }
            }
            else
            {
                if (position == _selectedPosition)
                {
                    ClearHighlights();
                    _selectedPosition = null;
                }
                else
                {
                    try
                    {
                        var selectedMove = _game.GetValidMoves(_game.CurrentPlayer)
                            .OfType<CheckersMove>()
                            .FirstOrDefault(m => m.From == _selectedPosition.Value && m.To == position);

                        if (selectedMove != null && _game.MakeMove(selectedMove))
                        {
                            _lastMove = (_selectedPosition.Value, position);
                            ClearHighlights();
                            _selectedPosition = null;
                            UpdateBoard();
                            _onMoveMade?.Invoke();
                        }
                        else
                        {
                            ClearHighlights();
                            try
                            {
                                var piece = board.GetPiece(position);
                                if (piece != null && piece.Owner == _game.CurrentPlayer)
                                {
                                    _selectedPosition = position;
                                    HighlightCell(position, true);
                                    HighlightValidMoves(position);
                                }
                                else
                                {
                                    _selectedPosition = null;
                                }
                            }
                            catch
                            {
                                _selectedPosition = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Помилка при виконанні ходу: {ex.Message}");
                        ClearHighlights();
                        _selectedPosition = null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка в OnCellClicked: {ex.Message}\n{ex.StackTrace}");
            ClearHighlights();
            _selectedPosition = null;
        }
        finally
        {
            _isProcessingClick = false;
        }
    }

    private void HighlightValidMoves(Position from)
    {
        if (_game == null) return;

        try
        {
            var validMoves = _game.GetValidMoves(_game.CurrentPlayer)
                .OfType<CheckersMove>()
                .Where(m => m.From == from)
                .ToList();

            foreach (var move in validMoves)
            {
                try
                {
                    HighlightCell(move.To, false);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка при підсвітці валідних ходів: {ex.Message}");
        }
    }

    private void HighlightCell(Position position, bool isSelected)
    {
        if (!_cells.TryGetValue(position, out var refs) || !refs.IsDark) return;

        if (isSelected)
        {
            refs.SelectedOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            refs.ValidMoveEllipse.Visibility = Visibility.Visible;
        }
    }

    private void ClearHighlights()
    {
        foreach (var refs in _cells.Values)
        {
            refs.SelectedOverlay.Visibility = Visibility.Collapsed;
            refs.ValidMoveEllipse.Visibility = Visibility.Collapsed;
        }
    }

    public void UpdateBoard()
    {
        try
        {
            if (_game?.Board is not CheckersBoard board) return;

            var previousState = new Dictionary<Position, bool>();
            foreach (var (pos, refs) in _cells)
            {
                if (refs.IsDark && refs.PieceContainer != null)
                {
                    previousState[pos] = refs.PieceContainer.Visibility == Visibility.Visible;
                }
            }

            ClearHighlights();
            _selectedPosition = null;

            Position? newPiecePosition = null;
            Position? removedPiecePosition = null;

            foreach (var (position, refs) in _cells)
            {
                try
                {
                    refs.LastMoveOverlay.Visibility = _lastMove.HasValue &&
                        (position == _lastMove.Value.From || position == _lastMove.Value.To)
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    if (refs.IsDark && refs.PieceContainer != null && refs.PieceDisc != null && refs.KingMark != null)
                    {
                        var piece = board.GetPiece(position);
                        var wasOccupied = previousState.GetValueOrDefault(position, false);
                        var isOccupied = piece != null;

                        if (piece != null)
                        {
                            if (piece.Owner == Player.Player1)
                            {
                                refs.PieceDisc.Fill = new RadialGradientBrush(
                                    Color.FromRgb(251, 113, 133),
                                    Color.FromRgb(190, 24, 93));
                                refs.PieceDisc.Stroke = new SolidColorBrush(Color.FromRgb(136, 19, 55));
                            }
                            else
                            {
                                refs.PieceDisc.Fill = new RadialGradientBrush(
                                    Color.FromRgb(248, 250, 252),
                                    Color.FromRgb(148, 163, 184));
                                refs.PieceDisc.Stroke = new SolidColorBrush(Color.FromRgb(71, 85, 105));
                            }
                            refs.KingMark.Visibility = piece is CheckersPiece cp && cp.IsKing
                                ? Visibility.Visible
                                : Visibility.Collapsed;
                            refs.PieceContainer.Visibility = Visibility.Visible;

                            if (!wasOccupied && isOccupied)
                            {
                                newPiecePosition = position;
                                AnimatePieceAppearance(refs.PieceContainer);
                            }
                        }
                        else
                        {
                            refs.KingMark.Visibility = Visibility.Collapsed;
                            refs.PieceContainer.Visibility = Visibility.Collapsed;
                            if (wasOccupied && !isOccupied)
                            {
                                removedPiecePosition = position;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка при оновленні клітинки {position}: {ex.Message}");
                }
            }

            if (removedPiecePosition.HasValue && newPiecePosition.HasValue)
            {
                AnimatePieceMove(removedPiecePosition.Value, newPiecePosition.Value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка в UpdateBoard: {ex.Message}");
        }
    }

    private void AnimatePieceAppearance(UIElement pieceElement)
    {
        var scaleTransform = new ScaleTransform(0.5, 0.5);
        pieceElement.RenderTransform = scaleTransform;
        pieceElement.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        pieceElement.Opacity = 0;

        var storyboard = new System.Windows.Media.Animation.Storyboard();

        var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.5,
            To = 1.0,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new System.Windows.Media.Animation.ElasticEase
            {
                Oscillations = 1,
                Springiness = 3,
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            }
        };
        var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.5,
            To = 1.0,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new System.Windows.Media.Animation.ElasticEase
            {
                Oscillations = 1,
                Springiness = 3,
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            }
        };
        var opacityAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(300))
        };

        System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnimation, scaleTransform);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleXProperty));
        System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnimation, scaleTransform);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleYProperty));
        System.Windows.Media.Animation.Storyboard.SetTarget(opacityAnimation, pieceElement);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnimation, new System.Windows.PropertyPath(UIElement.OpacityProperty));

        storyboard.Children.Add(scaleXAnimation);
        storyboard.Children.Add(scaleYAnimation);
        storyboard.Children.Add(opacityAnimation);
        storyboard.Begin();
    }

    private void AnimatePieceMove(Position from, Position to)
    {
        if (_cells.TryGetValue(from, out var fromRefs) && _cells.TryGetValue(to, out var toRefs))
        {
            AnimateCellPulse(fromRefs.BaseBg, false);
            AnimateCellPulse(toRefs.BaseBg, true);
        }
    }

    private void AnimateCellPulse(Border cell, bool isTarget)
    {
        if (cell.Background is not SolidColorBrush originalBrush) return;

        var pulseColor = isTarget
            ? Color.FromRgb(34, 197, 94)
            : Color.FromRgb(251, 191, 36);

        var storyboard = new System.Windows.Media.Animation.Storyboard();

        var colorAnimation = new System.Windows.Media.Animation.ColorAnimation
        {
            From = originalBrush.Color,
            To = pulseColor,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(200))
        };

        var reverseAnimation = new System.Windows.Media.Animation.ColorAnimation
        {
            From = pulseColor,
            To = originalBrush.Color,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(200)),
            BeginTime = TimeSpan.FromMilliseconds(200)
        };

        System.Windows.Media.Animation.Storyboard.SetTarget(colorAnimation, originalBrush);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(colorAnimation, new System.Windows.PropertyPath(SolidColorBrush.ColorProperty));
        System.Windows.Media.Animation.Storyboard.SetTarget(reverseAnimation, originalBrush);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(reverseAnimation, new System.Windows.PropertyPath(SolidColorBrush.ColorProperty));

        storyboard.Children.Add(colorAnimation);
        storyboard.Children.Add(reverseAnimation);
        storyboard.Begin();
    }

    private sealed record CheckersCellRefs(
        Grid Root,
        Border BaseBg,
        Border SelectedOverlay,
        Border LastMoveOverlay,
        Ellipse ValidMoveEllipse,
        Grid? PieceContainer,
        Ellipse? PieceDisc,
        TextBlock? KingMark,
        Color BaseColor,
        bool IsDark
    );
}
