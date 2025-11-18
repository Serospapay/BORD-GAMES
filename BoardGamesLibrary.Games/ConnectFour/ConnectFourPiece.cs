/**
 * @file: ConnectFourPiece.cs
 * @description: Реалізація фігури в Connect Four
 * @dependencies: IPiece, Position, Player
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.ConnectFour;

/// <summary>
/// Фігура в Connect Four
/// </summary>
public class ConnectFourPiece : IPiece
{
    public Position Position { get; private set; }
    public Player Owner { get; }
    public string Symbol { get; }

    public ConnectFourPiece(Position position, Player owner)
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

