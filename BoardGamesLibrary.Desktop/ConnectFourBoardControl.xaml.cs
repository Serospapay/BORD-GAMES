/**
 * @file: ConnectFourBoardControl.xaml.cs
 * @description: Інтерактивна дошка для Connect Four з кліками мишкою
 * @dependencies: ConnectFourGame, ConnectFourMove
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
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
    private readonly Dictionary<Position, Ellipse> _cells = new();
    private bool _isProcessingClick = false;

    public ConnectFourBoardControl()
    {
        InitializeComponent();
    }

    public void Initialize(ConnectFourGame game, Action onMoveMade)
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
        for (int row = 5; row >= 0; row--)
        {
            for (int col = 0; col < 7; col++)
            {
                var position = new Position(row, col);
                var (root, ellipse) = CreateCell(position);
                _cells[position] = ellipse;
                BoardGrid.Children.Add(root);
            }
        }
    }

    private (Grid Root, Ellipse Ellipse) CreateCell(Position position)
    {
        var root = new Grid
        {
            Margin = new Thickness(4)
        };

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

        return (root, ellipse);
    }

    private void ColumnButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int column)
        {
            MakeMove(column);
        }
    }

    private void MakeMove(int column)
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

                var move = new ConnectFourMove(column, _game.CurrentPlayer);
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
            System.Diagnostics.Debug.WriteLine($"Помилка в MakeMove: {ex.Message}\n{ex.StackTrace}");
            _isProcessingClick = false;
        }
    }

    public void UpdateBoard()
    {
        try
        {
            if (_game?.Board is not ConnectFourBoard board) return;

            foreach (var (position, ellipse) in _cells)
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
                        ellipse.Fill = gradient;
                        ellipse.Effect = new DropShadowEffect
                        {
                            Color = Colors.Black,
                            BlurRadius = 5,
                            ShadowDepth = 3,
                            Direction = 315,
                            Opacity = 0.6
                        };
                    }
                    else
                    {
                        ellipse.Fill = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                        ellipse.Effect = new DropShadowEffect
                        {
                            Color = Colors.Black,
                            BlurRadius = 4,
                            ShadowDepth = 2,
                            Opacity = 0.3
                        };
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
}
