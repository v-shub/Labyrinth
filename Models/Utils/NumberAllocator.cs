using System.Collections.Generic;
using System.Linq;

public static class NumberAllocator
{
    public static int NextPortalNumber(Map map)
    {
        int max = 0;
        foreach (var cell in map.Cells)
            foreach (var f in cell.Fills)
                if (f is Portal p && p.Num > max)
                    max = p.Num;
        return max + 1;
    }

    public static int NextKeyNumber(Map map)
    {
        int max = 0;
        foreach (var cell in map.Cells)
            foreach (var f in cell.Fills)
                if (f is Key k && k.PlayerNum > max)
                    max = k.PlayerNum;
        return max + 1;
    }

    // Ограничение «не больше 4 ключей» снято: ключей может быть 1+.
    public static bool CanAddKey(Map map) => true;

    /// <summary>
    /// Пересчитывает номера порталов и ключей, закрывая "дырки" после удаления.
    /// 1,2,4,5 -> 1,2,3,4
    /// </summary>
    public static void ReindexAfterRemoval(Map map)
    {
        ReindexType<Portal>(map, p => p.Num, (p, n) => p.Num = n);
        ReindexType<Key>(map, k => k.PlayerNum, (k, n) => k.PlayerNum = n);
    }

    private static void ReindexType<T>(
        Map map,
        System.Func<T, int> get,
        System.Action<T, int> set) where T : class, ICellFill
    {
        var all = new List<T>();
        foreach (var cell in map.Cells)
            foreach (var f in cell.Fills)
                if (f is T t) all.Add(t);

        // Сортируем по текущему номеру и присваиваем 1..N
        var ordered = all.OrderBy(get).ToList();
        for (int i = 0; i < ordered.Count; i++)
            set(ordered[i], i + 1);
    }
}