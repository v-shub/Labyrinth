using System.Collections.Generic;
using System.Linq;

/// <summary>Хранилище «ключ игрока лежит у дракона» — реализовано как Key в клетке дракона.</summary>
public static class DragonStorage
{
    private static IEnumerable<(int r, int c)> DragonCells(Map map)
    {
        for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Cols; c++)
                if (map[r, c].Fills.OfType<Dragon>().Any())
                    yield return (r, c);
    }

    public static bool HoldsKeyOf(Map map, int playerNum)
        => DragonCells(map).Any(rc =>
            map[rc.r, rc.c].Fills.OfType<Key>().Any(k => k.PlayerNum == playerNum));

    public static void TakeKey(Map map, int row, int col, int playerNum)
    {
        var cell = map[row, col];
        if (cell.Fills.OfType<Key>().Any(k => k.PlayerNum == playerNum)) return;
        cell.Add(new Key(playerNum));
    }

    public static void ReleaseKey(Map map, int row, int col)
    {
        var cell = map[row, col];
        var key = cell.Fills.OfType<Key>().FirstOrDefault();
        if (key != null) cell.Remove(key);
    }
}