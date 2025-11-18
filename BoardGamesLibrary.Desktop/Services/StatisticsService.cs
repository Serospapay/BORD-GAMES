/**
 * @file: StatisticsService.cs
 * @description: Сервіс для збереження та завантаження статистики ігор
 * @dependencies: GameStatistics, System.Text.Json
 * @created: 2024-12-19
 */

using System.IO;
using System.Text.Json;
using BoardGamesLibrary.Core;

namespace BoardGamesLibrary.Desktop.Services;

/// <summary>
/// Сервіс для роботи зі статистикою ігор
/// </summary>
public class StatisticsService
{
    private readonly string _statisticsFilePath;
    private GameStatistics? _statistics;
    private readonly object _lockObject = new();

    public StatisticsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BoardGamesLibrary");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _statisticsFilePath = Path.Combine(appDataPath, "statistics.json");
    }

    /// <summary>
    /// Завантажує статистику з файлу
    /// </summary>
    public GameStatistics LoadStatistics()
    {
        lock (_lockObject)
        {
            if (_statistics != null)
                return _statistics;

            if (File.Exists(_statisticsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_statisticsFilePath);
                    _statistics = JsonSerializer.Deserialize<GameStatistics>(json) ?? new GameStatistics();
                    return _statistics;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Помилка завантаження статистики: {ex.Message}");
                    _statistics = new GameStatistics();
                    return _statistics;
                }
            }

            _statistics = new GameStatistics();
            return _statistics;
        }
    }

    /// <summary>
    /// Зберігає статистику в файл
    /// </summary>
    public void SaveStatistics(GameStatistics statistics)
    {
        lock (_lockObject)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(statistics, options);
                File.WriteAllText(_statisticsFilePath, json);
                _statistics = statistics;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка збереження статистики: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Оновлює статистику після завершення гри
    /// </summary>
    public void RecordGameResult(string gameName, Player? winner, int moveCount)
    {
        var stats = LoadStatistics();
        stats.RecordGameResult(gameName, winner, moveCount);
        SaveStatistics(stats);
    }

    /// <summary>
    /// Отримує статистику для конкретної гри
    /// </summary>
    public GameStats? GetGameStats(string gameName)
    {
        var stats = LoadStatistics();
        return stats.Games.TryGetValue(gameName, out var gameStats) ? gameStats : null;
    }

    /// <summary>
    /// Отримує загальну статистику
    /// </summary>
    public GameStatistics GetAllStatistics()
    {
        return LoadStatistics();
    }
}

