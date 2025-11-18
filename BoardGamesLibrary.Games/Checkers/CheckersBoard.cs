/**
 * @file: CheckersBoard.cs
 * @description: Реалізація дошки для шашок
 * @dependencies: BaseBoard, CheckersPiece, Position
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Checkers;

/// <summary>
/// Дошка для шашок 8x8
/// </summary>
public class CheckersBoard : BaseBoard
{
    private readonly ILogger? _logger;

    public CheckersBoard(ILogger? logger = null) : base(8, 8)
    {
        _logger = logger;
        InitializeBoard();
    }

    private CheckersBoard(CheckersBoard other) : base(8, 8)
    {
        _logger = other._logger;
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = other._pieces[row, col];
                if (piece is CheckersPiece checkersPiece)
                {
                    _pieces[row, col] = new CheckersPiece(
                        checkersPiece.Position, 
                        checkersPiece.Owner, 
                        checkersPiece.IsKing);
                }
            }
        }
    }

    private void InitializeBoard()
    {
        _logger?.LogDebug("Ініціалізація дошки для шашок");

        // Розміщення фігур Player1 (внизу, темні клітинки)
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if ((row + col) % 2 == 1) // Темні клітинки
                {
                    var position = new Position(row, col);
                    SetPiece(position, new CheckersPiece(position, Player.Player1));
                }
            }
        }

        // Розміщення фігур Player2 (вгорі, темні клітинки)
        for (int row = 5; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if ((row + col) % 2 == 1) // Темні клітинки
                {
                    var position = new Position(row, col);
                    SetPiece(position, new CheckersPiece(position, Player.Player2));
                }
            }
        }

        _logger?.LogInfo("Дошка для шашок ініціалізована");
    }

    public override IBoard Clone()
    {
        return new CheckersBoard(this);
    }

    public new CheckersPiece? GetPiece(Position position)
    {
        return base.GetPiece(position) as CheckersPiece;
    }

    /// <summary>
    /// Перевіряє, чи клітинка темна (придатна для розміщення фігур)
    /// </summary>
    public bool IsDarkSquare(Position position)
    {
        return (position.Row + position.Column) % 2 == 1;
    }
}

