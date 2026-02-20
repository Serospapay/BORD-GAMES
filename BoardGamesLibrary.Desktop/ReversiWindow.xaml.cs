/**
 * @file: ReversiWindow.xaml.cs
 * @description: Вікно для гри Reversi/Othello
 * @dependencies: ReversiGame, ReversiBoardControl, ILogger
 * @created: 2024-12-19
 */

using System;
using System.Windows;
using System.Windows.Threading;
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
    private readonly FileLogger _fileLogger;
    private readonly StatisticsService _statisticsService;
    private readonly bool _playVsAI;
    private readonly AIDifficulty _aiDifficulty;
    private ReversiAIPlayer? _aiPlayer;
    private int _moveCount = 0;
    private bool _gameResultRecorded = false;
    private DateTime _gameStartTime;
    private DispatcherTimer? _gameTimer;
    private bool _endgameDialogShown = false;
    private bool _endgameEffectsPlayed = false;

    public ReversiWindow(bool playVsAI = false, AIDifficulty aiDifficulty = AIDifficulty.Medium)
    {
        InitializeComponent();
        _playVsAI = playVsAI;
        _aiDifficulty = aiDifficulty;
        
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        _fileLogger = new FileLogger("logs", "reversi.log", LogLevel.Debug);
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
        _game = new ReversiGame(_logger);
        _game.StartNewGame();
        _aiPlayer = _playVsAI ? new ReversiAIPlayer(Player.Player2, _aiDifficulty, _logger) : null;
        ReversiBoard.Initialize(_game, OnMoveMade);
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
            if (move != null && _game.MakeMove(move))
            {
                ReversiBoard.UpdateBoard();
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
                _statisticsService.RecordGameResult("Reversi", winner, _moveCount);
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
            if (PiecesText != null && _game.Board is ReversiBoard board)
            {
                var (p1, p2) = CountReversiPieces(board);
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
                    StatusText.Text = "Гра в процесі";
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

    private static (int p1, int p2) CountReversiPieces(ReversiBoard board)
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

