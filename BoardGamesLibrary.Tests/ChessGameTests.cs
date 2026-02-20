using BoardGamesLibrary.Core;
using BoardGamesLibrary.Games.Chess;
using BoardGamesLibrary.Logging;
using Xunit;

namespace BoardGamesLibrary.Tests;

public class ChessGameTests
{
    private static ILogger CreateLogger() => new ConsoleLogger(LogLevel.Warning);

    [Fact]
    public void StartNewGame_InitializesCorrectly()
    {
        var game = new ChessGame(CreateLogger());
        game.StartNewGame();

        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(Player.Player1, game.CurrentPlayer);
        Assert.False(game.IsGameOver());
        Assert.Null(game.GetWinner());
    }

    [Fact]
    public void ValidPawnMove_IsAccepted()
    {
        var game = new ChessGame(CreateLogger());
        game.StartNewGame();

        var move = new ChessMove(new Position(1, 0), new Position(3, 0), Player.Player1);
        Assert.True(game.IsValidMove(move));
        Assert.True(game.MakeMove(move));

        Assert.Equal(Player.Player2, game.CurrentPlayer);
    }

    [Fact]
    public void InvalidMove_IsRejected()
    {
        var game = new ChessGame(CreateLogger());
        game.StartNewGame();

        var move = new ChessMove(new Position(1, 0), new Position(4, 0), Player.Player1);
        Assert.False(game.IsValidMove(move));
        Assert.False(game.MakeMove(move));
    }

    [Fact]
    public void MoveLeavingKingInCheck_IsRejected()
    {
        var game = new ChessGame(CreateLogger());
        game.StartNewGame();

        game.MakeMove(new ChessMove(new Position(1, 4), new Position(3, 4), Player.Player1));
        game.MakeMove(new ChessMove(new Position(6, 3), new Position(4, 3), Player.Player2));
        game.MakeMove(new ChessMove(new Position(1, 5), new Position(3, 5), Player.Player1));
        game.MakeMove(new ChessMove(new Position(7, 4), new Position(6, 4), Player.Player2));

        var queenMove = new ChessMove(new Position(7, 3), new Position(3, 7), Player.Player2);
        Assert.False(game.MakeMove(queenMove));
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var game = new ChessGame(CreateLogger());
        game.StartNewGame();
        game.MakeMove(new ChessMove(new Position(1, 0), new Position(3, 0), Player.Player1));

        var clone = game.Clone() as ChessGame;
        Assert.NotNull(clone);
        Assert.Equal(Player.Player2, game.CurrentPlayer);
        Assert.Equal(game.CurrentPlayer, clone!.CurrentPlayer);
        Assert.NotSame(game.Board, clone.Board);

        clone.MakeMove(new ChessMove(new Position(6, 0), new Position(4, 0), Player.Player2));
        Assert.Equal(Player.Player2, game.CurrentPlayer);
        Assert.Equal(Player.Player1, clone.CurrentPlayer);
    }

    [Fact]
    public void CastlingWithoutRook_IsRejected()
    {
        var game = new ChessGame(CreateLogger());
        game.StartNewGame();
        var board = (ChessBoard)game.Board;

        board.SetPiece(new Position(0, 7), null);
        board.SetPiece(new Position(0, 6), null);
        board.SetPiece(new Position(0, 5), null);

        var castling = new ChessMove(new Position(0, 4), new Position(0, 6), Player.Player1) { IsCastling = true };
        Assert.False(game.IsValidMove(castling));
        Assert.False(game.MakeMove(castling));
    }

    [Fact]
    public void IsValidMove_RejectsMoveThatExposesOwnKing()
    {
        var game = new ChessGame(CreateLogger());
        game.StartNewGame();
        var board = (ChessBoard)game.Board;
        board.Clear();

        board.SetPiece(new Position(0, 4), new ChessPiece(new Position(0, 4), Player.Player1, ChessPieceType.King));
        board.SetPiece(new Position(1, 4), new ChessPiece(new Position(1, 4), Player.Player1, ChessPieceType.Rook));
        board.SetPiece(new Position(7, 4), new ChessPiece(new Position(7, 4), Player.Player2, ChessPieceType.Rook));
        board.SetPiece(new Position(7, 0), new ChessPiece(new Position(7, 0), Player.Player2, ChessPieceType.King));

        var illegalMove = new ChessMove(new Position(1, 4), new Position(1, 3), Player.Player1);
        Assert.False(game.IsValidMove(illegalMove));
        Assert.False(game.MakeMove(illegalMove));
    }
}
