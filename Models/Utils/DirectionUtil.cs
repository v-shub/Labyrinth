using System;

public enum Direction { Left, Right, Up, Down }

public static class DirectionUtil
{
    public static Direction FromDelta(int dr, int dc) => (dr, dc) switch
    {
        (-1, 0) => Direction.Up,
        (1, 0) => Direction.Down,
        (0, -1) => Direction.Left,
        (0, 1) => Direction.Right,
        _ => throw new Exception("не соседи")
    };

    public static Direction ToNeighbor(int r1, int c1, int r2, int c2)
        => FromDelta(r2 - r1, c2 - c1);

    public static (int dr, int dc) Offset(Direction dir) => dir switch
    {
        Direction.Up => (-1, 0),
        Direction.Down => (1, 0),
        Direction.Left => (0, -1),
        Direction.Right => (0, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(dir))
    };

    public static Direction Opposite(Direction dir) => dir switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(dir))
    };

    public static string ToRussian(Direction dir) => dir switch
    {
        Direction.Up => "вверх",
        Direction.Down => "вниз",
        Direction.Left => "налево",
        Direction.Right => "направо",
        _ => throw new ArgumentOutOfRangeException(nameof(dir))
    };
}