/**
 * @file: CheckersPiece.cs
 * @description: Реалізація фігури в шашках
 * @dependencies: IPiece, Position, Player
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Games.Checkers;

/// <summary>
/// Фігура в шашках
/// </summary>
public class CheckersPiece : IPiece
{
    public Position Position { get; private set; }
    public Player Owner { get; }
    public bool IsKing { get; private set; }
    public string Symbol { get; private set; }

    public CheckersPiece(Position position, Player owner, bool isKing = false)
    {
        Position = position;
        Owner = owner;
        IsKing = isKing;
        Symbol = string.Empty; // Ініціалізація перед викликом UpdateSymbol
        UpdateSymbol();
    }

    public void MoveTo(Position newPosition)
    {
        Position = newPosition;
        
        // Перевірка на перетворення в дамку
        if (!IsKing)
        {
            if ((Owner == Player.Player1 && newPosition.Row == 7) ||
                (Owner == Player.Player2 && newPosition.Row == 0))
            {
                IsKing = true;
                UpdateSymbol();
            }
        }
    }

    public void PromoteToKing()
    {
        IsKing = true;
        UpdateSymbol();
    }

    private void UpdateSymbol()
    {
        if (IsKing)
        {
            Symbol = Owner == Player.Player1 ? "♔" : "♚";
        }
        else
        {
            Symbol = Owner == Player.Player1 ? "●" : "○";
        }
    }

    public override string ToString()
    {
        var type = IsKing ? "King" : "Piece";
        return $"{Owner} {type} at {Position}";
    }
}

