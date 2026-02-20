/**
 * @file: ChessWindow.xaml.cs
 * @description: Вікно для гри в шахи
 * @dependencies: ChessGame, ChessBoardControl, ILogger
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Chess;
using BoardGamesLibrary.Logging;
using BoardGamesLibrary.Desktop.Services;
using static BoardGamesLibrary.Core.GameState;

namespace BoardGamesLibrary.Desktop;

public partial class ChessWindow : Window
{
    private ChessGame? _game;
    private readonly ILogger _logger;
    private readonly FileLogger _fileLogger;
    private readonly StatisticsService _statisticsService;
    private readonly bool _playVsAI;
    private readonly AIDifficulty _aiDifficulty;
    private ChessAIPlayer? _aiPlayer;
    private int _moveCount = 0;
    private bool _gameResultRecorded = false;
    private DispatcherTimer? _gameTimer;
    private DateTime _gameStartTime;
    private bool _endgameDialogShown = false;
    private bool _endgameEffectsPlayed = false;

    public ChessWindow(bool playVsAI = false, AIDifficulty aiDifficulty = AIDifficulty.Medium)
    {
        InitializeComponent();
        _playVsAI = playVsAI;
        _aiDifficulty = aiDifficulty;
        
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        _fileLogger = new FileLogger("logs", "chess.log", LogLevel.Debug);
        _logger = new CompositeLogger(consoleLogger, _fileLogger);
        _statisticsService = new StatisticsService();
        
        Loaded += ChessWindow_Loaded;
        Closed += (s, e) => _fileLogger.Dispose();
    }
    
    private void ChessWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StartNewGame();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Критична помилка в ChessWindow_Loaded", ex);
            MessageBox.Show($"Помилка ініціалізації гри: {ex.Message}", 
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void StartNewGame()
    {
        _game = new ChessGame(_logger);
        _game.StartNewGame();
        _aiPlayer = _playVsAI ? new ChessAIPlayer(Player.Player2, _aiDifficulty, _logger) : null;
        ChessBoard.Initialize(_game, OnMoveMade);
        _moveCount = 0;
        _gameResultRecorded = false;
        _endgameDialogShown = false;
        _endgameEffectsPlayed = false;
        _gameStartTime = DateTime.Now;
        StartGameTimer();
        
        UpdateUI();
        TryAIMove();
    }

    private void StartGameTimer()
    {
        _gameTimer?.Stop();
        _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _gameTimer.Tick += (s, e) => UpdateUI();
        _gameTimer.Start();
    }

    private void TryAIMove()
    {
        if (_game == null || _aiPlayer == null || _game.IsGameOver() || _game.State != GameState.InProgress)
            return;
        if (_game.CurrentPlayer != _aiPlayer.Player)
            return;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            var move = _aiPlayer.ChooseMove(_game);
            if (move != null && _game.MakeMove(move))
            {
                ChessBoard.UpdateBoard();
                OnMoveMade();
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void OnMoveMade()
    {
        try
        {
            if (_game == null || Dispatcher.HasShutdownStarted) return;
            if (_game.State != GameState.InProgress && !_game.IsGameOver()) return;
            
            _moveCount++;
            
            // Відтворюємо звук ходу
            SoundService.Instance.PlayMoveSound();
            
            // Записуємо результат гри, якщо вона завершилася
            if (_game.IsGameOver() && !_gameResultRecorded)
            {
                _gameResultRecorded = true;
                var winner = _game.GetWinner();
                _statisticsService.RecordGameResult("Шахи", winner, _moveCount);
            }
            
            UpdateUI();
            TryAIMove();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Помилка в OnMoveMade", ex);
        }
    }

    private void UpdateUI()
    {
        try
        {
            if (_game == null || Dispatcher.HasShutdownStarted) return;
            
            if (Dispatcher.CheckAccess())
            {
                UpdateUIInternal();
            }
            else
            {
                Dispatcher.InvokeAsync(UpdateUIInternal);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Помилка в UpdateUI", ex);
        }
    }

    private void AnimateVictory(TextBlock textBlock)
    {
        // Відтворюємо звук перемоги
        SoundService.Instance.PlayVictorySound();
        
        // Анімація перемоги - пульсація та збільшення
        var scaleTransform = new ScaleTransform(1.0, 1.0);
        textBlock.RenderTransform = scaleTransform;
        textBlock.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        
        // Змінюємо колір на золотий
        textBlock.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(251, 191, 36));

        var storyboard = new System.Windows.Media.Animation.Storyboard();
        
        // Анімація збільшення
        var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 1.3,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(500)),
            EasingFunction = new System.Windows.Media.Animation.ElasticEase 
            { 
                Oscillations = 2,
                Springiness = 4,
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            },
            AutoReverse = true,
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };
        var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 1.3,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(500)),
            EasingFunction = new System.Windows.Media.Animation.ElasticEase 
            { 
                Oscillations = 2,
                Springiness = 4,
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            },
            AutoReverse = true,
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };

        System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnimation, scaleTransform);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleXProperty));
        System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnimation, scaleTransform);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleYProperty));

        storyboard.Children.Add(scaleXAnimation);
        storyboard.Children.Add(scaleYAnimation);
        storyboard.Begin();
    }

    private void AnimateDraw(TextBlock textBlock)
    {
        // Відтворюємо звук нічиєї
        SoundService.Instance.PlayDrawSound();
        
        // Анімація нічиї - легка пульсація
        var scaleTransform = new ScaleTransform(1.0, 1.0);
        textBlock.RenderTransform = scaleTransform;
        textBlock.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        
        // Змінюємо колір на сірий
        textBlock.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(148, 163, 184));

        var storyboard = new System.Windows.Media.Animation.Storyboard();
        
        // Легка пульсація
        var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 1.1,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(800)),
            EasingFunction = new System.Windows.Media.Animation.CubicEase 
            { 
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut
            },
            AutoReverse = true,
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };
        var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 1.1,
            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(800)),
            EasingFunction = new System.Windows.Media.Animation.CubicEase 
            { 
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut
            },
            AutoReverse = true,
            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
        };

        System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnimation, scaleTransform);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleXProperty));
        System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnimation, scaleTransform);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnimation, new System.Windows.PropertyPath(ScaleTransform.ScaleYProperty));

        storyboard.Children.Add(scaleXAnimation);
        storyboard.Children.Add(scaleYAnimation);
        storyboard.Begin();
    }
    
    private void UpdateUIInternal()
    {
        try
        {
            if (_game == null || Dispatcher.HasShutdownStarted) return;

            if (CurrentPlayerText != null)
                CurrentPlayerText.Text = $"Хід: {GetPlayerDisplayName(_game.CurrentPlayer)}";
            
            if (_game.IsGameOver())
            {
                _gameTimer?.Stop();
                HandleGameOverDialog();
            }
            if (MoveCountText != null)
                MoveCountText.Text = $"Ходів: {_moveCount}";
            if (GameTimeText != null)
            {
                var elapsed = DateTime.Now - _gameStartTime;
                GameTimeText.Text = $"Час: {elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            }
            if (MaterialText != null && _game.Board is ChessBoard board)
            {
                var (p1, p2) = CountChessPieces(board);
                MaterialText.Text = $"Г1: {p1} фігур | Г2: {p2} фігур";
            }
            
            if (StatusText != null)
            {
                if (_game.IsGameOver())
                {
                    var winner = _game.GetWinner();
                    if (winner.HasValue)
                    {
                        StatusText.Text = _playVsAI
                            ? (winner.Value == Player.Player1 ? "Перемога!" : "Поразка!")
                            : $"Переміг: {GetPlayerDisplayName(winner.Value)}";
                        if (!_endgameEffectsPlayed)
                        {
                            AnimateVictory(StatusText);
                            _endgameEffectsPlayed = true;
                        }
                    }
                    else
                    {
                        StatusText.Text = "Нічия!";
                        if (!_endgameEffectsPlayed)
                        {
                            AnimateDraw(StatusText);
                            _endgameEffectsPlayed = true;
                        }
                    }
                }
                else
                {
                    StatusText.Text = "Гра в процесі";
                    // Скидаємо анімації, якщо гра продовжується
                    if (StatusText.RenderTransform is ScaleTransform)
                    {
                        StatusText.RenderTransform = null;
                    }
                    StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(148, 163, 184));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Помилка в UpdateUIInternal", ex);
        }
    }

    private void HandleGameOverDialog()
    {
        if (_game == null || _endgameDialogShown)
            return;

        _endgameDialogShown = true;
        var winner = _game.GetWinner();
        var resultText = winner switch
        {
            null => "Нічия!",
            _ when _playVsAI && winner.Value == Player.Player1 => "Перемога!",
            _ when _playVsAI && winner.Value == Player.Player2 => "Поразка!",
            _ => $"Переміг: {GetPlayerDisplayName(winner.Value)}"
        };

        var choice = MessageBox.Show(
            this,
            $"{resultText}\n\nЗіграти ще раз?\n\nТак — нова гра\nНі — повернутися в меню",
            "Гру завершено",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (choice == MessageBoxResult.Yes)
        {
            StartNewGame();
        }
        else
        {
            Close();
        }
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
    }

    private void RulesButton_Click(object sender, RoutedEventArgs e)
    {
        var rulesText = GetChessRules();
        var rulesDialog = new RulesDialog("Шахи", rulesText)
        {
            Owner = this
        };
        rulesDialog.ShowDialog();
    }

    private string GetChessRules()
    {
        return @"ШАХИ - Класична стратегічна гра для двох гравців.

ЦІЛЬ ГРИ:
Мета - поставити мат королю супротивника (загроза взяття, від якої неможливо захиститися).

ПОЧАТКОВА ПОЗИЦІЯ:
• Білі фігури на рядках 1-2, чорні на рядках 7-8
• Король та королева в центрі, тури по кутах
• Коні біля тур, слони біля коней, пішаки попереду

ХОДИ ФІГУР:

♔ КОРОЛЬ - ходить на одну клітинку в будь-якому напрямку
♕ КОРОЛЕВА - ходить по діагоналі, горизонталі та вертикалі на будь-яку відстань
♖ ТУРА - ходить по горизонталі та вертикалі на будь-яку відстань
♗ СЛОН - ходить по діагоналі на будь-яку відстань
♘ КІНЬ - ходить буквою 'Г' (2 клітинки в одному напрямку, 1 в перпендикулярному)
♙ ПІШАК - ходить на 1 клітинку вперед (на першому ході можна на 2), б'є по діагоналі

СПЕЦІАЛЬНІ ПРАВИЛА:

• РОКІРОВКА - король і тура роблять хід одночасно (король на 2 клітинки, тура через короля)
• ВЗЯТТЯ НА ПРОХОДІ - пішак може взяти пішака супротивника, який пройшов повз нього
• ПЕРЕТВОРЕННЯ ПІШАКА - коли пішак доходить до кінця дошки, він перетворюється на будь-яку фігуру (зазвичай королеву)

ПРИКЛАДИ ХОДІВ:

1. Пішак e2-e4 (білий пішак з e2 на e4)
2. Конь g1-f3 (білий кінь з g1 на f3)
3. Слон f1-c4 (білий слон з f1 на c4)
4. Королева d1-f3 (біла королева з d1 на f3)

ШАХ І МАТ:
• ШАХ - король під атакою, але може втекти або захиститися
• МАТ - король під атакою і не може втекти - гра закінчується
• ПАТ - гравець не може зробити хід, але король не під атакою - нічия";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private static (int p1, int p2) CountChessPieces(ChessBoard board)
    {
        int p1 = 0, p2 = 0;
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
            {
                var piece = board.GetPiece(new Position(r, c));
                if (piece != null)
                {
                    if (piece.Owner == Player.Player1) p1++;
                    else p2++;
                }
            }
        return (p1, p2);
    }

    private static string GetPlayerDisplayName(Player player)
    {
        return player switch
        {
            Player.Player1 => "Гравець 1",
            Player.Player2 => "Гравець 2",
            _ => "Невідомий гравець"
        };
    }
}

