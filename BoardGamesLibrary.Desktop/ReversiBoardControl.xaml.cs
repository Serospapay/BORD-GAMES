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
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Reversi;
using BoardGamesLibrary.Desktop.Services;

namespace BoardGamesLibrary.Desktop;

public partial class ReversiBoardControl : UserControl
{
    private ReversiGame? _game;
    private Action? _onMoveMade;
    private readonly Dictionary<Position, Border> _cells = new();
    private bool _isProcessingClick = false;

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
                var cell = CreateCell(position);
                _cells[position] = cell;
                BoardGrid.Children.Add(cell);
            }
        }
    }

    private Border CreateCell(Position position)
    {
        var isLight = (position.Row + position.Column) % 2 == 0;
        var cell = new Border
        {
            Background = new SolidColorBrush(isLight ? Color.FromRgb(34, 197, 94) : Color.FromRgb(16, 185, 129)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(6, 78, 59)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 2,
                ShadowDepth = 1,
                Opacity = 0.2
            }
        };

        var textBlock = new TextBlock
        {
            FontSize = 48,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Name = "PieceText",
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 3,
                ShadowDepth = 2,
                Opacity = 0.6
            }
        };

        cell.Child = textBlock;
        cell.MouseLeftButtonDown += (s, e) => OnCellClicked(position);
        
        return cell;
    }

    private void OnCellClicked(Position position)
    {
        try
        {
            if (_isProcessingClick) return;
            _isProcessingClick = true;
            
            // Відтворюємо звук кліку
            SoundService.Instance.PlayClickSound();
            
            try
            {
                if (_game == null) return;
                if (_game.IsGameOver() || _game.State != GameState.InProgress) return;

                var move = new ReversiMove(position, _game.CurrentPlayer);
                if (_game.MakeMove(move))
                {
                    UpdateBoard();
                    try
                    {
                        _onMoveMade?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Помилка в _onMoveMade: {ex.Message}");
                    }
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

            foreach (var (position, cell) in _cells)
            {
                try
                {
                    var piece = board.GetPiece(position);
                    var textBlock = cell.Child as TextBlock;
                    
                    if (textBlock != null)
                    {
                        if (piece != null)
                        {
                            textBlock.Text = piece.Symbol;
                            
                            // Визначаємо, чи клітинка світла чи темна
                            var isLight = (position.Row + position.Column) % 2 == 0;
                            
                            // Зелений фон (світлий: RGB 34, 197, 94, темний: RGB 16, 185, 129)
                            // На світлих клітинках - темний текст, на темних - світлий
                            if (piece.Owner == Player.Player1)
                            {
                                // Player1: темний текст на світлому, світлий на темному
                                textBlock.Foreground = isLight 
                                    ? new SolidColorBrush(Color.FromRgb(20, 20, 20)) // Майже чорний на світлому зеленому
                                    : new SolidColorBrush(Color.FromRgb(255, 255, 255)); // Білий на темному зеленому
                            }
                            else
                            {
                                // Player2: світлий текст на світлому, темний на темному (інвертовано для контрасту)
                                textBlock.Foreground = isLight 
                                    ? new SolidColorBrush(Color.FromRgb(255, 255, 255)) // Білий на світлому зеленому
                                    : new SolidColorBrush(Color.FromRgb(20, 20, 20)); // Майже чорний на темному зеленому
                            }
                        }
                        else
                        {
                            textBlock.Text = "";
                        }
                    }

                    // Підсвітка валідних ходів
                    if (piece == null && _game != null)
                    {
                        var testMove = new ReversiMove(position, _game.CurrentPlayer);
                        if (_game.IsValidMove(testMove))
                        {
                            cell.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                            cell.BorderThickness = new Thickness(4);
                            cell.Effect = new System.Windows.Media.Effects.DropShadowEffect
                            {
                                Color = Color.FromRgb(251, 191, 36),
                                BlurRadius = 12,
                                ShadowDepth = 0,
                                Opacity = 0.7
                            };
                        }
                        else
                        {
                            cell.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 78, 59));
                            cell.BorderThickness = new Thickness(1);
                            cell.Effect = new System.Windows.Media.Effects.DropShadowEffect
                            {
                                Color = Colors.Black,
                                BlurRadius = 2,
                                ShadowDepth = 1,
                                Opacity = 0.2
                            };
                        }
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

