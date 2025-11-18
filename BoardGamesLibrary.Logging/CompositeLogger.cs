/**
 * @file: CompositeLogger.cs
 * @description: Композитний логгер для одночасного використання кількох логгерів
 * @dependencies: ILogger
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Logging;

/// <summary>
/// Композитний логгер, який дозволяє використовувати кілька логгерів одночасно
/// </summary>
public class CompositeLogger : ILogger
{
    private readonly IList<ILogger> _loggers;

    public CompositeLogger(params ILogger[] loggers)
    {
        _loggers = loggers?.ToList() ?? new List<ILogger>();
    }

    public void AddLogger(ILogger logger)
    {
        if (logger != null)
        {
            _loggers.Add(logger);
        }
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        foreach (var logger in _loggers)
        {
            try
            {
                logger.Log(level, message, exception);
            }
            catch
            {
                // Ігноруємо помилки логування, щоб не порушити роботу основного коду
            }
        }
    }

    public void LogDebug(string message)
    {
        Log(LogLevel.Debug, message);
    }

    public void LogInfo(string message)
    {
        Log(LogLevel.Info, message);
    }

    public void LogWarning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        Log(LogLevel.Error, message, exception);
    }

    public void LogCritical(string message, Exception? exception = null)
    {
        Log(LogLevel.Critical, message, exception);
    }
}

