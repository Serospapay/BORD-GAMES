/**
 * @file: CheckersWindow.xaml.cs
 * @description: Вікно для гри в шашки
 * @dependencies: CheckersGame, CheckersBoardControl, ILogger
 * @created: 2024-12-19
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Checkers;
using BoardGamesLibrary.Logging;
using BoardGamesLibrary.Desktop.Services;
using static BoardGamesLibrary.Core.GameState;

namespace BoardGamesLibrary.Desktop;

public partial class CheckersWindow : Window
{
    private CheckersGame? _game;
    private readonly ILogger _logger;
    private readonly StatisticsService _statisticsService;
    private int _moveCount = 0;
    private bool _gameResultRecorded = false;

    public CheckersWindow()
    {
        InitializeComponent();
        
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        var fileLogger = new FileLogger("logs", "checkers.log", LogLevel.Debug);
        _logger = new CompositeLogger(consoleLogger, fileLogger);
        _statisticsService = new StatisticsService();
        
        Loaded += CheckersWindow_Loaded;
    }
    
    private void CheckersWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StartNewGame();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Критична помилка в CheckersWindow_Loaded", ex);
            MessageBox.Show($"Помилка ініціалізації гри: {ex.Message}", 
                "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void StartNewGame()
    {
        _game = new CheckersGame(_logger);
        _game.StartNewGame();
        CheckersBoard.Initialize(_game, OnMoveMade);
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
                _statisticsService.RecordGameResult("Шашки", winner, _moveCount);
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

    private void AnimateVictory(TextBlock textBlock)
    {
        // Відтворюємо звук перемоги
        SoundService.Instance.PlayVictorySound();
        
        var scaleTransform = new ScaleTransform(1.0, 1.0);
        textBlock.RenderTransform = scaleTransform;
        textBlock.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        textBlock.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(251, 191, 36));

        var storyboard = new System.Windows.Media.Animation.Storyboard();
        
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
        
        var scaleTransform = new ScaleTransform(1.0, 1.0);
        textBlock.RenderTransform = scaleTransform;
        textBlock.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        textBlock.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(148, 163, 184));

        var storyboard = new System.Windows.Media.Animation.Storyboard();
        
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

    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
    }

    private void RulesButton_Click(object sender, RoutedEventArgs e)
    {
        var rulesText = GetCheckersRules();
        var rulesDialog = new RulesDialog("Шашки", rulesText)
        {
            Owner = this
        };
        rulesDialog.ShowDialog();
    }

    private string GetCheckersRules()
    {
        return @"ШАШКИ - Класична гра для двох гравців на дошці 8x8.

ЦІЛЬ ГРИ:
Мета - з'їсти всі шашки супротивника або заблокувати їх, щоб вони не могли ходити.

ПОЧАТКОВА ПОЗИЦІЯ:
• Кожен гравець має 12 шашок
• Шашки розміщені на темних клітинках перших трьох рядків з кожного боку
• Гравець 1 (білі) - нижня частина дошки
• Гравець 2 (чорні) - верхня частина дошки

ОСНОВНІ ПРАВИЛА:

1. ХОДИ:
   • Шашки ходять тільки по діагоналі вперед на одну клітинку
   • Ходи можна робити тільки на темні клітинки
   • Шашка не може ходити назад (до перетворення в дамку)

2. ВЗЯТТЯ ШАШОК:
   • Якщо за шашкою супротивника є вільна клітинка, ви зобов'язані її взяти
   • Під час взяття шашка перестрибує через шашку супротивника
   • Якщо після взяття можна взяти ще одну шашку - продовжуйте (обов'язково)
   • Взяття можна робити і назад, і вперед

3. ДАМКА:
   • Коли шашка доходить до протилежного краю дошки, вона стає дамкою
   • Дамка може ходити по діагоналі на будь-яку відстань (вперед і назад)
   • Дамка може брати шашки на будь-якій відстані по діагоналі

ПРИКЛАДИ ХОДІВ:

1. Звичайний хід: шашка з c3 на d4
2. Взяття: шашка з e3 перестрибує через f4 на g5
3. Множинне взяття: після взяття на g5, можна взяти ще одну шашку на c7
4. Хід дамкою: дамка з a1 може піти на h8 за один хід

ПЕРЕМОГА:
• Гравець виграє, якщо:
  - Всі шашки супротивника з'їдені
  - Всі шашки супротивника заблоковані і не можуть ходити
  - Супротивник здається";
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

