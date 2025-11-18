/**
 * @file: BaseBoard.cs
 * @description: Базовий клас для ігрової дошки з загальною логікою
 * @dependencies: IBoard, Position, IPiece
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Базовий клас для ігрової дошки
/// </summary>
public abstract class BaseBoard : IBoard
{
    protected readonly IPiece?[,] _pieces;

    public int Rows { get; }
    public int Columns { get; }

    protected BaseBoard(int rows, int columns)
    {
        if (rows <= 0 || columns <= 0)
            throw new ArgumentException("Розміри дошки повинні бути більше 0");

        Rows = rows;
        Columns = columns;
        _pieces = new IPiece?[rows, columns];
    }

    public virtual IPiece? GetPiece(Position position)
    {
        if (!IsValidPosition(position))
            return null;

        return _pieces[position.Row, position.Column];
    }

    public virtual void SetPiece(Position position, IPiece? piece)
    {
        if (!IsValidPosition(position))
            throw new ArgumentOutOfRangeException(nameof(position), "Позиція поза межами дошки");

        _pieces[position.Row, position.Column] = piece;
    }

    public virtual bool IsValidPosition(Position position)
    {
        return position.IsWithinBounds(Rows, Columns);
    }

    public virtual void Clear()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                _pieces[row, col] = null;
            }
        }
    }

    public abstract IBoard Clone();
}

