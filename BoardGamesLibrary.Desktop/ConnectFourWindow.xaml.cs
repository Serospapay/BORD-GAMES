/**
 * @file: ConnectFourWindow.xaml.cs
 * @description: Вікно для гри Connect Four
 * @dependencies: ConnectFourGame, ConnectFourBoardControl, ILogger
 * @created: 2024-12-19
 */

using System;
using System.Windows;
using System.Windows.Threading;
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
    private readonly FileLogger _fileLogger;
    private readonly StatisticsService _statisticsService;
    private readonly bool _playVsAI;
    private readonly AIDifficulty _aiDifficulty;
    private ConnectFourAIPlayer? _aiPlayer;
    private int _moveCount = 0;
    private bool _gameResultRecorded = false;
    private DateTime _gameStartTime;
    private DispatcherTimer? _gameTimer;
    private bool _endgameDialogShown = false;
    private bool _endgameEffectsPlayed = false;

    public ConnectFourWindow(bool playVsAI = false, AIDifficulty aiDifficulty = AIDifficulty.Medium)
    {
        InitializeComponent();
        _playVsAI = playVsAI;
        _aiDifficulty = aiDifficulty;
        
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        _fileLogger = new FileLogger("logs", "connectfour.log", LogLevel.Debug);
        _logger = new CompositeLogger(consoleLogger, _fileLogger);
        _statisticsService = new StatisticsService();
        
        Closed += (s, e) => _fileLogger.Dispose();
        Loaded += (s, e) =>
        {
            try { StartNewGame(); }
            catch (Exception ex)
            {
                _logger?.LogError("Критична помилка", ex);
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        };
    }

    private void StartNewGame()
    {
        _game = new ConnectFourGame(_logger);
        _game.StartNewGame();
        _aiPlayer = _playVsAI ? new ConnectFourAIPlayer(Player.Player2, _aiDifficulty, _logger) : null;
        ConnectFourBoard.Initialize(_game, OnMoveMade, _playVsAI ? Player.Player1 : null);
        _moveCount = 0;
        _gameResultRecorded = false;
        _endgameDialogShown = false;
        _endgameEffectsPlayed = false;
        _gameStartTime = DateTime.Now;
        _gameTimer?.Stop();
        _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _gameTimer.Tick += (_, _) => UpdateUI();
        _gameTimer.Start();
        UpdateUI();
        TryAIMove();
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
            if (move is ConnectFourMove connectFourMove)
            {
                _ = ConnectFourBoard.ExecuteMoveWithAnimationAsync(connectFourMove.Column, enforceHumanTurn: false);
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
                _statisticsService.RecordGameResult("Connect Four", winner, _moveCount);
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
            if (PiecesText != null && _game.Board is ConnectFourBoard board)
            {
                var (p1, p2) = CountConnectFourPieces(board);
                PiecesText.Text = $"Г1: {p1} фішок | Г2: {p2} фішок";
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
                            SoundService.Instance.PlayVictorySound();
                            _endgameEffectsPlayed = true;
                        }
                    }
                    else
                    {
                        StatusText.Text = "Нічия!";
                        if (!_endgameEffectsPlayed)
                        {
                            SoundService.Instance.PlayDrawSound();
                            _endgameEffectsPlayed = true;
                        }
                    }
                }
                else
                {
                    if (_playVsAI && _game.CurrentPlayer == Player.Player2)
                    {
                        StatusText.Text = "Хід AI: обирає колонку...";
                    }
                    else
                    {
                        StatusText.Text = "Оберіть колонку для ходу";
                    }
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

    private static (int p1, int p2) CountConnectFourPieces(ConnectFourBoard board)
    {
        int p1 = 0, p2 = 0;
        for (int r = 0; r < board.Rows; r++)
        {
            for (int c = 0; c < board.Columns; c++)
            {
                var piece = board.GetPiece(new Position(r, c));
                if (piece == null) continue;
                if (piece.Owner == Player.Player1) p1++;
                else p2++;
            }
        }
        return (p1, p2);
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

