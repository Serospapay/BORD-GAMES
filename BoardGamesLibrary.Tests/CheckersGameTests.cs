using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Checkers;
using BoardGamesLibrary.Logging;
using Xunit;

namespace BoardGamesLibrary.Tests;

public class CheckersGameTests
{
    private static ILogger CreateLogger() => new ConsoleLogger(LogLevel.Warning);

    [Fact]
    public void StartNewGame_InitializesCorrectly()
    {
        var game = new CheckersGame(CreateLogger());
        game.StartNewGame();

        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(Player.Player1, game.CurrentPlayer);
        Assert.False(game.IsGameOver());
    }

    [Fact]
    public void ValidDiagonalMove_IsAccepted()
    {
        var game = new CheckersGame(CreateLogger());
        game.StartNewGame();

        var move = new CheckersMove(new Position(2, 1), new Position(3, 0), Player.Player1);
        Assert.True(game.IsValidMove(move));
        Assert.True(game.MakeMove(move));

        Assert.Equal(Player.Player2, game.CurrentPlayer);
    }

    [Fact]
    public void GetValidMoves_ReturnsMovesForCurrentPlayer()
    {
        var game = new CheckersGame(CreateLogger());
        game.StartNewGame();

        var moves = game.GetValidMoves(Player.Player1).ToList();
        Assert.NotEmpty(moves);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var game = new CheckersGame(CreateLogger());
        game.StartNewGame();
        game.MakeMove(new CheckersMove(new Position(2, 1), new Position(3, 0), Player.Player1));

        var clone = game.Clone() as CheckersGame;
        Assert.NotNull(clone);
        Assert.Equal(game.CurrentPlayer, clone!.CurrentPlayer);
    }
}
