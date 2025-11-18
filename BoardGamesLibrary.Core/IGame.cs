/**
 * @file: IGame.cs
 * @description: Інтерфейс для ігор з дошкою
 * @dependencies: IBoard, IMove, Player
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Стан гри
/// </summary>
public enum GameState
{
    NotStarted,
    InProgress,
    Player1Won,
    Player2Won,
    Draw,
    Paused
}

/// <summary>
/// Інтерфейс для ігор з дошкою
/// </summary>
public interface IGame
{
    /// <summary>
    /// Поточна дошка
    /// </summary>
    IBoard Board { get; }

    /// <summary>
    /// Поточний стан гри
    /// </summary>
    GameState State { get; }

    /// <summary>
    /// Поточний гравець
    /// </summary>
    Player CurrentPlayer { get; }

    /// <summary>
    /// Виконує хід
    /// </summary>
    bool MakeMove(IMove move);

    /// <summary>
    /// Отримує список валідних ходів для поточного гравця
    /// </summary>
    IEnumerable<IMove> GetValidMoves(Player player);

    /// <summary>
    /// Перевіряє, чи хід валідний
    /// </summary>
    bool IsValidMove(IMove move);

    /// <summary>
    /// Починає нову гру
    /// </summary>
    void StartNewGame();

    /// <summary>
    /// Перевіряє, чи гра завершена
    /// </summary>
    bool IsGameOver();

    /// <summary>
    /// Отримує переможця (якщо гра завершена)
    /// </summary>
    Player? GetWinner();
}

