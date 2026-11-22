using System;
using System.Collections.Generic;

/// <summary>
/// Подсчёт наполнений на карте.
/// Для River считается количество рек, а не речных ячеек, т.е.учитываются только истоки (RiverEnd.Source).
/// </summary>
public static class MapFillCounter
{
    /// <summary>
    /// Возвращает словарь: имя типа из FillTypeRegistry -> количество.
    /// </summary>
    public static Dictionary<string, int> Count(Map map)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));

        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var cell in map.Cells)
        {
            foreach (var fill in cell.Fills)
            {
                // River считаем только с End == Source ("End": 1 в JSON).
                if (fill is River river && river.End != RiverEnd.Source)
                    continue;

                string name = FillTypeRegistry.GetName(fill.FillType);
                result.TryGetValue(name, out int c);
                result[name] = c + 1;
            }
        }

        return result;
    }
}