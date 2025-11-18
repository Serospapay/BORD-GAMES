/**
 * @file: ConnectFourAIPlayer.cs
 * @description: AI гравець для Connect Four з різними рівнями складності
 * @dependencies: BaseAIPlayer, ConnectFourGame, ConnectFourMove
 * @created: 2024-12-19
 */

using BoardGamesLibrary.Core;
using BoardGamesLibrary.Logging;

namespace BoardGamesLibrary.Games.ConnectFour;

/// <summary>
/// AI гравець для Connect Four
/// </summary>
public class ConnectFourAIPlayer : BaseAIPlayer
{
    private readonly ILogger? _logger;

    public ConnectFourAIPlayer(Player player, AIDifficulty difficulty, ILogger? logger = null) 
        : base(player, difficulty)
    {
        _logger = logger;
    }

    public override IMove? ChooseMove(IGame game)
    {
        if (game is not ConnectFourGame connectFourGame)
            return null;

        return Difficulty switch
        {
            AIDifficulty.Easy => GetRandomMove(connectFourGame),
            AIDifficulty.Medium => GetBestMoveByHeuristic(connectFourGame, EvaluatePosition),
            AIDifficulty.Hard => GetBestMoveMinimax(connectFourGame, 6),
            _ => GetRandomMove(connectFourGame)
        };
    }

    protected override IGame CloneGame(IGame game)
    {
        if (game is not ConnectFourGame connectFourGame)
            return game;

        // Перевіряємо, чи оригінальна гра в правильному стані для клонування
        if (connectFourGame.State != GameState.InProgress)
        {
            _logger?.LogWarning($"Не можна клонувати гру, яка не в стані InProgress (State: {connectFourGame.State})");
            return game;
        }

        try
        {
            var newGame = new ConnectFourGame(_logger ?? new ConsoleLogger(LogLevel.Info));
            var clonedBoard = connectFourGame.Board.Clone() as ConnectFourBoard;
            if (clonedBoard == null)
            {
                _logger?.LogError("Не вдалося клонувати дошку для Connect Four");
                return game;
            }

            var boardField = typeof(ConnectFourGame).GetField("Board", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (boardField == null)
            {
                _logger?.LogError("Не вдалося знайти поле Board через рефлексію");
                return game;
            }
            boardField.SetValue(newGame, clonedBoard);

            var stateField = typeof(ConnectFourGame).GetField("State", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var currentPlayerField = typeof(ConnectFourGame).GetField("CurrentPlayer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (stateField != null && currentPlayerField != null)
            {
                // Завжди встановлюємо InProgress для клонованої гри
                stateField.SetValue(newGame, GameState.InProgress);
                currentPlayerField.SetValue(newGame, connectFourGame.CurrentPlayer);
                
                if (newGame.State != GameState.InProgress)
                {
                    _logger?.LogWarning($"Клонована гра не в стані InProgress");
                    return game;
                }
                
                if (newGame.CurrentPlayer != connectFourGame.CurrentPlayer)
                {
                    _logger?.LogWarning($"Клонована гра має іншого гравця (Expected: {connectFourGame.CurrentPlayer}, Actual: {newGame.CurrentPlayer})");
                    return game;
                }
            }
            else
            {
                _logger?.LogWarning("Не вдалося відновити стан гри через рефлексію");
                return game;
            }

            return newGame;
        }
        catch (Exception ex)
        {
            _logger?.LogError("Помилка при клонуванні гри Connect Four", ex);
            return game;
        }
    }

    private IMove? GetBestMoveMinimax(ConnectFourGame game, int depth)
    {
        var validMoves = game.GetValidMoves(Player).OfType<ConnectFourMove>().ToList();
        if (!validMoves.Any())
            return null;

        IMove? bestMove = null;
        int bestScore = int.MinValue;

        foreach (var move in validMoves)
        {
            try
            {
                var gameCopy = CloneGame(game) as ConnectFourGame;
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

    private int Minimax(ConnectFourGame game, int depth, bool maximizingPlayer, int alpha, int beta)
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
                    var gameCopy = CloneGame(game) as ConnectFourGame;
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
                    var gameCopy = CloneGame(game) as ConnectFourGame;
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
        if (game is not ConnectFourGame connectFourGame)
            return 0;

        var board = connectFourGame.Board as ConnectFourBoard;
        if (board == null) return 0;

        var opponent = player.GetOpponent();

        // Перевірка на перемогу
        if (game.IsGameOver())
        {
            var winner = game.GetWinner();
            if (winner == player)
                return 10000;
            else if (winner == opponent)
                return -10000;
        }

        int score = 0;

        // Оцінка позиції за кількістю можливих виграшних комбінацій
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var pos = new Position(row, col);
                var piece = board.GetPiece(pos);
                
                if (piece != null)
                {
                    // Перевірка можливих комбінацій
                    score += CountPotentialWins(board, pos, piece.Owner == player ? 1 : -1);
                }
            }
        }

        return score;
    }

    private int CountPotentialWins(ConnectFourBoard board, Position pos, int multiplier)
    {
        int count = 0;
        var piece = board.GetPiece(pos);
        if (piece == null) return 0;

        (int rowDir, int colDir)[] directions = { (0, 1), (1, 0), (1, 1), (1, -1) };

        foreach (var (rowDir, colDir) in directions)
        {
            int inRow = 1;
            for (int i = 1; i < 4; i++)
            {
                var newPos = new Position(pos.Row + rowDir * i, pos.Column + colDir * i);
                if (!board.IsValidPosition(newPos))
                    break;

                var newPiece = board.GetPiece(newPos);
                if (newPiece != null && newPiece.Owner == piece.Owner)
                    inRow++;
                else
                    break;
            }

            if (inRow >= 4)
                count += 100;
            else if (inRow == 3)
                count += 10;
            else if (inRow == 2)
                count += 1;
        }

        return count * multiplier;
    }
}

