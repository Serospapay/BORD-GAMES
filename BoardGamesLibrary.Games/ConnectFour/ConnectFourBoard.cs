/**
 * @file: ConnectFourBoard.cs
 * @description: Реалізація дошки для Connect Four
 * @dependencies: BaseBoard, Position
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.ConnectFour;

/// <summary>
/// Дошка для Connect Four 6x7 (6 рядків, 7 стовпців)
/// </summary>
public class ConnectFourBoard : BaseBoard
{
    private readonly ILogger? _logger;

    public ConnectFourBoard(ILogger? logger = null) : base(6, 7)
    {
        _logger = logger;
        _logger?.LogDebug("Ініціалізація дошки для Connect Four");
    }

    private ConnectFourBoard(ConnectFourBoard other) : base(6, 7)
    {
        _logger = other._logger;
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                _pieces[row, col] = other._pieces[row, col];
            }
        }
    }

    public override IBoard Clone()
    {
        return new ConnectFourBoard(this);
    }

    public new ConnectFourPiece? GetPiece(Position position)
    {
        return base.GetPiece(position) as ConnectFourPiece;
    }

    /// <summary>
    /// Отримує найнижчу вільну позицію в стовпці
    /// </summary>
    public Position? GetLowestEmptyPosition(int column)
    {
        if (column < 0 || column >= Columns)
            return null;

        for (int row = Rows - 1; row >= 0; row--)
        {
            var position = new Position(row, column);
            if (GetPiece(position) == null)
                return position;
        }

        return null; // Стовпець повний
    }
}

