/**
 * @file: IAIPlayer.cs
 * @description: Інтерфейс для AI гравця
 * @dependencies: IGame, IMove, Player
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Рівні складності AI
/// </summary>
public enum AIDifficulty
{
    Easy,      // Легкий - випадкові ходи з базовою перевіркою
    Medium,    // Середній - мінімакс з обмеженою глибиною
    Hard       // Важкий - мінімакс з повною глибиною та евристиками
}

/// <summary>
/// Інтерфейс для AI гравця
/// </summary>
public interface IAIPlayer
{
    /// <summary>
    /// Рівень складності AI
    /// </summary>
    AIDifficulty Difficulty { get; }

    /// <summary>
    /// Гравець, якого представляє AI
    /// </summary>
    Player Player { get; }

    /// <summary>
    /// Обирає найкращий хід для поточної позиції
    /// </summary>
    IMove? ChooseMove(IGame game);
}

