/**
 * @file: ChessBoardControl.xaml.cs
 * @description: Інтерактивна дошка для шахів з кліками мишкою
 * @dependencies: ChessGame, ChessMove, Position
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Chess;
using BoardGamesLibrary.Desktop.Services;

namespace BoardGamesLibrary.Desktop;

public partial class ChessBoardControl : UserControl
{
    private ChessGame? _game;
    private Action? _onMoveMade;
    private Position? _selectedPosition;
    private readonly Dictionary<Position, Border> _cells = new();
    private bool _isProcessingClick = false;

    public ChessBoardControl()
    {
        InitializeComponent();
    }

    public void Initialize(ChessGame game, Action onMoveMade)
    {
        _game = game;
        _onMoveMade = onMoveMade;
        _selectedPosition = null;
        _cells.Clear();
        BoardGrid.Children.Clear();
        
        CreateBoard();
        UpdateBoard();
    }

    private void CreateBoard()
    {
        for (int row = 7; row >= 0; row--)
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
            Background = new SolidColorBrush(isLight ? Color.FromRgb(238, 238, 210) : Color.FromRgb(118, 150, 86)),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 2,
                ShadowDepth = 1,
                Opacity = 0.1
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
                Opacity = 0.5
            }
        };

        cell.Child = textBlock;
        cell.MouseLeftButtonDown += (s, e) => OnCellClicked(position);
        
        return cell;
    }

    private void OnCellClicked(Position position)
    {
        // Блокуємо одночасні кліки
        if (_isProcessingClick) return;
        _isProcessingClick = true;
        
        // Відтворюємо звук кліку
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

            if (_selectedPosition == null)
            {
                // Вибираємо позицію
                var board = _game.Board as ChessBoard;
                if (board == null)
                {
                    _isProcessingClick = false;
                    return;
                }
                
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
                // Робимо хід
                if (position == _selectedPosition)
                {
                    // Скасовуємо вибір
                    ClearHighlights();
                    _selectedPosition = null;
                }
                else
                {
                    if (_game == null || _game.IsGameOver() || _game.State != GameState.InProgress)
                    {
                        ClearHighlights();
                        _selectedPosition = null;
                        _isProcessingClick = false;
                        return;
                    }
                    
                    try
                    {
                    var move = new ChessMove(_selectedPosition.Value, position, _game.CurrentPlayer);
                    if (_game.MakeMove(move))
                    {
                        ClearHighlights();
                        _selectedPosition = null;
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
                    else
                    {
                        // Невірний хід - вибираємо нову позицію
                        ClearHighlights();
                        var board = _game.Board as ChessBoard;
                        if (board != null)
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
                        else
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
            .OfType<ChessMove>()
                .Where(m => m.From == from)
                .ToList(); // Materialize to avoid multiple enumerations

        foreach (var move in validMoves)
            {
                try
        {
            HighlightCell(move.To, false);
                }
                catch
                {
                    // Ігноруємо помилки підсвітки окремих клітинок
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Помилка при підсвітці валідних ходів: {ex.Message}");
        }
    }

    private void HighlightCell(Position position, bool isSelected)
    {
        if (_cells.TryGetValue(position, out var cell))
        {
            if (isSelected)
            {
                cell.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                cell.BorderThickness = new Thickness(4);
                cell.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(251, 191, 36),
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };
                
                // Анімація вибору - плавне збільшення
                AnimateCellSelection(cell, true);
            }
            else
            {
                cell.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                cell.BorderThickness = new Thickness(3);
                cell.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(34, 197, 94),
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.6
                };
            }
        }
    }

    private void AnimateCellSelection(Border cell, bool isSelected)
    {
        // Встановлюємо RenderTransform для анімації
        if (cell.RenderTransform is not ScaleTransform scaleTransform)
        {
            scaleTransform = new ScaleTransform(1.0, 1.0);
            cell.RenderTransform = scaleTransform;
            cell.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        var storyboard = new System.Windows.Media.Animation.Storyboard();
        
        if (isSelected)
        {
            // Анімація збільшення при виборі
            var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.1,
                Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.1,
                Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnimation, scaleTransform);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleXProperty));
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnimation, scaleTransform);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleYProperty));
            
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
        }
        else
        {
            // Анімація повернення до нормального розміру
            var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.0,
                Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };
            var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.0,
                Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };
            
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnimation, scaleTransform);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleXProperty));
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnimation, scaleTransform);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleYProperty));
            
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
        }
        
        storyboard.Begin();
    }

    private void ClearHighlights()
    {
        foreach (var cell in _cells.Values)
        {
            cell.BorderBrush = Brushes.Transparent;
            cell.BorderThickness = new Thickness(0);
            cell.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 2,
                ShadowDepth = 1,
                Opacity = 0.1
            };
        }
    }

    private Position? _lastMoveFrom = null;
    private Position? _lastMoveTo = null;

    public void UpdateBoard()
    {
        try
        {
            if (_game?.Board is not ChessBoard board) return;

            // Зберігаємо інформацію про останній хід для анімації
            var previousState = new Dictionary<Position, string>();
            foreach (var (pos, cell) in _cells)
            {
                var textBlock = cell.Child as TextBlock;
                if (textBlock != null)
                {
                    previousState[pos] = textBlock.Text ?? "";
                }
            }

            ClearHighlights();
            _selectedPosition = null;

            // Знаходимо позиції, де з'явилася або зникла фігура
            Position? newPiecePosition = null;
            Position? removedPiecePosition = null;

            foreach (var (position, cell) in _cells)
            {
                try
                {
                    var piece = board.GetPiece(position);
                    var textBlock = cell.Child as TextBlock;
                    
                    if (textBlock != null)
                    {
                        var oldText = previousState.GetValueOrDefault(position, "");
                        var newText = piece?.Symbol ?? "";

                        if (piece != null)
                        {
                            textBlock.Text = piece.Symbol;
                            
                            // Визначаємо, чи клітинка світла чи темна
                            var isLight = (position.Row + position.Column) % 2 == 0;
                            
                            // Білі фігури (Player1) завжди світлі, чорні (Player2) завжди темні
                            // Для контрасту на різних клітинках використовуємо різні відтінки
                            if (piece.Owner == Player.Player1)
                            {
                                // Білі фігури: завжди світлі
                                textBlock.Foreground = isLight 
                                    ? new SolidColorBrush(Color.FromRgb(250, 250, 250)) // Білий на світлому бежевому
                                    : new SolidColorBrush(Color.FromRgb(255, 255, 255)); // Білий на темному зеленому
                            }
                            else
                            {
                                // Чорні фігури: завжди темні
                                textBlock.Foreground = isLight 
                                    ? new SolidColorBrush(Color.FromRgb(10, 10, 10)) // Чорний на світлому бежевому
                                    : new SolidColorBrush(Color.FromRgb(30, 30, 30)); // Темно-сірий на темному зеленому (трохи світліший для контрасту)
                            }

                            // Якщо фігура з'явилася на новому місці
                            if (oldText == "" && newText != "")
                            {
                                newPiecePosition = position;
                                // Анімація появи фігури
                                AnimatePieceAppearance(textBlock);
                            }
                        }
                        else
                        {
                            textBlock.Text = "";
                            
                            // Якщо фігура зникла
                            if (oldText != "" && newText == "")
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

            // Анімація руху фігури (якщо є інформація про переміщення)
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

    private void AnimatePieceAppearance(TextBlock textBlock)
    {
        // Анімація появи - плавне збільшення з прозорістю
        var scaleTransform = new ScaleTransform(0.5, 0.5);
        textBlock.RenderTransform = scaleTransform;
        textBlock.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        textBlock.Opacity = 0;

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
        System.Windows.Media.Animation.Storyboard.SetTarget(opacityAnimation, textBlock);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnimation, new System.Windows.PropertyPath(UIElement.OpacityProperty));

        storyboard.Children.Add(scaleXAnimation);
        storyboard.Children.Add(scaleYAnimation);
        storyboard.Children.Add(opacityAnimation);
        storyboard.Begin();
    }

    private void AnimatePieceMove(Position from, Position to)
    {
        // Анімація руху - підсвітка клітинок
        if (_cells.TryGetValue(from, out var fromCell) && _cells.TryGetValue(to, out var toCell))
        {
            // Анімація вихідної клітинки (зникнення)
            AnimateCellPulse(fromCell, false);
            
            // Анімація цільової клітинки (поява)
            AnimateCellPulse(toCell, true);
        }
    }

    private void AnimateCellPulse(Border cell, bool isTarget)
    {
        var originalBrush = cell.Background as SolidColorBrush;
        if (originalBrush == null) return;

        var pulseColor = isTarget 
            ? Color.FromRgb(34, 197, 94) // Зелений для цільової клітинки
            : Color.FromRgb(251, 191, 36); // Жовтий для вихідної клітинки

        var storyboard = new System.Windows.Media.Animation.Storyboard();
        
        // Анімація кольору фону
        var colorAnimation = new System.Windows.Media.Animation.ColorAnimation
        {
            From = ((SolidColorBrush)originalBrush).Color,
            To = pulseColor,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(200))
        };
        
        var reverseAnimation = new System.Windows.Media.Animation.ColorAnimation
        {
            From = pulseColor,
            To = ((SolidColorBrush)originalBrush).Color,
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
}

