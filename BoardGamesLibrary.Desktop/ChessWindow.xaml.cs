/**
 * @file: ChessWindow.xaml.cs
 * @description: Вікно для гри в шахи
 * @dependencies: ChessGame, ChessBoardControl, ILogger
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private readonly StatisticsService _statisticsService;
    private int _moveCount = 0;
    private bool _gameResultRecorded = false;

    public ChessWindow()
    {
        InitializeComponent();
        
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        var fileLogger = new FileLogger("logs", "chess.log", LogLevel.Debug);
        _logger = new CompositeLogger(consoleLogger, fileLogger);
        _statisticsService = new StatisticsService();
        
        Loaded += ChessWindow_Loaded;
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
        ChessBoard.Initialize(_game, OnMoveMade);
        _moveCount = 0;
        _gameResultRecorded = false;
        
        UpdateUI();
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
                CurrentPlayerText.Text = $"Хід гравця: {_game.CurrentPlayer}";
            
            if (StatusText != null)
            {
                if (_game.IsGameOver())
                {
                    var winner = _game.GetWinner();
                    if (winner.HasValue)
                    {
                        StatusText.Text = $"Переміг гравець: {winner.Value}!";
                        AnimateVictory(StatusText);
                    }
                    else
                    {
                        StatusText.Text = "Нічия!";
                        AnimateDraw(StatusText);
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
}

