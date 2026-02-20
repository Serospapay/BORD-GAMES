/**
 * @file: ReversiBoardControl.xaml.cs
 * @description: Інтерактивна дошка для Reversi з кліками мишкою
 * @dependencies: ReversiGame, ReversiMove, Position
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Reversi;
using BoardGamesLibrary.Desktop.Services;

namespace BoardGamesLibrary.Desktop;

public partial class ReversiBoardControl : UserControl
{
    private ReversiGame? _game;
    private Action? _onMoveMade;
    private readonly Dictionary<Position, ReversiCellRefs> _cells = new();
    private bool _isProcessingClick = false;

    private static readonly Color SquareLight = Color.FromRgb(255, 255, 221);
    private static readonly Color SquareDark = Color.FromRgb(118, 150, 86);

    public ReversiBoardControl()
    {
        InitializeComponent();
    }

    public void Initialize(ReversiGame game, Action onMoveMade)
    {
        _game = game;
        _onMoveMade = onMoveMade;
        _cells.Clear();
        BoardGrid.Children.Clear();

        CreateBoard();
        UpdateBoard();
    }

    private void CreateBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var position = new Position(row, col);
                var refs = CreateCell(position);
                _cells[position] = refs;
                BoardGrid.Children.Add(refs.Root);
            }
        }
    }

    private ReversiCellRefs CreateCell(Position position)
    {
        var isLight = (position.Row + position.Column) % 2 == 0;
        var baseColor = isLight ? SquareLight : SquareDark;

        var root = new Grid { Cursor = Cursors.Hand };

        var baseBg = new Border
        {
            Background = new SolidColorBrush(baseColor)
        };
        root.Children.Add(baseBg);

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

        var pieceText = new TextBlock
        {
            FontSize = 72,
            FontWeight = FontWeights.Bold,
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

        var viewbox = new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = pieceText
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
        root.MouseEnter += (s, e) => hoverOverlay.Visibility = Visibility.Visible;
        root.MouseLeave += (s, e) => hoverOverlay.Visibility = Visibility.Collapsed;

        return new ReversiCellRefs(root, validMoveEllipse, pieceText);
    }

    private void OnCellClicked(Position position)
    {
        try
        {
            if (_isProcessingClick) return;
            _isProcessingClick = true;

            SoundService.Instance.PlayClickSound();

            try
            {
                if (_game == null) return;
                if (_game.IsGameOver() || _game.State != GameState.InProgress) return;

                var move = new ReversiMove(position, _game.CurrentPlayer);
                if (_game.MakeMove(move))
                {
                    UpdateBoard();
                    _onMoveMade?.Invoke();
                }
            }
            finally
            {
                _isProcessingClick = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка в OnCellClicked: {ex.Message}\n{ex.StackTrace}");
            _isProcessingClick = false;
        }
    }

    public void UpdateBoard()
    {
        try
        {
            if (_game?.Board is not ReversiBoard board) return;

            foreach (var (position, refs) in _cells)
            {
                try
                {
                    var piece = board.GetPiece(position);
                    var isLight = (position.Row + position.Column) % 2 == 0;

                    if (piece != null)
                    {
                        refs.PieceText.Text = piece.Symbol;

                        if (piece.Owner == Player.Player1)
                        {
                            refs.PieceText.Foreground = isLight
                                ? new SolidColorBrush(Color.FromRgb(20, 20, 20))
                                : new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        }
                        else
                        {
                            refs.PieceText.Foreground = isLight
                                ? new SolidColorBrush(Color.FromRgb(255, 255, 255))
                                : new SolidColorBrush(Color.FromRgb(20, 20, 20));
                        }

                        refs.ValidMoveEllipse.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        refs.PieceText.Text = "";

                        var testMove = new ReversiMove(position, _game.CurrentPlayer);
                        refs.ValidMoveEllipse.Visibility = _game.IsValidMove(testMove)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка при оновленні клітинки {position}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка в UpdateBoard: {ex.Message}");
        }
    }

    private sealed record ReversiCellRefs(
        Grid Root,
        Ellipse ValidMoveEllipse,
        TextBlock PieceText
    );
}
