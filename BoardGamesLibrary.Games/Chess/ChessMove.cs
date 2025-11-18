/**
 * @file: ChessMove.cs
 * @description: Реалізація ходу в шахах
 * @dependencies: BaseMove, Position, Player
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.Chess;

/// <summary>
/// Хід в шахах
/// </summary>
public class ChessMove : BaseMove
{
    public ChessPiece? CapturedPiece { get; set; }
    public bool IsCastling { get; set; }
    public bool IsEnPassant { get; set; }
    public bool IsPromotion { get; set; }
    public ChessPieceType? PromotionType { get; set; }

    public ChessMove(Position from, Position to, Player player) 
        : base(from, to, player)
    {
    }

    public override string ToString()
    {
        var moveStr = base.ToString();
        if (IsCastling) moveStr += " (Castling)";
        if (IsEnPassant) moveStr += " (En Passant)";
        if (IsPromotion && PromotionType.HasValue) moveStr += $" (Promotion to {PromotionType})";
        return moveStr;
    }
}

