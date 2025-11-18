/**
 * @file: IPiece.cs
 * @description: Інтерфейс для фігур на дошці
 * @dependencies: Position, Player
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Інтерфейс для фігур на дошці
/// </summary>
public interface IPiece
{
    /// <summary>
    /// Позиція фігури на дошці
    /// </summary>
    Position Position { get; }

    /// <summary>
    /// Гравець, якому належить фігура
    /// </summary>
    Player Owner { get; }

    /// <summary>
    /// Символ для відображення фігури
    /// </summary>
    string Symbol { get; }

    /// <summary>
    /// Переміщує фігуру на нову позицію
    /// </summary>
    void MoveTo(Position newPosition);
}

