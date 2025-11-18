/**
 * @file: BaseMove.cs
 * @description: Базовий клас для ходу в грі
 * @dependencies: IMove, Position, Player
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Базовий клас для ходу в грі
/// </summary>
public abstract class BaseMove : IMove
{
    public Position From { get; }
    public Position To { get; }
    public Player Player { get; }

    protected BaseMove(Position from, Position to, Player player)
    {
        From = from;
        To = to;
        Player = player;
    }

    public virtual bool IsValid()
    {
        return From != To && Player.IsValid();
    }

    public override string ToString()
    {
        return $"{Player}: {From} -> {To}";
    }
}

