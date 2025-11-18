/**
 * @file: ReversiGame.cs
 * @description: Реалізація гри Reversi/Othello
 * @dependencies: IGame, ReversiBoard, ReversiMove, ILogger, ErrorHandler
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Reversi;

/// <summary>
/// Гра Reversi/Othello
/// </summary>
public class ReversiGame : IGame
{
    private readonly ILogger _logger;
    private readonly ErrorHandler _errorHandler;
    private readonly List<ReversiMove> _moveHistory;

    public IBoard Board { get; private set; }
    public GameState State { get; private set; }
    public Player CurrentPlayer { get; private set; }

    public ReversiGame(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = new ErrorHandler(_logger);
        _moveHistory = new List<ReversiMove>();
        Board = new ReversiBoard(_logger);
        State = GameState.NotStarted;
        CurrentPlayer = Player.Player1;
    }

    public void StartNewGame()
    {
        _logger.LogInfo("Початок нової гри Reversi");
        Board = new ReversiBoard(_logger);
        State = GameState.InProgress;
        CurrentPlayer = Player.Player1;
        _moveHistory.Clear();
    }

    public bool MakeMove(IMove move)
    {
        if (move is not ReversiMove reversiMove)
        {
            _logger.LogWarning($"Невірний тип ходу: {move?.GetType().Name}");
            return false;
        }

        if (!IsValidMove(reversiMove))
        {
            _logger.LogWarning($"Невірний хід: {reversiMove}");
            return false;
        }

        if (State != GameState.InProgress)
        {
            _logger.LogWarning("Спроба зробити хід, коли гра не в процесі");
            return false;
        }

        if (reversiMove.Player != CurrentPlayer)
        {
            _logger.LogWarning($"Хід належить іншому гравцю. Поточний: {CurrentPlayer}, Хід: {reversiMove.Player}");
            return false;
        }

        return _errorHandler.ExecuteSafely(() =>
        {
            var board = Board as ReversiBoard;
            if (board == null)
            {
                _logger.LogError("Дошка не є дошкою для Reversi");
                return false;
            }

            // Розміщуємо фігуру
            var piece = new ReversiPiece(reversiMove.From, CurrentPlayer);
            board.SetPiece(reversiMove.From, piece);

            // Перевертаємо фігури
            foreach (var flippedPos in reversiMove.FlippedPositions)
            {
                var flippedPiece = board.GetPiece(flippedPos);
                if (flippedPiece != null)
                {
                    // Створюємо нову фігуру з іншим гравцем
                    var newPiece = new ReversiPiece(flippedPos, CurrentPlayer);
                    board.SetPiece(flippedPos, newPiece);
                }
            }

            _moveHistory.Add(reversiMove);
            _logger.LogInfo($"Хід виконано: {reversiMove}");

            // Перевіряємо стан гри
            CheckGameState();

            // Змінюємо гравця
            var nextPlayer = CurrentPlayer.GetOpponent();
            var nextPlayerMoves = GetValidMoves(nextPlayer);

            // Якщо наступний гравець не може ходити, поточний гравець ходить знову
            if (!nextPlayerMoves.Any())
            {
                var currentPlayerMoves = GetValidMoves(CurrentPlayer);
                if (!currentPlayerMoves.Any())
                {
                    // Обидва гравці не можуть ходити - гра закінчена
                    _logger.LogInfo("Обидва гравці не можуть ходити - гра закінчена");
                }
                else
                {
                    _logger.LogInfo($"{nextPlayer} не може ходити, {CurrentPlayer} ходить знову");
                    // CurrentPlayer залишається тим самим
                }
            }
            else
            {
                CurrentPlayer = nextPlayer;
            }

            return true;
        }, false, "Виконання ходу в Reversi");
    }

    public IEnumerable<IMove> GetValidMoves(Player player)
    {
        var validMoves = new List<IMove>();
        var board = Board as ReversiBoard;
        if (board == null) return validMoves;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var position = new Position(row, col);
                if (board.GetPiece(position) != null)
                    continue; // Позиція зайнята

                var move = new ReversiMove(position, player);
                if (FindFlippedPieces(move, board, player))
                {
                    validMoves.Add(move);
                }
            }
        }

        return validMoves;
    }

    private bool FindFlippedPieces(ReversiMove move, ReversiBoard board, Player player)
    {
        bool foundAny = false;
        int[] directions = { -1, 0, 1 };

        foreach (int rowDir in directions)
        {
            foreach (int colDir in directions)
            {
                if (rowDir == 0 && colDir == 0) continue;

                var flipped = FindFlippedInDirection(move.From, board, player, rowDir, colDir);
                if (flipped.Any())
                {
                    move.FlippedPositions.AddRange(flipped);
                    foundAny = true;
                }
            }
        }

        return foundAny;
    }

    private List<Position> FindFlippedInDirection(Position start, ReversiBoard board, Player player, int rowDir, int colDir)
    {
        var flipped = new List<Position>();
        var opponent = player.GetOpponent();
        int row = start.Row + rowDir;
        int col = start.Column + colDir;

        // Спочатку має бути фігура супротивника
        while (board.IsValidPosition(new Position(row, col)))
        {
            var piece = board.GetPiece(new Position(row, col));
            if (piece == null)
                return new List<Position>(); // Порожня клітинка - не валідний напрямок

            if (piece.Owner == opponent)
            {
                flipped.Add(new Position(row, col));
                row += rowDir;
                col += colDir;
            }
            else if (piece.Owner == player)
            {
                // Знайшли свою фігуру - всі між ними перевертаються
                return flipped;
            }
        }

        return new List<Position>(); // Дійшли до краю дошки
    }

    public bool IsValidMove(IMove move)
    {
        if (move is not ReversiMove reversiMove)
            return false;

        if (!reversiMove.IsValid())
            return false;

        var board = Board as ReversiBoard;
        if (board == null) return false;

        // Позиція має бути порожньою
        if (board.GetPiece(reversiMove.From) != null)
            return false;

        // Має бути хоча б одна фігура для перевертання
        var testMove = new ReversiMove(reversiMove.From, reversiMove.Player);
        return FindFlippedPieces(testMove, board, reversiMove.Player);
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

    private void CheckGameState()
    {
        var board = Board as ReversiBoard;
        if (board == null) return;

        // Підрахунок фігур
        int player1Count = 0;
        int player2Count = 0;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));
                if (piece != null)
                {
                    if (piece.Owner == Player.Player1)
                        player1Count++;
                    else if (piece.Owner == Player.Player2)
                        player2Count++;
                }
            }
        }

        // Перевірка чи є валідні ходи
        var player1Moves = GetValidMoves(Player.Player1);
        var player2Moves = GetValidMoves(Player.Player2);

        if (!player1Moves.Any() && !player2Moves.Any())
        {
            // Гра закінчена
            if (player1Count > player2Count)
            {
                State = GameState.Player1Won;
                _logger.LogInfo($"Гра завершена: Player1 переміг ({player1Count} vs {player2Count})");
            }
            else if (player2Count > player1Count)
            {
                State = GameState.Player2Won;
                _logger.LogInfo($"Гра завершена: Player2 переміг ({player2Count} vs {player1Count})");
            }
            else
            {
                State = GameState.Draw;
                _logger.LogInfo($"Гра завершена: Нічия ({player1Count} vs {player2Count})");
            }
        }
    }
}

