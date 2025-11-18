/**
 * @file: Program.cs
 * @description: Головний файл консольного UI для бібліотеки ігор
 * @dependencies: All game libraries, Logging
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Chess;
using BoardGamesLibrary.Games.Checkers;
using BoardGamesLibrary.Games.Reversi;
using BoardGamesLibrary.Games.ConnectFour;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.UI;

class Program
{
    private static ILogger? _logger;
    private static ErrorHandler? _errorHandler;

    static void Main(string[] args)
    {
        // Ініціалізація логування
        var consoleLogger = new ConsoleLogger(LogLevel.Info);
        var fileLogger = new FileLogger("logs", "boardgames.log", LogLevel.Debug);
        _logger = new CompositeLogger(consoleLogger, fileLogger);
        _errorHandler = new ErrorHandler(_logger);

        _logger.LogInfo("=== Бібліотека ігор з дошкою ===");
        _logger.LogInfo("Версія 1.0");

        try
        {
            ShowMainMenu();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Критична помилка в програмі", ex);
            Console.WriteLine("\nВиникла критична помилка. Деталі в логах.");
        }
        finally
        {
            if (fileLogger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    static void ShowMainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     БІБЛІОТЕКА ІГОР З ДОШКОЮ          ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Оберіть гру:");
            Console.WriteLine("1. Шахи");
            Console.WriteLine("2. Шашки");
            Console.WriteLine("3. Reversi/Othello");
            Console.WriteLine("4. Connect Four");
            Console.WriteLine("0. Вихід");
            Console.WriteLine();
            Console.Write("Ваш вибір: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    PlayChess();
                    break;
                case "2":
                    PlayCheckers();
                    break;
                case "3":
                    PlayReversi();
                    break;
                case "4":
                    PlayConnectFour();
                    break;
                case "0":
                    _logger?.LogInfo("Вихід з програми");
                    return;
                default:
                    Console.WriteLine("Невірний вибір. Натисніть будь-яку клавішу...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    static void PlayChess()
    {
        if (_logger == null || _errorHandler == null) return;

        var game = new ChessGame(_logger);
        game.StartNewGame();

        Console.Clear();
        Console.WriteLine("=== ШАХИ ===");
        Console.WriteLine();

        while (!game.IsGameOver())
        {
            DisplayChessBoard(game);
            Console.WriteLine($"\nХід гравця: {game.CurrentPlayer}");
            Console.WriteLine("Формат ходу: рядок стовпець -> рядок стовпець (наприклад: 1 0 -> 3 0)");
            Console.WriteLine("Або 'q' для виходу в меню");
            Console.Write("Ваш хід: ");

            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "q")
                return;

            if (TryParseChessMove(input, game.CurrentPlayer, out var move))
            {
                if (game.MakeMove(move))
                {
                    Console.WriteLine("Хід виконано!");
                }
                else
                {
                    Console.WriteLine("Невірний хід! Спробуйте ще раз.");
                }
            }
            else
            {
                Console.WriteLine("Невірний формат ходу!");
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        DisplayChessBoard(game);
        var winner = game.GetWinner();
        if (winner.HasValue)
        {
            Console.WriteLine($"\nПереміг гравець: {winner.Value}!");
        }
        else
        {
            Console.WriteLine("\nНічия!");
        }

        Console.WriteLine("Натисніть будь-яку клавішу для повернення в меню...");
        Console.ReadKey();
    }

    static void PlayCheckers()
    {
        if (_logger == null || _errorHandler == null) return;

        var game = new CheckersGame(_logger);
        game.StartNewGame();

        Console.Clear();
        Console.WriteLine("=== ШАШКИ ===");
        Console.WriteLine();

        while (!game.IsGameOver())
        {
            DisplayCheckersBoard(game);
            Console.WriteLine($"\nХід гравця: {game.CurrentPlayer}");
            Console.WriteLine("Формат ходу: рядок стовпець -> рядок стовпець (наприклад: 2 1 -> 3 0)");
            Console.WriteLine("Або 'q' для виходу в меню");
            Console.Write("Ваш хід: ");

            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "q")
                return;

            if (TryParseMove(input, game.CurrentPlayer, out var move))
            {
                if (game.MakeMove(move))
                {
                    Console.WriteLine("Хід виконано!");
                }
                else
                {
                    Console.WriteLine("Невірний хід! Спробуйте ще раз.");
                }
            }
            else
            {
                Console.WriteLine("Невірний формат ходу!");
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        DisplayCheckersBoard(game);
        var winner = game.GetWinner();
        if (winner.HasValue)
        {
            Console.WriteLine($"\nПереміг гравець: {winner.Value}!");
        }
        else
        {
            Console.WriteLine("\nНічия!");
        }

        Console.WriteLine("Натисніть будь-яку клавішу для повернення в меню...");
        Console.ReadKey();
    }

    static void PlayReversi()
    {
        if (_logger == null || _errorHandler == null) return;

        var game = new ReversiGame(_logger);
        game.StartNewGame();

        Console.Clear();
        Console.WriteLine("=== REVERSI/OTHELLO ===");
        Console.WriteLine();

        while (!game.IsGameOver())
        {
            DisplayReversiBoard(game);
            Console.WriteLine($"\nХід гравця: {game.CurrentPlayer}");
            Console.WriteLine("Формат ходу: рядок стовпець (наприклад: 2 3)");
            Console.WriteLine("Або 'q' для виходу в меню");
            Console.Write("Ваш хід: ");

            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "q")
                return;

            if (TryParseReversiMove(input, game.CurrentPlayer, out var move))
            {
                if (game.MakeMove(move))
                {
                    Console.WriteLine("Хід виконано!");
                }
                else
                {
                    Console.WriteLine("Невірний хід! Спробуйте ще раз.");
                }
            }
            else
            {
                Console.WriteLine("Невірний формат ходу!");
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        DisplayReversiBoard(game);
        var winner = game.GetWinner();
        if (winner.HasValue)
        {
            Console.WriteLine($"\nПереміг гравець: {winner.Value}!");
        }
        else
        {
            Console.WriteLine("\nНічия!");
        }

        Console.WriteLine("Натисніть будь-яку клавішу для повернення в меню...");
        Console.ReadKey();
    }

    static void PlayConnectFour()
    {
        if (_logger == null || _errorHandler == null) return;

        var game = new ConnectFourGame(_logger);
        game.StartNewGame();

        Console.Clear();
        Console.WriteLine("=== CONNECT FOUR ===");
        Console.WriteLine();

        while (!game.IsGameOver())
        {
            DisplayConnectFourBoard(game);
            Console.WriteLine($"\nХід гравця: {game.CurrentPlayer}");
            Console.WriteLine("Введіть номер стовпця (0-6):");
            Console.WriteLine("Або 'q' для виходу в меню");
            Console.Write("Ваш хід: ");

            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "q")
                return;

            if (int.TryParse(input, out int column))
            {
                var move = new ConnectFourMove(column, game.CurrentPlayer);
                if (game.MakeMove(move))
                {
                    Console.WriteLine("Хід виконано!");
                }
                else
                {
                    Console.WriteLine("Невірний хід! Спробуйте ще раз.");
                }
            }
            else
            {
                Console.WriteLine("Невірний формат! Введіть число від 0 до 6.");
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        DisplayConnectFourBoard(game);
        var winner = game.GetWinner();
        if (winner.HasValue)
        {
            Console.WriteLine($"\nПереміг гравець: {winner.Value}!");
        }
        else
        {
            Console.WriteLine("\nНічия!");
        }

        Console.WriteLine("Натисніть будь-яку клавішу для повернення в меню...");
        Console.ReadKey();
    }

    static void DisplayChessBoard(ChessGame game)
    {
        var board = game.Board as ChessBoard;
        if (board == null) return;

        Console.WriteLine("   a b c d e f g h");
        for (int row = 7; row >= 0; row--)
        {
            Console.Write($"{row} ");
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));
                if (piece != null)
                {
                    Console.Write($"{piece.Symbol} ");
                }
                else
                {
                    Console.Write(". ");
                }
            }
            Console.WriteLine($" {row}");
        }
        Console.WriteLine("   a b c d e f g h");
    }

    static void DisplayCheckersBoard(CheckersGame game)
    {
        var board = game.Board as CheckersBoard;
        if (board == null) return;

        Console.WriteLine("   0 1 2 3 4 5 6 7");
        for (int row = 7; row >= 0; row--)
        {
            Console.Write($"{row} ");
            for (int col = 0; col < 8; col++)
            {
                var pos = new Position(row, col);
                if (board.IsDarkSquare(pos))
                {
                    var piece = board.GetPiece(pos);
                    if (piece != null)
                    {
                        Console.Write($"{piece.Symbol} ");
                    }
                    else
                    {
                        Console.Write(". ");
                    }
                }
                else
                {
                    Console.Write("  ");
                }
            }
            Console.WriteLine($" {row}");
        }
        Console.WriteLine("   0 1 2 3 4 5 6 7");
    }

    static void DisplayReversiBoard(ReversiGame game)
    {
        var board = game.Board as ReversiBoard;
        if (board == null) return;

        Console.WriteLine("   0 1 2 3 4 5 6 7");
        for (int row = 0; row < 8; row++)
        {
            Console.Write($"{row} ");
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));
                if (piece != null)
                {
                    Console.Write($"{piece.Symbol} ");
                }
                else
                {
                    Console.Write(". ");
                }
            }
            Console.WriteLine($" {row}");
        }
        Console.WriteLine("   0 1 2 3 4 5 6 7");
    }

    static void DisplayConnectFourBoard(ConnectFourGame game)
    {
        var board = game.Board as ConnectFourBoard;
        if (board == null) return;

        Console.WriteLine("   0 1 2 3 4 5 6");
        for (int row = 5; row >= 0; row--)
        {
            Console.Write($"{row} ");
            for (int col = 0; col < 7; col++)
            {
                var piece = board.GetPiece(new Position(row, col));
                if (piece != null)
                {
                    Console.Write($"{piece.Symbol} ");
                }
                else
                {
                    Console.Write(". ");
                }
            }
            Console.WriteLine($" {row}");
        }
        Console.WriteLine("   0 1 2 3 4 5 6");
    }

    static bool TryParseChessMove(string input, Player player, out ChessMove move)
    {
        move = null!;
        var parts = input.Split(new[] { "->", "->", "-" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        var fromParts = parts[0].Trim().Split(' ');
        var toParts = parts[1].Trim().Split(' ');

        if (fromParts.Length != 2 || toParts.Length != 2) return false;

        if (int.TryParse(fromParts[0], out int fromRow) &&
            int.TryParse(fromParts[1], out int fromCol) &&
            int.TryParse(toParts[0], out int toRow) &&
            int.TryParse(toParts[1], out int toCol))
        {
            move = new ChessMove(new Position(fromRow, fromCol), new Position(toRow, toCol), player);
            return true;
        }

        return false;
    }

    static bool TryParseMove(string input, Player player, out CheckersMove move)
    {
        move = null!;
        var parts = input.Split(new[] { "->", "->", "-" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        var fromParts = parts[0].Trim().Split(' ');
        var toParts = parts[1].Trim().Split(' ');

        if (fromParts.Length != 2 || toParts.Length != 2) return false;

        if (int.TryParse(fromParts[0], out int fromRow) &&
            int.TryParse(fromParts[1], out int fromCol) &&
            int.TryParse(toParts[0], out int toRow) &&
            int.TryParse(toParts[1], out int toCol))
        {
            move = new CheckersMove(new Position(fromRow, fromCol), new Position(toRow, toCol), player);
            return true;
        }

        return false;
    }

    static bool TryParseReversiMove(string input, Player player, out ReversiMove move)
    {
        move = null!;
        var parts = input.Trim().Split(' ');
        if (parts.Length != 2) return false;

        if (int.TryParse(parts[0], out int row) &&
            int.TryParse(parts[1], out int col))
        {
            move = new ReversiMove(new Position(row, col), player);
            return true;
        }

        return false;
    }
}
