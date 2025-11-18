/**
 * @file: ILogger.cs
 * @description: Інтерфейс для системи логування з підтримкою різних рівнів логів
 * @dependencies: None
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Logging;

/// <summary>
/// Рівні логування
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

/// <summary>
/// Інтерфейс для системи логування
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Логує повідомлення з вказаним рівнем
    /// </summary>
    void Log(LogLevel level, string message, Exception? exception = null);

    /// <summary>
    /// Логує повідомлення рівня Debug
    /// </summary>
    void LogDebug(string message);

    /// <summary>
    /// Логує повідомлення рівня Info
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Логує повідомлення рівня Warning
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Логує повідомлення рівня Error
    /// </summary>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Логує повідомлення рівня Critical
    /// </summary>
    void LogCritical(string message, Exception? exception = null);
}

