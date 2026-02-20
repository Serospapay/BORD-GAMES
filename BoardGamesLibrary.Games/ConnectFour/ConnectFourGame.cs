/**
 * @file: ConnectFourGame.cs
 * @description: Реалізація гри Connect Four
 * @dependencies: IGame, ConnectFourBoard, ConnectFourMove, ILogger, ErrorHandler
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.ConnectFour;

/// <summary>
/// Гра Connect Four
/// </summary>
public class ConnectFourGame : IGame
{
    private readonly ILogger _logger;
    private readonly ErrorHandler _errorHandler;
    private readonly List<ConnectFourMove> _moveHistory;

    public IBoard Board { get; private set; }
    public GameState State { get; private set; }
    public Player CurrentPlayer { get; private set; }

    public ConnectFourGame(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = new ErrorHandler(_logger);
        _moveHistory = new List<ConnectFourMove>();
        Board = new ConnectFourBoard(_logger);
        State = GameState.NotStarted;
        CurrentPlayer = Player.Player1;
    }

    private ConnectFourGame(ILogger logger, IBoard board, GameState state, Player currentPlayer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = new ErrorHandler(_logger);
        _moveHistory = new List<ConnectFourMove>();
        Board = board;
        State = state;
        CurrentPlayer = currentPlayer;
    }

    public void StartNewGame()
    {
        _logger.LogInfo("Початок нової гри Connect Four");
        Board = new ConnectFourBoard(_logger);
        State = GameState.InProgress;
        CurrentPlayer = Player.Player1;
        _moveHistory.Clear();
    }

    public bool MakeMove(IMove move)
    {
        if (move == null)
        {
            _logger.LogWarning("Спроба зробити null хід");
            return false;
        }

        if (move is not ConnectFourMove connectFourMove)
        {
            _logger.LogWarning($"Невірний тип ходу: {move?.GetType().Name}");
            return false;
        }

        if (!IsValidMove(connectFourMove))
        {
            _logger.LogWarning($"Невірний хід: {connectFourMove}");
            return false;
        }

        if (State != GameState.InProgress)
        {
            _logger.LogWarning("Спроба зробити хід, коли гра не в процесі");
            return false;
        }

        if (connectFourMove.Player != CurrentPlayer)
        {
            _logger.LogWarning($"Хід належить іншому гравцю. Поточний: {CurrentPlayer}, Хід: {connectFourMove.Player}");
            return false;
        }

        return _errorHandler.ExecuteSafely(() =>
        {
            var board = Board as ConnectFourBoard;
            if (board == null)
            {
                _logger.LogError("Дошка не є дошкою для Connect Four");
                return false;
            }

            var position = board.GetLowestEmptyPosition(connectFourMove.Column);
            if (!position.HasValue)
            {
                _logger.LogWarning($"Стовпець {connectFourMove.Column} повний");
                return false;
            }

            var piece = new ConnectFourPiece(position.Value, CurrentPlayer);
            board.SetPiece(position.Value, piece);

            _moveHistory.Add(connectFourMove);
            _logger.LogInfo($"Хід виконано: {connectFourMove} на позицію {position.Value}");

            // Перевіряємо стан гри
            CheckGameState();

            // Змінюємо гравця
            CurrentPlayer = CurrentPlayer.GetOpponent();

            return true;
        }, false, "Виконання ходу в Connect Four");
    }

    public IEnumerable<IMove> GetValidMoves(Player player)
    {
        var validMoves = new List<IMove>();
        var board = Board as ConnectFourBoard;
        if (board == null) return validMoves;

        for (int col = 0; col < 7; col++)
        {
            var position = board.GetLowestEmptyPosition(col);
            if (position.HasValue)
            {
                validMoves.Add(new ConnectFourMove(col, player));
            }
        }

        return validMoves;
    }

    public bool IsValidMove(IMove move)
    {
        if (move is not ConnectFourMove connectFourMove)
            return false;

        if (!connectFourMove.IsValid())
            return false;

        var board = Board as ConnectFourBoard;
        if (board == null) return false;

        var position = board.GetLowestEmptyPosition(connectFourMove.Column);
        return position.HasValue;
    }

    public bool IsGameOver()
    {
        return State == GameState.Player1Won || 
               State == GameState.Player2Won || 
               State == GameState.Draw;
    }

    public Player? GetWinner()
    {
        return State switch
        {
            GameState.Player1Won => Player.Player1,
            GameState.Player2Won => Player.Player2,
            _ => null
        };
    }

    public IGame Clone()
    {
        return new ConnectFourGame(_logger, Board.Clone(), State, CurrentPlayer);
    }

    private void CheckGameState()
    {
        var board = Board as ConnectFourBoard;
        if (board == null) return;

        // Перевірка на перемогу
        var winner = CheckForWinner(board);
        if (winner.HasValue)
        {
            State = winner.Value == Player.Player1 ? GameState.Player1Won : GameState.Player2Won;
            _logger.LogInfo($"Гра завершена: {winner.Value} переміг");
            return;
        }

        // Перевірка на нічию (дошка повна)
        bool isFull = true;
        for (int col = 0; col < 7; col++)
        {
            if (board.GetLowestEmptyPosition(col).HasValue)
            {
                isFull = false;
                break;
            }
        }

        if (isFull)
        {
            State = GameState.Draw;
            _logger.LogInfo("Гра завершена: Нічия (дошка повна)");
        }
    }

    private Player? CheckForWinner(ConnectFourBoard board)
    {
        (int rowDir, int colDir)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) }; // Горизонталь, вертикаль, діагоналі

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var position = new Position(row, col);
                var piece = board.GetPiece(position);
                if (piece == null) continue;

                foreach (var (rowDir, colDir) in directions)
                {
                    int count = 1;
                    for (int i = 1; i < 4; i++)
                    {
                        var newRow = row + rowDir * i;
                        var newCol = col + colDir * i;
                        var newPos = new Position(newRow, newCol);

                        if (!board.IsValidPosition(newPos))
                            break;

                        var nextPiece = board.GetPiece(newPos);
                        if (nextPiece == null || nextPiece.Owner != piece.Owner)
                            break;

                        count++;
                    }

                    if (count == 4)
                    {
                        return piece.Owner;
                    }
                }
            }
        }

        return null;
    }
}

