/**
 * @file: ErrorHandler.cs
 * @description: Централізований обробник помилок з логуванням та відновленням
 * @dependencies: ILogger
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Logging;

/// <summary>
/// Централізований обробник помилок
/// </summary>
public class ErrorHandler
{
    private readonly ILogger _logger;

    public ErrorHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Безпечно виконує дію з обробкою помилок
    /// </summary>
    public T? ExecuteSafely<T>(Func<T> action, T? defaultValue = default, string? context = null)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Помилка при виконанні операції: {ex.Message}" 
                : $"{context}: {ex.Message}";
            
            _logger.LogError(message, ex);
            return defaultValue;
        }
    }

    /// <summary>
    /// Безпечно виконує дію без повернення значення
    /// </summary>
    public void ExecuteSafely(Action action, string? context = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Помилка при виконанні операції: {ex.Message}" 
                : $"{context}: {ex.Message}";
            
            _logger.LogError(message, ex);
        }
    }

    /// <summary>
    /// Безпечно виконує асинхронну дію
    /// </summary>
    public async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, T? defaultValue = default, string? context = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Помилка при виконанні асинхронної операції: {ex.Message}" 
                : $"{context}: {ex.Message}";
            
            _logger.LogError(message, ex);
            return defaultValue;
        }
    }

    /// <summary>
    /// Безпечно виконує асинхронну дію без повернення значення
    /// </summary>
    public async Task ExecuteSafelyAsync(Func<Task> action, string? context = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Помилка при виконанні асинхронної операції: {ex.Message}" 
                : $"{context}: {ex.Message}";
            
            _logger.LogError(message, ex);
        }
    }
}

