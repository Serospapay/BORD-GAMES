/**
 * @file: ChessPiece.cs
 * @description: Реалізація шахової фігури
 * @dependencies: IPiece, Position, Player, ChessPieceType
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.Chess;

/// <summary>
/// Шахова фігура
/// </summary>
public class ChessPiece : IPiece
{
    public Position Position { get; private set; }
    public Player Owner { get; }
    public ChessPieceType Type { get; }
    public string Symbol { get; }

    private static readonly Dictionary<(ChessPieceType, Player), string> Symbols = new()
    {
        { (ChessPieceType.Pawn, Player.Player1), "♙" },
        { (ChessPieceType.Rook, Player.Player1), "♖" },
        { (ChessPieceType.Knight, Player.Player1), "♘" },
        { (ChessPieceType.Bishop, Player.Player1), "♗" },
        { (ChessPieceType.Queen, Player.Player1), "♕" },
        { (ChessPieceType.King, Player.Player1), "♔" },
        { (ChessPieceType.Pawn, Player.Player2), "♟" },
        { (ChessPieceType.Rook, Player.Player2), "♜" },
        { (ChessPieceType.Knight, Player.Player2), "♞" },
        { (ChessPieceType.Bishop, Player.Player2), "♝" },
        { (ChessPieceType.Queen, Player.Player2), "♛" },
        { (ChessPieceType.King, Player.Player2), "♚" }
    };

    public ChessPiece(Position position, Player owner, ChessPieceType type)
    {
        Position = position;
        Owner = owner;
        Type = type;
        Symbol = Symbols.GetValueOrDefault((type, owner), "?");
    }

    public void MoveTo(Position newPosition)
    {
        Position = newPosition;
    }

    public override string ToString()
    {
        return $"{Owner} {Type} at {Position}";
    }
}

