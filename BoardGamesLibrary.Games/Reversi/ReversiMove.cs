/**
 * @file: ReversiMove.cs
 * @description: Реалізація ходу в Reversi/Othello
 * @dependencies: BaseMove, Position, Player
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.Reversi;

/// <summary>
/// Хід в Reversi/Othello
/// </summary>
public class ReversiMove : BaseMove
{
    public List<Position> FlippedPositions { get; set; }

    public ReversiMove(Position position, Player player) 
        : base(position, position, player)
    {
        FlippedPositions = new List<Position>();
    }

    public override string ToString()
    {
        return $"{Player}: {From} (flipped {FlippedPositions.Count} pieces)";
    }
}

