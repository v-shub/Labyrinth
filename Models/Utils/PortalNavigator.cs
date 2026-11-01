using System.Collections.Generic;
using System.Linq;

/// <summary>Вычисление «следующего портала по кругу» над картой.</summary>
public static class PortalNavigator
{
    public static IEnumerable<(int row, int col, int num)> AllPortals(Map map)
    {
        for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Cols; c++)
                foreach (var p in map[r, c].Fills.OfType<Portal>())
                    yield return (r, c, p.Num);
    }
    
    /// <summary>Следующий по кругу портал (одна из его клеток). null — если текущий номер не найден.</summary>
    public static (int row, int col, int num)? Next(Map map, int currentNum)
    {
        var portals = AllPortals(map).ToList();
        if (portals.Count == 0) return null;

        var nums = portals.Select(p => p.num).Distinct().OrderBy(n => n).ToList();
        int idx = nums.IndexOf(currentNum);
        if (idx < 0) return null;

        int nextNum = nums[(idx + 1) % nums.Count];
        var cell = portals.First(p => p.num == nextNum);
        return (cell.row, cell.col, nextNum);
    }
}