/**
 * @file: ConsoleLogger.cs
 * @description: Реалізація логгера для виводу в консоль з кольоровим форматуванням
 * @dependencies: ILogger
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Logging;

/// <summary>
/// Консольний логгер з підтримкою кольорового виводу
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly LogLevel _minimumLevel;
    private readonly object _lockObject = new();

    public ConsoleLogger(LogLevel minimumLevel = LogLevel.Debug)
    {
        _minimumLevel = minimumLevel;
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (level < _minimumLevel)
            return;

        lock (_lockObject)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelName = level.ToString().ToUpper().PadRight(8);
            
            ConsoleColor color = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            Console.Write($"[{timestamp}] [{levelName}] ");
            Console.ResetColor();
            Console.WriteLine(message);

            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception: {exception.GetType().Name}");
                Console.WriteLine($"Message: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    Console.WriteLine($"StackTrace: {exception.StackTrace}");
                }
                Console.ResetColor();
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

