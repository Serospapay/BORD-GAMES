/**
 * @file: Position.cs
 * @description: Структура для представлення позиції на дошці
 * @dependencies: None
 * @created: 2024-12-19
 */

namespace BoardGamesLibrary.Core;

/// <summary>
/// Структура для представлення позиції на дошці
/// </summary>
public readonly struct Position : IEquatable<Position>
{
    public int Row { get; }
    public int Column { get; }

    public Position(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public bool Equals(Position other)
    {
        return Row == other.Row && Column == other.Column;
    }

    public override bool Equals(object? obj)
    {
        return obj is Position other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Column);
    }

    public static bool operator ==(Position left, Position right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Position left, Position right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"({Row}, {Column})";
    }

    /// <summary>
    /// Перевіряє, чи позиція знаходиться в межах дошки
    /// </summary>
    public bool IsWithinBounds(int maxRow, int maxColumn)
    {
        return Row >= 0 && Row < maxRow && Column >= 0 && Column < maxColumn;
    }
}

