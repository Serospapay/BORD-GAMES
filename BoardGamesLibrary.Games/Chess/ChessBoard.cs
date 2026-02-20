/**
 * @file: ChessBoard.cs
 * @description: Реалізація шахової дошки
 * @dependencies: BaseBoard, ChessPiece, Position
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Chess;

/// <summary>
/// Шахова дошка 8x8
/// </summary>
public class ChessBoard : BaseBoard
{
    private readonly ILogger? _logger;

    public bool WhiteKingMoved { get; private set; }
    public bool WhiteRookKingMoved { get; private set; }
    public bool WhiteRookQueenMoved { get; private set; }
    public bool BlackKingMoved { get; private set; }
    public bool BlackRookKingMoved { get; private set; }
    public bool BlackRookQueenMoved { get; private set; }

    public Position? EnPassantTarget { get; set; }

    public ChessBoard(ILogger? logger = null) : base(8, 8)
    {
        _logger = logger;
        InitializeBoard();
    }

    private ChessBoard(ChessBoard other) : base(8, 8)
    {
        _logger = other._logger;
        WhiteKingMoved = other.WhiteKingMoved;
        WhiteRookKingMoved = other.WhiteRookKingMoved;
        WhiteRookQueenMoved = other.WhiteRookQueenMoved;
        BlackKingMoved = other.BlackKingMoved;
        BlackRookKingMoved = other.BlackRookKingMoved;
        BlackRookQueenMoved = other.BlackRookQueenMoved;
        EnPassantTarget = other.EnPassantTarget;
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = other._pieces[row, col];
                if (piece is ChessPiece chessPiece)
                {
                    _pieces[row, col] = new ChessPiece(chessPiece.Position, chessPiece.Owner, chessPiece.Type);
                }
            }
        }
    }

    public void MarkPieceMoved(Position from, ChessPiece piece)
    {
        if (piece.Type == ChessPieceType.King)
        {
            if (piece.Owner == Player.Player1) WhiteKingMoved = true;
            else BlackKingMoved = true;
        }
        else if (piece.Type == ChessPieceType.Rook)
        {
            if (piece.Owner == Player.Player1)
            {
                if (from.Row == 0 && from.Column == 7) WhiteRookKingMoved = true;
                else if (from.Row == 0 && from.Column == 0) WhiteRookQueenMoved = true;
            }
            else
            {
                if (from.Row == 7 && from.Column == 7) BlackRookKingMoved = true;
                else if (from.Row == 7 && from.Column == 0) BlackRookQueenMoved = true;
            }
        }
    }

    public void MarkRookCaptured(Position position, Player rookOwner)
    {
        if (rookOwner == Player.Player1)
        {
            if (position.Row == 0 && position.Column == 7) WhiteRookKingMoved = true;
            else if (position.Row == 0 && position.Column == 0) WhiteRookQueenMoved = true;
        }
        else if (rookOwner == Player.Player2)
        {
            if (position.Row == 7 && position.Column == 7) BlackRookKingMoved = true;
            else if (position.Row == 7 && position.Column == 0) BlackRookQueenMoved = true;
        }
    }

    private void InitializeBoard()
    {
        _logger?.LogDebug("Ініціалізація шахової дошки");

        // Розміщення фігур Player1 (білі, внизу)
        SetPiece(new Position(0, 0), new ChessPiece(new Position(0, 0), Player.Player1, ChessPieceType.Rook));
        SetPiece(new Position(0, 1), new ChessPiece(new Position(0, 1), Player.Player1, ChessPieceType.Knight));
        SetPiece(new Position(0, 2), new ChessPiece(new Position(0, 2), Player.Player1, ChessPieceType.Bishop));
        SetPiece(new Position(0, 3), new ChessPiece(new Position(0, 3), Player.Player1, ChessPieceType.Queen));
        SetPiece(new Position(0, 4), new ChessPiece(new Position(0, 4), Player.Player1, ChessPieceType.King));
        SetPiece(new Position(0, 5), new ChessPiece(new Position(0, 5), Player.Player1, ChessPieceType.Bishop));
        SetPiece(new Position(0, 6), new ChessPiece(new Position(0, 6), Player.Player1, ChessPieceType.Knight));
        SetPiece(new Position(0, 7), new ChessPiece(new Position(0, 7), Player.Player1, ChessPieceType.Rook));

        for (int col = 0; col < 8; col++)
        {
            SetPiece(new Position(1, col), new ChessPiece(new Position(1, col), Player.Player1, ChessPieceType.Pawn));
        }

        // Розміщення фігур Player2 (чорні, вгорі)
        SetPiece(new Position(7, 0), new ChessPiece(new Position(7, 0), Player.Player2, ChessPieceType.Rook));
        SetPiece(new Position(7, 1), new ChessPiece(new Position(7, 1), Player.Player2, ChessPieceType.Knight));
        SetPiece(new Position(7, 2), new ChessPiece(new Position(7, 2), Player.Player2, ChessPieceType.Bishop));
        SetPiece(new Position(7, 3), new ChessPiece(new Position(7, 3), Player.Player2, ChessPieceType.Queen));
        SetPiece(new Position(7, 4), new ChessPiece(new Position(7, 4), Player.Player2, ChessPieceType.King));
        SetPiece(new Position(7, 5), new ChessPiece(new Position(7, 5), Player.Player2, ChessPieceType.Bishop));
        SetPiece(new Position(7, 6), new ChessPiece(new Position(7, 6), Player.Player2, ChessPieceType.Knight));
        SetPiece(new Position(7, 7), new ChessPiece(new Position(7, 7), Player.Player2, ChessPieceType.Rook));

        for (int col = 0; col < 8; col++)
        {
            SetPiece(new Position(6, col), new ChessPiece(new Position(6, col), Player.Player2, ChessPieceType.Pawn));
        }

        _logger?.LogInfo("Шахова дошка ініціалізована");
    }

    public override IBoard Clone()
    {
        return new ChessBoard(this);
    }

    public new ChessPiece? GetPiece(Position position)
    {
        return base.GetPiece(position) as ChessPiece;
    }
}

