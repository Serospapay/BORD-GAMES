/**
 * @file: ReversiPiece.cs
 * @description: Реалізація фігури в Reversi/Othello
 * @dependencies: IPiece, Position, Player
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.Reversi;

/// <summary>
/// Фігура в Reversi/Othello
/// </summary>
public class ReversiPiece : IPiece
{
    public Position Position { get; private set; }
    public Player Owner { get; }
    public string Symbol { get; }

    public ReversiPiece(Position position, Player owner)
    {
        Position = position;
        Owner = owner;
        Symbol = owner == Player.Player1 ? "●" : "○";
    }

    public void MoveTo(Position newPosition)
    {
        Position = newPosition;
    }

    public override string ToString()
    {
        return $"{Owner} piece at {Position}";
    }
}

