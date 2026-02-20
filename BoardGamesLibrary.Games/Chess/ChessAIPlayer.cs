/**
 * @file: ChessAIPlayer.cs
 * @description: AI гравець для шахів з різними рівнями складності
 * @dependencies: BaseAIPlayer, ChessGame, ChessMove
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.Chess;

/// <summary>
/// AI гравець для шахів
/// </summary>
public class ChessAIPlayer : BaseAIPlayer
{
    private readonly ILogger? _logger;

    public ChessAIPlayer(Player player, AIDifficulty difficulty, ILogger? logger = null) 
        : base(player, difficulty)
    {
        _logger = logger;
    }

    public override IMove? ChooseMove(IGame game)
    {
        if (game is not ChessGame chessGame)
            return null;

        return Difficulty switch
        {
            AIDifficulty.Easy => GetRandomMove(chessGame),
            AIDifficulty.Medium => GetBestMoveByHeuristic(chessGame, EvaluatePosition),
            AIDifficulty.Hard => GetBestMoveMinimax(chessGame, 3),
            _ => GetRandomMove(chessGame)
        };
    }

    protected override IGame CloneGame(IGame game)
    {
        if (game is not ChessGame chessGame)
            return game;

        if (chessGame.State != GameState.InProgress)
        {
            _logger?.LogWarning($"Не можна клонувати гру, яка не в стані InProgress (State: {chessGame.State})");
            return game;
        }

        return chessGame.Clone();
    }

    private IMove? GetBestMoveMinimax(ChessGame game, int depth)
    {
        var validMoves = game.GetValidMoves(Player).OfType<ChessMove>().ToList();
        if (!validMoves.Any())
            return null;

        IMove? bestMove = null;
        int bestScore = int.MinValue;

        foreach (var move in validMoves)
        {
            try
            {
                var gameCopy = CloneGame(game) as ChessGame;
                if (gameCopy == null) continue;
                
                // Критичні перевірки перед ходом
                if (gameCopy.State != GameState.InProgress)
                {
                    _logger?.LogWarning($"Клонована гра не в стані InProgress (State: {gameCopy.State}), пропускаємо хід {move}");
                    continue;
                }
                
                if (gameCopy.CurrentPlayer != Player)
                {
                    _logger?.LogWarning($"Клонована гра має іншого гравця (Expected: {Player}, Actual: {gameCopy.CurrentPlayer}), пропускаємо хід {move}");
                    continue;
                }
                
                if (gameCopy.IsGameOver())
                {
                    _logger?.LogWarning($"Клонована гра вже закінчена, пропускаємо хід {move}");
                    continue;
                }

                if (gameCopy.MakeMove(move))
                {
                    // Перевіряємо стан після ходу
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

    private int Minimax(ChessGame game, int depth, bool maximizingPlayer, int alpha, int beta)
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
                    var gameCopy = CloneGame(game) as ChessGame;
                    if (gameCopy == null) continue;
                    
                    if (gameCopy.State != GameState.InProgress || gameCopy.IsGameOver()) continue;
                    if (gameCopy.CurrentPlayer != currentPlayer) continue;

                    if (gameCopy.MakeMove(move))
                    {
                        // Перевіряємо стан після ходу - якщо гра закінчилася, не продовжуємо рекурсію
                        if (gameCopy.IsGameOver() || gameCopy.State != GameState.InProgress)
                        {
                            // Гра закінчилася після цього ходу - оцінюємо позицію
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
                            break; // Alpha-beta pruning
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
                    var gameCopy = CloneGame(game) as ChessGame;
                    if (gameCopy == null) continue;
                    
                    if (gameCopy.State != GameState.InProgress || gameCopy.IsGameOver()) continue;
                    if (gameCopy.CurrentPlayer != currentPlayer) continue;

                    if (gameCopy.MakeMove(move))
                    {
                        // Перевіряємо стан після ходу
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
                            break; // Alpha-beta pruning
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
        if (game is not ChessGame chessGame)
            return 0;

        var board = chessGame.Board as ChessBoard;
        if (board == null) return 0;

        int score = 0;
        var opponent = player.GetOpponent();

        // Значення фігур
        var pieceValues = new Dictionary<ChessPieceType, int>
        {
            { ChessPieceType.Pawn, 1 },
            { ChessPieceType.Knight, 3 },
            { ChessPieceType.Bishop, 3 },
            { ChessPieceType.Rook, 5 },
            { ChessPieceType.Queen, 9 },
            { ChessPieceType.King, 100 }
        };

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));
                if (piece != null)
                {
                    int value = pieceValues.GetValueOrDefault(piece.Type, 0);
                    if (piece.Owner == player)
                        score += value;
                    else if (piece.Owner == opponent)
                        score -= value;
                }
            }
        }

        // Перевірка на перемогу
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

