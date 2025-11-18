/**
 * @file: CheckersMove.cs
 * @description: Реалізація ходу в шашках
 * @dependencies: BaseMove, Position, Player
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.Checkers;

/// <summary>
/// Хід в шашках
/// </summary>
public class CheckersMove : BaseMove
{
    public List<Position> CapturedPositions { get; set; }
    public bool IsJump { get; set; }
    public bool IsPromotion { get; set; }

    public CheckersMove(Position from, Position to, Player player) 
        : base(from, to, player)
    {
        CapturedPositions = new List<Position>();
        IsJump = false;
        IsPromotion = false;
    }

    public override string ToString()
    {
        var moveStr = base.ToString();
        if (IsJump) moveStr += $" (Jump, captured: {CapturedPositions.Count})";
        if (IsPromotion) moveStr += " (Promotion to King)";
        return moveStr;
    }
}

