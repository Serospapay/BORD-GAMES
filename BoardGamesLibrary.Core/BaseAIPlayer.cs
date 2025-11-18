/**
 * @file: BaseAIPlayer.cs
 * @description: Базовий клас для AI гравців з загальною логікою
 * @dependencies: IAIPlayer, IGame, IMove, Player
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Базовий клас для AI гравців
/// </summary>
public abstract class BaseAIPlayer : IAIPlayer
{
    public AIDifficulty Difficulty { get; }
    public Player Player { get; }

    protected BaseAIPlayer(Player player, AIDifficulty difficulty)
    {
        Player = player;
        Difficulty = difficulty;
    }

    public abstract IMove? ChooseMove(IGame game);

    /// <summary>
    /// Отримує випадковий валідний хід (для легкого рівня)
    /// </summary>
    protected IMove? GetRandomMove(IGame game)
    {
        var validMoves = game.GetValidMoves(Player).ToList();
        if (!validMoves.Any())
            return null;

        var random = new Random();
        return validMoves[random.Next(validMoves.Count)];
    }

    /// <summary>
    /// Отримує найкращий хід за евристикою (для середнього рівня)
    /// </summary>
    protected IMove? GetBestMoveByHeuristic(IGame game, Func<IGame, Player, int> heuristic)
    {
        var validMoves = game.GetValidMoves(Player).ToList();
        if (!validMoves.Any())
            return null;

        IMove? bestMove = null;
        int bestScore = int.MinValue;

        foreach (var move in validMoves)
        {
            // Симулюємо хід
            var gameCopy = CloneGame(game);
            if (gameCopy.MakeMove(move))
            {
                var score = heuristic(gameCopy, Player);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
        }

        return bestMove ?? GetRandomMove(game);
    }

    /// <summary>
    /// Клонує гру для симуляції ходів
    /// </summary>
    protected abstract IGame CloneGame(IGame game);
}

