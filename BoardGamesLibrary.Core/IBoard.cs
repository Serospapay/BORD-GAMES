/**
 * @file: IBoard.cs
 * @description: Інтерфейс для ігрової дошки
 * @dependencies: Position, IPiece, Player
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Інтерфейс для ігрової дошки
/// </summary>
public interface IBoard
{
    /// <summary>
    /// Кількість рядків
    /// </summary>
    int Rows { get; }

    /// <summary>
    /// Кількість стовпців
    /// </summary>
    int Columns { get; }

    /// <summary>
    /// Отримує фігуру на вказаній позиції
    /// </summary>
    IPiece? GetPiece(Position position);

    /// <summary>
    /// Встановлює фігуру на вказану позицію
    /// </summary>
    void SetPiece(Position position, IPiece? piece);

    /// <summary>
    /// Перевіряє, чи позиція в межах дошки
    /// </summary>
    bool IsValidPosition(Position position);

    /// <summary>
    /// Очищає дошку
    /// </summary>
    void Clear();

    /// <summary>
    /// Отримує копію стану дошки
    /// </summary>
    IBoard Clone();
}

