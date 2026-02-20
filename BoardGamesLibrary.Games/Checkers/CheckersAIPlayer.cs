/**
 * @file: CheckersAIPlayer.cs
 * @description: AI гравець для шашок з різними рівнями складності
 * @dependencies: BaseAIPlayer, CheckersGame, CheckersMove
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Checkers;

/// <summary>
/// AI гравець для шашок
/// </summary>
public class CheckersAIPlayer : BaseAIPlayer
{
    private readonly ILogger? _logger;

    public CheckersAIPlayer(Player player, AIDifficulty difficulty, ILogger? logger = null) 
        : base(player, difficulty)
    {
        _logger = logger;
    }

    public override IMove? ChooseMove(IGame game)
    {
        if (game is not CheckersGame checkersGame)
            return null;

        return Difficulty switch
        {
            AIDifficulty.Easy => GetRandomMove(checkersGame),
            AIDifficulty.Medium => GetBestMoveByHeuristic(checkersGame, EvaluatePosition),
            AIDifficulty.Hard => GetBestMoveMinimax(checkersGame, 4),
            _ => GetRandomMove(checkersGame)
        };
    }

    protected override IGame CloneGame(IGame game)
    {
        if (game is not CheckersGame checkersGame)
            return game;

        if (checkersGame.State != GameState.InProgress)
        {
            _logger?.LogWarning($"Не можна клонувати гру, яка не в стані InProgress (State: {checkersGame.State})");
            return game;
        }

        return checkersGame.Clone();
    }

    private IMove? GetBestMoveMinimax(CheckersGame game, int depth)
    {
        var validMoves = game.GetValidMoves(Player).OfType<CheckersMove>().ToList();
        if (!validMoves.Any())
            return null;

        IMove? bestMove = null;
        int bestScore = int.MinValue;

        foreach (var move in validMoves)
        {
            try
            {
                var gameCopy = CloneGame(game) as CheckersGame;
                if (gameCopy == null) continue;
                
                if (gameCopy.State != GameState.InProgress || gameCopy.IsGameOver()) continue;
                if (gameCopy.CurrentPlayer != Player) continue;

                if (gameCopy.MakeMove(move))
                {
                    if (gameCopy.IsGameOver() || gameCopy.State != GameState.InProgress)
                    {
                        var score = EvaluatePosition(gameCopy, Player);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMove = move;
                        }
                    }
                    else
                    {
                        var score = Minimax(gameCopy, depth - 1, false, int.MinValue, int.MaxValue);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMove = move;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Помилка при оцінці ходу {move}", ex);
                continue;
            }
        }

        return bestMove ?? GetRandomMove(game);
    }

    private int Minimax(CheckersGame game, int depth, bool maximizingPlayer, int alpha, int beta)
    {
        if (depth == 0 || game.IsGameOver())
        {
            return EvaluatePosition(game, Player);
        }

        var currentPlayer = maximizingPlayer ? Player : Player.GetOpponent();
        var validMoves = game.GetValidMoves(currentPlayer).ToList();

        if (!validMoves.Any())
        {
            return EvaluatePosition(game, Player);
        }

        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in validMoves)
            {
                try
                {
                    var gameCopy = CloneGame(game) as CheckersGame;
                    if (gameCopy == null) continue;
                    
                    if (gameCopy.State != GameState.InProgress || gameCopy.IsGameOver()) continue;
                    if (gameCopy.CurrentPlayer != currentPlayer) continue;

                    if (gameCopy.MakeMove(move))
                    {
                        if (gameCopy.IsGameOver() || gameCopy.State != GameState.InProgress)
                        {
                            int eval = EvaluatePosition(gameCopy, Player);
                            maxEval = Math.Max(maxEval, eval);
                            alpha = Math.Max(alpha, eval);
                        }
                        else
                        {
                            int eval = Minimax(gameCopy, depth - 1, false, alpha, beta);
                            maxEval = Math.Max(maxEval, eval);
                            alpha = Math.Max(alpha, eval);
                        }
                        if (beta <= alpha)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Помилка в Minimax (maximizing) для ходу {move}", ex);
                    continue;
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in validMoves)
            {
                try
                {
                    var gameCopy = CloneGame(game) as CheckersGame;
                    if (gameCopy == null) continue;
                    
                    if (gameCopy.State != GameState.InProgress || gameCopy.IsGameOver()) continue;
                    if (gameCopy.CurrentPlayer != currentPlayer) continue;

                    if (gameCopy.MakeMove(move))
                    {
                        if (gameCopy.IsGameOver() || gameCopy.State != GameState.InProgress)
                        {
                            int eval = EvaluatePosition(gameCopy, Player);
                            minEval = Math.Min(minEval, eval);
                            beta = Math.Min(beta, eval);
                        }
                        else
                        {
                            int eval = Minimax(gameCopy, depth - 1, true, alpha, beta);
                            minEval = Math.Min(minEval, eval);
                            beta = Math.Min(beta, eval);
                        }
                        if (beta <= alpha)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Помилка в Minimax (minimizing) для ходу {move}", ex);
                    continue;
                }
            }
            return minEval;
        }
    }

    private int EvaluatePosition(IGame game, Player player)
    {
        if (game is not CheckersGame checkersGame)
            return 0;

        var board = checkersGame.Board as CheckersBoard;
        if (board == null) return 0;

        int score = 0;
        var opponent = player.GetOpponent();

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));
                if (piece != null)
                {
                    int value = piece.IsKing ? 5 : 1;
                    if (piece.Owner == player)
                        score += value;
                    else if (piece.Owner == opponent)
                        score -= value;
                }
            }
        }

        if (game.IsGameOver())
        {
            var winner = game.GetWinner();
            if (winner == player)
                score += 1000;
            else if (winner == opponent)
                score -= 1000;
        }

        return score;
    }
}

