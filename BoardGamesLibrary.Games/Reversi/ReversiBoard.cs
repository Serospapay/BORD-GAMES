/**
 * @file: ReversiBoard.cs
 * @description: Реалізація дошки для Reversi/Othello
 * @dependencies: BaseBoard, Position
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Reversi;

/// <summary>
/// Дошка для Reversi/Othello 8x8
/// </summary>
public class ReversiBoard : BaseBoard
{
    private readonly ILogger? _logger;

    public ReversiBoard(ILogger? logger = null) : base(8, 8)
    {
        _logger = logger;
        InitializeBoard();
    }

    private ReversiBoard(ReversiBoard other) : base(8, 8)
    {
        _logger = other._logger;
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                _pieces[row, col] = other._pieces[row, col];
            }
        }
    }

    private void InitializeBoard()
    {
        _logger?.LogDebug("Ініціалізація дошки для Reversi");

        // Початкова конфігурація Reversi
        SetPiece(new Position(3, 3), new ReversiPiece(new Position(3, 3), Player.Player1));
        SetPiece(new Position(3, 4), new ReversiPiece(new Position(3, 4), Player.Player2));
        SetPiece(new Position(4, 3), new ReversiPiece(new Position(4, 3), Player.Player2));
        SetPiece(new Position(4, 4), new ReversiPiece(new Position(4, 4), Player.Player1));

        _logger?.LogInfo("Дошка для Reversi ініціалізована");
    }

    public override IBoard Clone()
    {
        return new ReversiBoard(this);
    }

    public new ReversiPiece? GetPiece(Position position)
    {
        return base.GetPiece(position) as ReversiPiece;
    }
}

