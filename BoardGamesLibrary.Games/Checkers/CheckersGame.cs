/**
 * @file: CheckersGame.cs
 * @description: Реалізація гри в шашки з повною логікою правил
 * @dependencies: IGame, CheckersBoard, CheckersMove, CheckersPiece, ILogger, ErrorHandler
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Checkers;

/// <summary>
/// Гра в шашки з повною реалізацією правил
/// </summary>
public class CheckersGame : IGame
{
    private readonly ILogger _logger;
    private readonly ErrorHandler _errorHandler;
    private readonly List<CheckersMove> _moveHistory;

    public IBoard Board { get; private set; }
    public GameState State { get; private set; }
    public Player CurrentPlayer { get; private set; }

    public CheckersGame(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = new ErrorHandler(_logger);
        _moveHistory = new List<CheckersMove>();
        Board = new CheckersBoard(_logger);
        State = GameState.NotStarted;
        CurrentPlayer = Player.Player1;
    }

    public void StartNewGame()
    {
        _logger.LogInfo("Початок нової гри в шашки");
        Board = new CheckersBoard(_logger);
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

        if (move is not CheckersMove checkersMove)
        {
            _logger.LogWarning($"Невірний тип ходу: {move.GetType().Name}");
            return false;
        }

        if (State != GameState.InProgress)
        {
            _logger.LogWarning("Спроба зробити хід, коли гра не в процесі");
            return false;
        }

        if (checkersMove.Player != CurrentPlayer)
        {
            _logger.LogWarning($"Хід належить іншому гравцю. Поточний: {CurrentPlayer}, Хід: {checkersMove.Player}");
            return false;
        }

        if (!IsValidMove(checkersMove))
        {
            _logger.LogWarning($"Невірний хід: {checkersMove}");
            return false;
        }

        return _errorHandler.ExecuteSafely(() =>
        {
            var board = Board as CheckersBoard;
            if (board == null)
            {
                _logger.LogError("Дошка не є дошкою для шашок");
                return false;
            }

            // Додаткова перевірка стану перед виконанням
            if (State != GameState.InProgress)
            {
                _logger.LogWarning("Стан гри змінився під час виконання ходу");
                return false;
            }

            var piece = board.GetPiece(checkersMove.From);
            if (piece == null)
            {
                _logger.LogWarning($"Фігура не знайдена на позиції {checkersMove.From}");
                return false;
            }

            if (piece.Owner != CurrentPlayer)
            {
                _logger.LogWarning($"Фігура належить іншому гравцю");
                return false;
            }

            // Виконуємо хід
            board.SetPiece(checkersMove.From, null);
            board.SetPiece(checkersMove.To, piece);
            piece.MoveTo(checkersMove.To);

            // Видаляємо захоплені фігури
            foreach (var capturedPos in checkersMove.CapturedPositions)
            {
                board.SetPiece(capturedPos, null);
            }

            _moveHistory.Add(checkersMove);
            _logger.LogInfo($"Хід виконано: {checkersMove}");

            // Перевіряємо стан гри
            CheckGameState();

            // Змінюємо гравця тільки якщо гра ще не закінчилася
            if (State == GameState.InProgress)
            {
                CurrentPlayer = CurrentPlayer.GetOpponent();
            }

            return true;
        }, false, "Виконання ходу в шашках");
    }

    public IEnumerable<IMove> GetValidMoves(Player player)
    {
        var validMoves = new List<IMove>();
        var board = Board as CheckersBoard;
        if (board == null) return validMoves;

        // Спочатку перевіряємо чи є обов'язкові стрибки
        var jumpMoves = new List<CheckersMove>();
        var regularMoves = new List<CheckersMove>();

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var position = new Position(row, col);
                var piece = board.GetPiece(position);
                if (piece != null && piece.Owner == player)
                {
                    var moves = GetValidMovesForPiece(piece, board);
                    foreach (var move in moves)
                    {
                        if (move.IsJump)
                            jumpMoves.Add(move);
                        else
                            regularMoves.Add(move);
                    }
                }
            }
        }

        // Якщо є стрибки, вони обов'язкові
        if (jumpMoves.Any())
        {
            return jumpMoves;
        }

        return regularMoves;
    }

    private IEnumerable<CheckersMove> GetValidMovesForPiece(CheckersPiece piece, CheckersBoard board)
    {
        var moves = new List<CheckersMove>();
        int direction = piece.Owner == Player.Player1 ? 1 : -1;

        // Звичайні ходи
        if (!piece.IsKing)
        {
            // Діагональні ходи вперед
            AddDiagonalMoves(piece, board, direction, moves, false);
        }
        else
        {
            // Дамка може ходити в обидві сторони
            AddDiagonalMoves(piece, board, 1, moves, false);
            AddDiagonalMoves(piece, board, -1, moves, false);
        }

        // Стрибки (обов'язкові, якщо можливі)
        if (!piece.IsKing)
        {
            AddJumpMoves(piece, board, direction, moves);
        }
        else
        {
            AddJumpMoves(piece, board, 1, moves);
            AddJumpMoves(piece, board, -1, moves);
        }

        return moves;
    }

    private void AddDiagonalMoves(CheckersPiece piece, CheckersBoard board, int rowDirection, List<CheckersMove> moves, bool isJump)
    {
        for (int colOffset = -1; colOffset <= 1; colOffset += 2)
        {
            var newRow = piece.Position.Row + rowDirection;
            var newCol = piece.Position.Column + colOffset;
            var newPos = new Position(newRow, newCol);

            if (!board.IsValidPosition(newPos) || !board.IsDarkSquare(newPos))
                continue;

            var targetPiece = board.GetPiece(newPos);
            if (targetPiece == null && !isJump)
            {
                var move = new CheckersMove(piece.Position, newPos, piece.Owner);
                if ((piece.Owner == Player.Player1 && newPos.Row == 7) ||
                    (piece.Owner == Player.Player2 && newPos.Row == 0))
                {
                    move.IsPromotion = true;
                }
                moves.Add(move);
            }
        }
    }

    private void AddJumpMoves(CheckersPiece piece, CheckersBoard board, int rowDirection, List<CheckersMove> moves)
    {
        for (int colOffset = -1; colOffset <= 1; colOffset += 2)
        {
            var jumpOverRow = piece.Position.Row + rowDirection;
            var jumpOverCol = piece.Position.Column + colOffset;
            var jumpOverPos = new Position(jumpOverRow, jumpOverCol);

            if (!board.IsValidPosition(jumpOverPos))
                continue;

            var jumpOverPiece = board.GetPiece(jumpOverPos);
            if (jumpOverPiece == null || jumpOverPiece.Owner == piece.Owner)
                continue;

            var landRow = jumpOverRow + rowDirection;
            var landCol = jumpOverCol + colOffset;
            var landPos = new Position(landRow, landCol);

            if (!board.IsValidPosition(landPos) || !board.IsDarkSquare(landPos))
                continue;

            var landPiece = board.GetPiece(landPos);
            if (landPiece == null)
            {
                var move = new CheckersMove(piece.Position, landPos, piece.Owner)
                {
                    IsJump = true
                };
                move.CapturedPositions.Add(jumpOverPos);

                if ((piece.Owner == Player.Player1 && landPos.Row == 7) ||
                    (piece.Owner == Player.Player2 && landPos.Row == 0))
                {
                    move.IsPromotion = true;
                }

                moves.Add(move);
            }
        }
    }

    public bool IsValidMove(IMove move)
    {
        if (move is not CheckersMove checkersMove)
            return false;

        if (!checkersMove.IsValid())
            return false;

        var board = Board as CheckersBoard;
        if (board == null) return false;

        var piece = board.GetPiece(checkersMove.From);
        if (piece == null || piece.Owner != checkersMove.Player)
            return false;

        // Перевіряємо чи є обов'язкові стрибки
        var allMoves = GetValidMoves(checkersMove.Player);
        var jumpMoves = allMoves.OfType<CheckersMove>().Where(m => m.IsJump).ToList();

        if (jumpMoves.Any() && !checkersMove.IsJump)
        {
            return false; // Якщо є стрибки, вони обов'язкові
        }

        var validMoves = GetValidMovesForPiece(piece, board);
        return validMoves.Any(m => m.From == checkersMove.From && m.To == checkersMove.To);
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
        var board = Board as CheckersBoard;
        if (board == null) return;

        // Перевірка кількості фігур
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

        if (player1Count == 0)
        {
            State = GameState.Player2Won;
            _logger.LogInfo("Гра завершена: Player2 переміг (всі фігури Player1 захоплені)");
        }
        else if (player2Count == 0)
        {
            State = GameState.Player1Won;
            _logger.LogInfo("Гра завершена: Player1 переміг (всі фігури Player2 захоплені)");
        }
        else
        {
            // Перевірка на пат (немає валідних ходів)
            var validMoves = GetValidMoves(CurrentPlayer);
            if (!validMoves.Any())
            {
                State = GameState.Draw;
                _logger.LogInfo("Гра завершена: Нічия (немає валідних ходів)");
            }
        }
    }
}

