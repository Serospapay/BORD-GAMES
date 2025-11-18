/**
 * @file: GameStatistics.cs
 * @description: Клас для зберігання статистики ігор
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Статистика для конкретної гри
/// </summary>
public class GameStats
{
    public string GameName { get; set; } = string.Empty;
    public int TotalGames { get; set; }
    public int Player1Wins { get; set; }
    public int Player2Wins { get; set; }
    public int Draws { get; set; }
    public int LongestGame { get; set; } // в ходах
    public int ShortestGame { get; set; } // в ходах
    public DateTime LastPlayed { get; set; }

    public double Player1WinRate => TotalGames > 0 ? (double)Player1Wins / TotalGames * 100 : 0;
    public double Player2WinRate => TotalGames > 0 ? (double)Player2Wins / TotalGames * 100 : 0;
    public double DrawRate => TotalGames > 0 ? (double)Draws / TotalGames * 100 : 0;
}

/// <summary>
/// Загальна статистика всіх ігор
/// </summary>
public class GameStatistics
{
    public Dictionary<string, GameStats> Games { get; set; } = new();
    public int TotalGamesPlayed => Games.Values.Sum(g => g.TotalGames);
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public GameStats GetOrCreateGameStats(string gameName)
    {
        if (!Games.ContainsKey(gameName))
        {
            Games[gameName] = new GameStats
            {
                GameName = gameName,
                ShortestGame = int.MaxValue
            };
        }
        return Games[gameName];
    }

    public void RecordGameResult(string gameName, Player? winner, int moveCount)
    {
        var stats = GetOrCreateGameStats(gameName);
        stats.TotalGames++;
        stats.LastPlayed = DateTime.Now;

        if (winner == Player.Player1)
            stats.Player1Wins++;
        else if (winner == Player.Player2)
            stats.Player2Wins++;
        else
            stats.Draws++;

        if (moveCount > stats.LongestGame)
            stats.LongestGame = moveCount;

        if (moveCount < stats.ShortestGame)
            stats.ShortestGame = moveCount;

        LastUpdated = DateTime.Now;
    }
}



