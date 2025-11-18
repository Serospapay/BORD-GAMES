/**
 * @file: Player.cs
 * @description: Перелік гравців для ігор з дошкою
 * @dependencies: None
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Перелік гравців
/// </summary>
public enum Player
{
    None = 0,
    Player1 = 1,
    Player2 = 2
}

/// <summary>
/// Розширення для переліку Player
/// </summary>
public static class PlayerExtensions
{
    /// <summary>
    /// Отримує протилежного гравця
    /// </summary>
    public static Player GetOpponent(this Player player)
    {
        return player switch
        {
            Player.Player1 => Player.Player2,
            Player.Player2 => Player.Player1,
            _ => Player.None
        };
    }

    /// <summary>
    /// Перевіряє, чи гравець є валідним (не None)
    /// </summary>
    public static bool IsValid(this Player player)
    {
        return player != Player.None;
    }
}

