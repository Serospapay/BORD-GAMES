/**
 * @file: FileLogger.cs
 * @description: Реалізація логгера для запису в файл з ротацією логів
 * @dependencies: ILogger
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Logging;

/// <summary>
/// Файловий логгер з підтримкою ротації логів
/// </summary>
public class FileLogger : ILogger, IDisposable
{
    private readonly LogLevel _minimumLevel;
    private readonly string _logDirectory;
    private readonly string _logFileName;
    private readonly object _lockObject = new();
    private StreamWriter? _writer;
    private DateTime _currentDate;

    public FileLogger(string logDirectory = "logs", string logFileName = "game.log", LogLevel minimumLevel = LogLevel.Debug)
    {
        _minimumLevel = minimumLevel;
        _logDirectory = logDirectory;
        _logFileName = logFileName;
        _currentDate = DateTime.Today;

        Directory.CreateDirectory(_logDirectory);
        InitializeWriter();
    }

    private void InitializeWriter()
    {
        var filePath = Path.Combine(_logDirectory, $"{DateTime.Today:yyyy-MM-dd}_{_logFileName}");
        _writer = new StreamWriter(filePath, append: true)
        {
            AutoFlush = true
        };
        _currentDate = DateTime.Today;
    }

    private void CheckDateRotation()
    {
        if (DateTime.Today != _currentDate)
        {
            _writer?.Dispose();
            InitializeWriter();
        }
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (level < _minimumLevel)
            return;

        lock (_lockObject)
        {
            CheckDateRotation();

            if (_writer == null)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelName = level.ToString().ToUpper().PadRight(8);
            
            _writer.WriteLine($"[{timestamp}] [{levelName}] {message}");

            if (exception != null)
            {
                _writer.WriteLine($"Exception: {exception.GetType().Name}");
                _writer.WriteLine($"Message: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    _writer.WriteLine($"StackTrace: {exception.StackTrace}");
                }
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

    public void Dispose()
    {
        _writer?.Dispose();
    }
}

