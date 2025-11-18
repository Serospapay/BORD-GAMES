/**
 * @file: ConnectFourWindow.xaml.cs
 * @description: Вікно для гри Connect Four
 * @dependencies: ConnectFourGame, ConnectFourBoardControl, ILogger
 * @created: 2024-12-19
 */

using System.Windows;
using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.ConnectFour;
using BoardGamesLibrary.Logging;
using BoardGamesLibrary.Desktop.Services;
using static BoardGamesLibrary.Core.GameState;

namespace BoardGamesLibrary.Desktop;

public partial class ConnectFourWindow : Window
{
    private ConnectFourGame? _game;
    private readonly ILogger _logger;
    private readonly StatisticsService _statisticsService;
    private int _moveCount = 0;
    private bool _gameResultRecorded = false;

    public ConnectFourWindow()
    {
        InitializeComponent();
        
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        var fileLogger = new FileLogger("logs", "connectfour.log", LogLevel.Debug);
        _logger = new CompositeLogger(consoleLogger, fileLogger);
        _statisticsService = new StatisticsService();
        
            StartNewGame();
    }

    private void StartNewGame()
    {
        _game = new ConnectFourGame(_logger);
        _game.StartNewGame();
        ConnectFourBoard.Initialize(_game, OnMoveMade);
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
                _statisticsService.RecordGameResult("Connect Four", winner, _moveCount);
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
        var rulesText = GetConnectFourRules();
        var rulesDialog = new RulesDialog("Connect Four", rulesText)
        {
            Owner = this
        };
        rulesDialog.ShowDialog();
    }

    private string GetConnectFourRules()
    {
        return @"CONNECT FOUR - Швидка стратегічна гра для двох гравців.

ЦІЛЬ ГРИ:
Мета - першим з'єднати 4 свої фішки по горизонталі, вертикалі або діагоналі.

ПОЧАТКОВА ПОЗИЦІЯ:
• Дошка 7 колонок x 6 рядків (вертикальна)
• Дошка порожня на початку гри
• Гравець 1 грає червоними фішками
• Гравець 2 грає жовтими фішками

ОСНОВНІ ПРАВИЛА:

1. ХОДИ:
   • Гравець обирає колонку і 'кидає' свою фішку в неї
   • Фішка падає вниз і займає найнижчу вільну клітинку в обраній колонці
   • Гравці ходять по черзі

2. ПЕРЕМОГА:
   • Гравець виграє, якщо з'єднує 4 свої фішки в ряд:
     - По горизонталі (4 фішки в одному рядку)
     - По вертикалі (4 фішки в одній колонці)
     - По діагоналі (4 фішки по діагоналі вгору або вниз)

3. НІЧИЯ:
   • Якщо дошка заповнена, але ніхто не з'єднав 4 фішки - нічия

ПРИКЛАДИ ХОДІВ:

1. Гравець 1: фішка в колонку 4 (падає вниз)
2. Гравець 2: фішка в колонку 4 (падає на фішку гравця 1)
3. Гравець 1: фішка в колонку 3
4. Гравець 2: фішка в колонку 5

СТРАТЕГІЯ:
• Блокуйте супротивника - не давайте йому з'єднати 4 фішки
• Будуючи власну комбінацію, одночасно блокуйте супротивника
• Контролюйте центр дошки - це дає більше можливостей
• Уважно стежте за діагоналями - їх легко пропустити

ПЕРЕМОГА:
• Гра закінчується, коли один з гравців з'єднує 4 фішки
• Якщо дошка заповнена без переможця - нічия";
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

