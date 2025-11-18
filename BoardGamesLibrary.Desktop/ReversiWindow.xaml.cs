/**
 * @file: ReversiWindow.xaml.cs
 * @description: Вікно для гри Reversi/Othello
 * @dependencies: ReversiGame, ReversiBoardControl, ILogger
 * @created: 2024-12-19
 */

using System.Windows;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Reversi;
using BoardGamesLibrary.Logging;
using BoardGamesLibrary.Desktop.Services;
using static BoardGamesLibrary.Core.GameState;

namespace BoardGamesLibrary.Desktop;

public partial class ReversiWindow : Window
{
    private ReversiGame? _game;
    private readonly ILogger _logger;
    private readonly StatisticsService _statisticsService;
    private int _moveCount = 0;
    private bool _gameResultRecorded = false;

    public ReversiWindow()
    {
        InitializeComponent();
        
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        var fileLogger = new FileLogger("logs", "reversi.log", LogLevel.Debug);
        _logger = new CompositeLogger(consoleLogger, fileLogger);
        _statisticsService = new StatisticsService();
        
            StartNewGame();
    }

    private void StartNewGame()
    {
        _game = new ReversiGame(_logger);
        _game.StartNewGame();
        ReversiBoard.Initialize(_game, OnMoveMade);
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
                _statisticsService.RecordGameResult("Reversi", winner, _moveCount);
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
                        SoundService.Instance.PlayVictorySound();
                    }
                    else
                    {
                        StatusText.Text = "Нічия!";
                        SoundService.Instance.PlayDrawSound();
                    }
                }
                else
                {
                    StatusText.Text = "Гра в процесі";
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
        var rulesText = GetReversiRules();
        var rulesDialog = new RulesDialog("Reversi", rulesText)
        {
            Owner = this
        };
        rulesDialog.ShowDialog();
    }

    private string GetReversiRules()
    {
        return @"REVERSI (ОТЕЛЛО) - Стратегічна гра для двох гравців на дошці 8x8.

ЦІЛЬ ГРИ:
Мета - мати більше фішок свого кольору на дошці, коли гра закінчується.

ПОЧАТКОВА ПОЗИЦІЯ:
• Дошка 8x8 клітинок
• У центрі дошки розміщені 4 фішки: 2 білі (d4, e5) та 2 чорні (d5, e4)
• Гравець 1 грає білими, Гравець 2 - чорними

ОСНОВНІ ПРАВИЛА:

1. ХОДИ:
   • Гравець кладе свою фішку на дошку так, щоб вона 'охопила' одну або більше фішок супротивника
   • 'Охоплення' означає, що ваша фішка має бути на одній лінії (горизонталь, вертикаль або діагональ) з фішкою супротивника, а між ними мають бути тільки фішки супротивника, після яких йде ваша фішка
   • Всі 'охоплені' фішки супротивника перевертаються на ваш колір

2. ОБОВ'ЯЗКОВІ ХОДИ:
   • Ви повинні зробити хід, який охоплює хоча б одну фішку супротивника
   • Якщо у вас немає можливих ходів, хід переходить до супротивника
   • Якщо обидва гравці не можуть ходити, гра закінчується

3. ПЕРЕВЕРТАННЯ ФІШОК:
   • Після вашого ходу всі фішки супротивника, які опинилися між вашими фішками по лінії, перевертаються
   • Перевертаються фішки тільки по прямій лінії (горизонталь, вертикаль, діагональ)

ПРИКЛАДИ ХОДІВ:

1. Білий хід: фішка на c4 охоплює чорну фішку на d4, яка перевертається
2. Чорний хід: фішка на f5 охоплює білу фішку на e5, яка перевертається
3. Множинне охоплення: фішка на a1 може охопити кілька фішок по діагоналі одночасно

СТРАТЕГІЯ:
• Контролюйте кути дошки - вони дають стабільні позиції
• Уникайте клітинок біля кутів, якщо це дасть супротивнику доступ до кута
• Намагайтеся мінімізувати кількість можливих ходів супротивника

ПЕРЕМОГА:
• Гра закінчується, коли:
  - Дошка заповнена
  - Обидва гравці не можуть зробити хід
• Перемагає гравець з більшою кількістю фішок свого кольору на дошці";
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

