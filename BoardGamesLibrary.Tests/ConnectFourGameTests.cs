using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.ConnectFour;
using BoardGamesLibrary.Logging;
using Xunit;

namespace BoardGamesLibrary.Tests;

public class ConnectFourGameTests
{
    private static ILogger CreateLogger() => new ConsoleLogger(LogLevel.Warning);

    [Fact]
    public void StartNewGame_InitializesCorrectly()
    {
        var game = new ConnectFourGame(CreateLogger());
        game.StartNewGame();

        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(Player.Player1, game.CurrentPlayer);
        Assert.False(game.IsGameOver());
    }

    [Fact]
    public void ValidMove_IsAccepted()
    {
        var game = new ConnectFourGame(CreateLogger());
        game.StartNewGame();

        var move = new ConnectFourMove(3, Player.Player1);
        Assert.True(game.IsValidMove(move));
        Assert.True(game.MakeMove(move));

        Assert.Equal(Player.Player2, game.CurrentPlayer);
    }

    [Fact]
    public void NullMove_IsRejected()
    {
        var game = new ConnectFourGame(CreateLogger());
        game.StartNewGame();

        Assert.False(game.MakeMove(null!));
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var game = new ConnectFourGame(CreateLogger());
        game.StartNewGame();
        game.MakeMove(new ConnectFourMove(0, Player.Player1));

        var clone = game.Clone() as ConnectFourGame;
        Assert.NotNull(clone);
        Assert.Equal(game.CurrentPlayer, clone!.CurrentPlayer);
    }
}
