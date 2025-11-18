/**
 * @file: ConnectFourMove.cs
 * @description: Реалізація ходу в Connect Four
 * @dependencies: BaseMove, Position, Player
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.ConnectFour;

/// <summary>
/// Хід в Connect Four (використовує стовпець замість позиції)
/// </summary>
public class ConnectFourMove : BaseMove
{
    public int Column { get; }

    public ConnectFourMove(int column, Player player) 
        : base(new Position(-1, column), new Position(-1, column), player)
    {
        Column = column;
    }

    public override bool IsValid()
    {
        return Column >= 0 && Column < 7 && Player.IsValid();
    }

    public override string ToString()
    {
        return $"{Player}: Column {Column}";
    }
}

