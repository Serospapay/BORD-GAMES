/**
 * @file: ChessGame.cs
 * @description: Реалізація шахової гри з повною логікою правил
 * @dependencies: IGame, ChessBoard, ChessMove, ChessPiece, ILogger, ErrorHandler
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Chess;

/// <summary>
/// Шахова гра з повною реалізацією правил
/// </summary>
public class ChessGame : IGame
{
    private readonly ILogger _logger;
    private readonly ErrorHandler _errorHandler;
    private readonly List<ChessMove> _moveHistory;

    public IBoard Board { get; private set; }
    public GameState State { get; private set; }
    public Player CurrentPlayer { get; private set; }

    public ChessGame(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = new ErrorHandler(_logger);
        _moveHistory = new List<ChessMove>();
        Board = new ChessBoard(_logger);
        State = GameState.NotStarted;
        CurrentPlayer = Player.Player1;
    }

    private ChessGame(ILogger logger, IBoard board, GameState state, Player currentPlayer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorHandler = new ErrorHandler(_logger);
        _moveHistory = new List<ChessMove>();
        Board = board;
        State = state;
        CurrentPlayer = currentPlayer;
    }

    public void StartNewGame()
    {
        _logger.LogInfo("Початок нової шахової гри");
        Board = new ChessBoard(_logger);
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

        if (move is not ChessMove chessMove)
        {
            _logger.LogWarning($"Невірний тип ходу: {move.GetType().Name}");
            return false;
        }

        if (State != GameState.InProgress)
        {
            _logger.LogWarning("Спроба зробити хід, коли гра не в процесі");
            return false;
        }

        if (chessMove.Player != CurrentPlayer)
        {
            _logger.LogWarning($"Хід належить іншому гравцю. Поточний: {CurrentPlayer}, Хід: {chessMove.Player}");
            return false;
        }

        if (!IsValidMove(chessMove))
        {
            _logger.LogWarning($"Невірний хід: {chessMove}");
            return false;
        }

        return _errorHandler.ExecuteSafely(() =>
        {
            var board = Board as ChessBoard;
            if (board == null)
            {
                _logger.LogError("Дошка не є шаховою дошкою");
                return false;
            }

            // Додаткова перевірка стану перед виконанням
            if (State != GameState.InProgress)
            {
                _logger.LogWarning("Стан гри змінився під час виконання ходу");
                return false;
            }

            var piece = board.GetPiece(chessMove.From);
            if (piece == null)
            {
                _logger.LogWarning($"Фігура не знайдена на позиції {chessMove.From}");
                return false;
            }

            if (piece.Owner != CurrentPlayer)
            {
                _logger.LogWarning($"Фігура належить іншому гравцю");
                return false;
            }

            board.EnPassantTarget = null;

            board.MarkPieceMoved(chessMove.From, piece);

            if (chessMove.IsCastling)
            {
                chessMove.CapturedPiece = null;
                board.SetPiece(chessMove.From, null);
                board.SetPiece(chessMove.To, piece);
                piece.MoveTo(chessMove.To);
                if (chessMove.To.Column == 6)
                {
                    var rook = board.GetPiece(new Position(chessMove.From.Row, 7)) as ChessPiece;
                    if (rook != null)
                    {
                        board.MarkPieceMoved(new Position(chessMove.From.Row, 7), rook);
                        board.SetPiece(new Position(chessMove.From.Row, 7), null);
                        board.SetPiece(new Position(chessMove.From.Row, 5), rook);
                        rook.MoveTo(new Position(chessMove.From.Row, 5));
                    }
                    else
                    {
                        _logger.LogWarning("Рокіровка неможлива: відсутня тура на королівському фланзі");
                        return false;
                    }
                }
                else if (chessMove.To.Column == 2)
                {
                    var rook = board.GetPiece(new Position(chessMove.From.Row, 0)) as ChessPiece;
                    if (rook != null)
                    {
                        board.MarkPieceMoved(new Position(chessMove.From.Row, 0), rook);
                        board.SetPiece(new Position(chessMove.From.Row, 0), null);
                        board.SetPiece(new Position(chessMove.From.Row, 3), rook);
                        rook.MoveTo(new Position(chessMove.From.Row, 3));
                    }
                    else
                    {
                        _logger.LogWarning("Рокіровка неможлива: відсутня тура на ферзевому фланзі");
                        return false;
                    }
                }
            }
            else if (chessMove.IsEnPassant)
            {
                chessMove.CapturedPiece = board.GetPiece(new Position(chessMove.From.Row, chessMove.To.Column)) as ChessPiece;
                if (chessMove.CapturedPiece != null)
                    board.SetPiece(new Position(chessMove.From.Row, chessMove.To.Column), null);
                board.SetPiece(chessMove.From, null);
                board.SetPiece(chessMove.To, piece);
                piece.MoveTo(chessMove.To);
            }
            else
            {
                chessMove.CapturedPiece = board.GetPiece(chessMove.To) as ChessPiece;
                if (chessMove.CapturedPiece is { Type: ChessPieceType.Rook })
                {
                    board.MarkRookCaptured(chessMove.To, chessMove.CapturedPiece.Owner);
                }
                board.SetPiece(chessMove.From, null);
                board.SetPiece(chessMove.To, piece);
                piece.MoveTo(chessMove.To);

                if (piece.Type == ChessPieceType.Pawn && Math.Abs(chessMove.To.Row - chessMove.From.Row) == 2)
                {
                    int epRow = CurrentPlayer == Player.Player1 ? 2 : 5;
                    board.EnPassantTarget = new Position(epRow, chessMove.To.Column);
                }
            }

            if (chessMove.IsPromotion && chessMove.PromotionType.HasValue)
            {
                var newPiece = new ChessPiece(chessMove.To, CurrentPlayer, chessMove.PromotionType.Value);
                board.SetPiece(chessMove.To, newPiece);
            }

            _moveHistory.Add(chessMove);
            _logger.LogInfo($"Хід виконано: {chessMove}");

            // Перевіряємо стан гри
            CheckGameState();

            // Змінюємо гравця тільки якщо гра ще не закінчилася
            if (State == GameState.InProgress)
            {
                CurrentPlayer = CurrentPlayer.GetOpponent();
            }

            return true;
        }, false, "Виконання ходу в шахах");
    }

    public IEnumerable<IMove> GetValidMoves(Player player)
    {
        var validMoves = new List<IMove>();
        var board = Board as ChessBoard;
        if (board == null) return validMoves;

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
                        if (!WouldLeaveKingInCheck(board, move, player))
                            validMoves.Add(move);
                    }
                }
            }
        }

        var castlingMoves = GetCastlingMoves(board, player);
        foreach (var move in castlingMoves)
        {
            if (!WouldLeaveKingInCheck(board, move, player))
                validMoves.Add(move);
        }

        return validMoves;
    }

    private bool IsKingInCheck(ChessBoard board, Player kingOwner)
    {
        var kingPos = FindKingPosition(board, kingOwner);
        if (!kingPos.HasValue) return false;
        var opponent = kingOwner.GetOpponent();
        return IsSquareAttackedBy(board, kingPos.Value, opponent);
    }

    private Position? FindKingPosition(ChessBoard board, Player owner)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var pos = new Position(row, col);
                var piece = board.GetPiece(pos);
                if (piece is ChessPiece cp && cp.Type == ChessPieceType.King && cp.Owner == owner)
                    return pos;
            }
        }
        return null;
    }

    private bool IsSquareAttackedBy(ChessBoard board, Position square, Player attacker)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var pos = new Position(row, col);
                var piece = board.GetPiece(pos) as ChessPiece;
                if (piece == null || piece.Owner != attacker) continue;

                if (piece.Type == ChessPieceType.Pawn)
                {
                    int dir = attacker == Player.Player1 ? 1 : -1;
                    if (square.Row == pos.Row + dir && Math.Abs(square.Column - pos.Column) == 1)
                        return true;
                }
                else
                {
                    var moves = GetValidMovesForPiece(piece, board);
                    if (moves.Any(m => m.To == square))
                        return true;
                }
            }
        }
        return false;
    }

    private bool WouldLeaveKingInCheck(ChessBoard board, ChessMove move, Player player)
    {
        var clone = (ChessBoard)board.Clone();
        var piece = clone.GetPiece(move.From) as ChessPiece;
        if (piece == null) return true;

        var newPiece = new ChessPiece(move.To, piece.Owner, piece.Type);
        clone.SetPiece(move.From, null);
        clone.SetPiece(move.To, newPiece);

        if (move.IsEnPassant)
        {
            var captureRow = player == Player.Player1 ? move.To.Row - 1 : move.To.Row + 1;
            clone.SetPiece(new Position(captureRow, move.To.Column), null);
        }
        else if (move.IsCastling)
        {
            if (move.To.Column == 6)
            {
                var rook = clone.GetPiece(new Position(move.From.Row, 7)) as ChessPiece;
                if (rook != null)
                {
                    clone.SetPiece(new Position(move.From.Row, 7), null);
                    clone.SetPiece(new Position(move.From.Row, 5), new ChessPiece(new Position(move.From.Row, 5), rook.Owner, ChessPieceType.Rook));
                }
            }
            else if (move.To.Column == 2)
            {
                var rook = clone.GetPiece(new Position(move.From.Row, 0)) as ChessPiece;
                if (rook != null)
                {
                    clone.SetPiece(new Position(move.From.Row, 0), null);
                    clone.SetPiece(new Position(move.From.Row, 3), new ChessPiece(new Position(move.From.Row, 3), rook.Owner, ChessPieceType.Rook));
                }
            }
        }

        return IsKingInCheck(clone, player);
    }

    private IEnumerable<ChessMove> GetCastlingMoves(ChessBoard board, Player player)
    {
        var moves = new List<ChessMove>();
        int kingRow = player == Player.Player1 ? 0 : 7;

        if (player == Player.Player1)
        {
            if (!board.WhiteKingMoved && !IsKingInCheck(board, player))
            {
                var kingSideRook = GetPiece(board, kingRow, 7) as ChessPiece;
                if (!board.WhiteRookKingMoved &&
                    kingSideRook is { Owner: Player.Player1, Type: ChessPieceType.Rook } &&
                    GetPiece(board, kingRow, 5) == null &&
                    GetPiece(board, kingRow, 6) == null)
                {
                    if (!IsSquareAttackedBy(board, new Position(kingRow, 5), Player.Player2))
                    {
                        moves.Add(new ChessMove(new Position(kingRow, 4), new Position(kingRow, 6), player) { IsCastling = true });
                    }
                }
                var queenSideRook = GetPiece(board, kingRow, 0) as ChessPiece;
                if (!board.WhiteRookQueenMoved &&
                    queenSideRook is { Owner: Player.Player1, Type: ChessPieceType.Rook } &&
                    GetPiece(board, kingRow, 1) == null &&
                    GetPiece(board, kingRow, 2) == null &&
                    GetPiece(board, kingRow, 3) == null)
                {
                    if (!IsSquareAttackedBy(board, new Position(kingRow, 3), Player.Player2))
                    {
                        moves.Add(new ChessMove(new Position(kingRow, 4), new Position(kingRow, 2), player) { IsCastling = true });
                    }
                }
            }
        }
        else
        {
            if (!board.BlackKingMoved && !IsKingInCheck(board, player))
            {
                var kingSideRook = GetPiece(board, kingRow, 7) as ChessPiece;
                if (!board.BlackRookKingMoved &&
                    kingSideRook is { Owner: Player.Player2, Type: ChessPieceType.Rook } &&
                    GetPiece(board, kingRow, 5) == null &&
                    GetPiece(board, kingRow, 6) == null)
                {
                    if (!IsSquareAttackedBy(board, new Position(kingRow, 5), Player.Player1))
                    {
                        moves.Add(new ChessMove(new Position(kingRow, 4), new Position(kingRow, 6), player) { IsCastling = true });
                    }
                }
                var queenSideRook = GetPiece(board, kingRow, 0) as ChessPiece;
                if (!board.BlackRookQueenMoved &&
                    queenSideRook is { Owner: Player.Player2, Type: ChessPieceType.Rook } &&
                    GetPiece(board, kingRow, 1) == null &&
                    GetPiece(board, kingRow, 2) == null &&
                    GetPiece(board, kingRow, 3) == null)
                {
                    if (!IsSquareAttackedBy(board, new Position(kingRow, 3), Player.Player1))
                    {
                        moves.Add(new ChessMove(new Position(kingRow, 4), new Position(kingRow, 2), player) { IsCastling = true });
                    }
                }
            }
        }

        return moves;
    }

    private IPiece? GetPiece(ChessBoard board, int row, int col)
    {
        return board.GetPiece(new Position(row, col));
    }

    private IEnumerable<ChessMove> GetValidMovesForPiece(ChessPiece piece, ChessBoard board)
    {
        var moves = new List<ChessMove>();

        switch (piece.Type)
        {
            case ChessPieceType.Pawn:
                moves.AddRange(GetPawnMoves(piece, board));
                break;
            case ChessPieceType.Rook:
                moves.AddRange(GetRookMoves(piece, board));
                break;
            case ChessPieceType.Knight:
                moves.AddRange(GetKnightMoves(piece, board));
                break;
            case ChessPieceType.Bishop:
                moves.AddRange(GetBishopMoves(piece, board));
                break;
            case ChessPieceType.Queen:
                moves.AddRange(GetQueenMoves(piece, board));
                break;
            case ChessPieceType.King:
                moves.AddRange(GetKingMoves(piece, board));
                break;
        }

        return moves;
    }

    private IEnumerable<ChessMove> GetPawnMoves(ChessPiece pawn, ChessBoard board)
    {
        var moves = new List<ChessMove>();
        int direction = pawn.Owner == Player.Player1 ? 1 : -1;
        int startRow = pawn.Owner == Player.Player1 ? 1 : 6;

        // Рух вперед на одну клітинку
        var forwardOne = new Position(pawn.Position.Row + direction, pawn.Position.Column);
        if (board.IsValidPosition(forwardOne) && board.GetPiece(forwardOne) == null)
        {
            var move = new ChessMove(pawn.Position, forwardOne, pawn.Owner);
            if (forwardOne.Row == 0 || forwardOne.Row == 7)
            {
                move.IsPromotion = true;
                move.PromotionType = ChessPieceType.Queen; // За замовчуванням ферзь
            }
            moves.Add(move);

            // Рух вперед на дві клітинки (тільки з початкової позиції)
            if (pawn.Position.Row == startRow)
            {
                var forwardTwo = new Position(pawn.Position.Row + 2 * direction, pawn.Position.Column);
                if (board.IsValidPosition(forwardTwo) && board.GetPiece(forwardTwo) == null)
                {
                    moves.Add(new ChessMove(pawn.Position, forwardTwo, pawn.Owner));
                }
            }
        }

        // Захоплення по діагоналі
        for (int colOffset = -1; colOffset <= 1; colOffset += 2)
        {
            var capturePos = new Position(pawn.Position.Row + direction, pawn.Position.Column + colOffset);
            if (board.IsValidPosition(capturePos))
            {
                var targetPiece = board.GetPiece(capturePos);
                if (targetPiece != null && targetPiece.Owner != pawn.Owner)
                {
                    var move = new ChessMove(pawn.Position, capturePos, pawn.Owner);
                    if (capturePos.Row == 0 || capturePos.Row == 7)
                    {
                        move.IsPromotion = true;
                        move.PromotionType = ChessPieceType.Queen;
                    }
                    moves.Add(move);
                }
            }
        }

        // Взяття на проході (en passant)
        if (board.EnPassantTarget.HasValue)
        {
            var epTarget = board.EnPassantTarget.Value;
            if (epTarget.Row == pawn.Position.Row + direction && Math.Abs(epTarget.Column - pawn.Position.Column) == 1)
            {
                var move = new ChessMove(pawn.Position, epTarget, pawn.Owner) { IsEnPassant = true };
                moves.Add(move);
            }
        }

        return moves;
    }

    private IEnumerable<ChessMove> GetRookMoves(ChessPiece rook, ChessBoard board)
    {
        var moves = new List<ChessMove>();
        int[] directions = { -1, 1 };

        // Горизонтальні та вертикальні напрямки
        foreach (int rowDir in directions)
        {
            for (int steps = 1; steps < 8; steps++)
            {
                var newPos = new Position(rook.Position.Row + rowDir * steps, rook.Position.Column);
                if (!board.IsValidPosition(newPos)) break;

                var piece = board.GetPiece(newPos);
                if (piece == null)
                {
                    moves.Add(new ChessMove(rook.Position, newPos, rook.Owner));
                }
                else
                {
                    if (piece.Owner != rook.Owner)
                        moves.Add(new ChessMove(rook.Position, newPos, rook.Owner));
                    break;
                }
            }
        }

        foreach (int colDir in directions)
        {
            for (int steps = 1; steps < 8; steps++)
            {
                var newPos = new Position(rook.Position.Row, rook.Position.Column + colDir * steps);
                if (!board.IsValidPosition(newPos)) break;

                var piece = board.GetPiece(newPos);
                if (piece == null)
                {
                    moves.Add(new ChessMove(rook.Position, newPos, rook.Owner));
                }
                else
                {
                    if (piece.Owner != rook.Owner)
                        moves.Add(new ChessMove(rook.Position, newPos, rook.Owner));
                    break;
                }
            }
        }

        return moves;
    }

    private IEnumerable<ChessMove> GetKnightMoves(ChessPiece knight, ChessBoard board)
    {
        var moves = new List<ChessMove>();
        int[] offsets = { -2, -1, 1, 2 };

        foreach (int rowOffset in offsets)
        {
            foreach (int colOffset in offsets)
            {
                if (Math.Abs(rowOffset) == Math.Abs(colOffset)) continue; // Не діагональ

                var newPos = new Position(knight.Position.Row + rowOffset, knight.Position.Column + colOffset);
                if (!board.IsValidPosition(newPos)) continue;

                var piece = board.GetPiece(newPos);
                if (piece == null || piece.Owner != knight.Owner)
                {
                    moves.Add(new ChessMove(knight.Position, newPos, knight.Owner));
                }
            }
        }

        return moves;
    }

    private IEnumerable<ChessMove> GetBishopMoves(ChessPiece bishop, ChessBoard board)
    {
        var moves = new List<ChessMove>();
        int[] directions = { -1, 1 };

        foreach (int rowDir in directions)
        {
            foreach (int colDir in directions)
            {
                for (int steps = 1; steps < 8; steps++)
                {
                    var newPos = new Position(bishop.Position.Row + rowDir * steps, bishop.Position.Column + colDir * steps);
                    if (!board.IsValidPosition(newPos)) break;

                    var piece = board.GetPiece(newPos);
                    if (piece == null)
                    {
                        moves.Add(new ChessMove(bishop.Position, newPos, bishop.Owner));
                    }
                    else
                    {
                        if (piece.Owner != bishop.Owner)
                            moves.Add(new ChessMove(bishop.Position, newPos, bishop.Owner));
                        break;
                    }
                }
            }
        }

        return moves;
    }

    private IEnumerable<ChessMove> GetQueenMoves(ChessPiece queen, ChessBoard board)
    {
        // Ферзь = тура + слон
        var moves = new List<ChessMove>();
        moves.AddRange(GetRookMoves(queen, board));
        moves.AddRange(GetBishopMoves(queen, board));
        return moves;
    }

    private IEnumerable<ChessMove> GetKingMoves(ChessPiece king, ChessBoard board)
    {
        var moves = new List<ChessMove>();

        for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (int colOffset = -1; colOffset <= 1; colOffset++)
            {
                if (rowOffset == 0 && colOffset == 0) continue;

                var newPos = new Position(king.Position.Row + rowOffset, king.Position.Column + colOffset);
                if (!board.IsValidPosition(newPos)) continue;

                var piece = board.GetPiece(newPos);
                if (piece == null || piece.Owner != king.Owner)
                {
                    moves.Add(new ChessMove(king.Position, newPos, king.Owner));
                }
            }
        }

        return moves;
    }

    public bool IsValidMove(IMove move)
    {
        if (move is not ChessMove chessMove)
            return false;

        if (!chessMove.IsValid())
            return false;

        var board = Board as ChessBoard;
        if (board == null) return false;

        var piece = board.GetPiece(chessMove.From);
        if (piece == null || piece.Owner != chessMove.Player)
            return false;

        var validMoves = GetValidMoves(chessMove.Player).OfType<ChessMove>();
        return validMoves.Any(m => m.From == chessMove.From && m.To == chessMove.To);
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
        return new ChessGame(_logger, Board.Clone(), State, CurrentPlayer);
    }

    private void CheckGameState()
    {
        var board = Board as ChessBoard;
        if (board == null) return;

        // Перевірка на мат (спрощена версія - перевіряємо чи є король)
        bool player1KingExists = false;
        bool player2KingExists = false;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));
                if (piece?.Type == ChessPieceType.King)
                {
                    if (piece.Owner == Player.Player1)
                        player1KingExists = true;
                    else if (piece.Owner == Player.Player2)
                        player2KingExists = true;
                }
            }
        }

        if (!player1KingExists)
        {
            State = GameState.Player2Won;
            _logger.LogInfo("Гра завершена: Player2 переміг (король Player1 захоплений)");
        }
        else if (!player2KingExists)
        {
            State = GameState.Player1Won;
            _logger.LogInfo("Гра завершена: Player1 переміг (король Player2 захоплений)");
        }
        else
        {
            var validMoves = GetValidMoves(CurrentPlayer);
            if (!validMoves.Any())
            {
                if (IsKingInCheck(board, CurrentPlayer))
                {
                    State = CurrentPlayer == Player.Player1 ? GameState.Player2Won : GameState.Player1Won;
                    _logger.LogInfo($"Гра завершена: Мат! Переміг {(State == GameState.Player1Won ? "Player1" : "Player2")}");
                }
                else
                {
                    State = GameState.Draw;
                    _logger.LogInfo("Гра завершена: Пат (немає валідних ходів)");
                }
            }
        }
    }
}

