/**
 * @file: IMove.cs
 * @description: Інтерфейс для ходу в грі
 * @dependencies: Position, Player
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Інтерфейс для ходу в грі
/// </summary>
public interface IMove
{
    /// <summary>
    /// Початкова позиція
    /// </summary>
    Position From { get; }

    /// <summary>
    /// Кінцева позиція
    /// </summary>
    Position To { get; }

    /// <summary>
    /// Гравець, який робить хід
    /// </summary>
    Player Player { get; }

    /// <summary>
    /// Перевіряє валідність ходу
    /// </summary>
    bool IsValid();
}

