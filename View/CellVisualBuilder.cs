using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Пара "логическое наполнение + его спрайт" — то, что реально хранит Cell.</summary>
public readonly struct FillWithSprite
{
    public readonly ICellFill Fill;
    public readonly Sprite Sprite;

    public FillWithSprite(ICellFill fill, Sprite sprite)
    {
        Fill = fill;
        Sprite = sprite;
    }
}

public enum LayerMode
{
    Full,
    CornerTopRight,
    CornerTopLeft,
    CornerBottomRight,
    CornerBottomLeft
}

public readonly struct CellLayer
{
    public readonly ICellFill Fill;
    public readonly Sprite Sprite;
    public readonly LayerMode Mode;
    public readonly int Number; // 0 = без номера
    public readonly string Label;

    public CellLayer(ICellFill fill, Sprite sprite, LayerMode mode, int number = 0, string label = null)
    {
        Fill = fill; Sprite = sprite; Mode = mode; Number = number; Label = label;
    }
}

public static class CellVisualBuilder
{
    // 1) "Всегда на всю клетку" — фиксированный порядок снизу вверх.
    private static readonly Type[] FullAlwaysOrder =
    {
        typeof(Swamp),
        typeof(River),
        typeof(RubberRoom),
        typeof(Portal),
        typeof(Dragon)
    };

    // 2) "Предметы" — фиксированный порядок приоритета (первый — на всю клетку).
    private static readonly Type[] ItemOrder =
    {
        typeof(Exit),
        typeof(Sword),
        typeof(Key)
    };

    // Для быстрой проверки "есть ли блокер" без LINQ.
    private static readonly HashSet<Type> FullAlwaysSet = new(FullAlwaysOrder);

    private static readonly LayerMode[] CornerCycle =
    {
        LayerMode.CornerTopRight,
        LayerMode.CornerTopLeft,
        LayerMode.CornerBottomRight,
        LayerMode.CornerBottomLeft
    };
    private static int NumberOf(ICellFill fill) => fill switch
    {
        Portal p => p.Num,
        Key k => k.PlayerNum,
        _ => 0
    };

    private static string LabelOf(ICellFill fill) => fill switch
    {
        River r => r.End switch
        {
            RiverEnd.Source => "и",
            RiverEnd.Mouth => "у",
            _ => null
        },
        _ => null
    };
    public static List<CellLayer> Build(IReadOnlyList<FillWithSprite> fills)
    {
        var result = new List<CellLayer>(4);
        if (fills == null || fills.Count == 0) return result;

        var byType = new Dictionary<Type, FillWithSprite>(fills.Count);
        for (int i = 0; i < fills.Count; i++)
        {
            var f = fills[i];
            if (f.Fill == null) continue;
            var t = f.Fill.FillType;
            if (t == null) continue;
            if (!byType.ContainsKey(t)) byType[t] = f;
        }

        // --- Группа 1: всегда full, фиксированный порядок снизу вверх ---
        bool hasBlocker = false; // RubberRoom / Portal / Dragon — только они мешают предметам
        foreach (var t in FullAlwaysOrder)
        {
            if (byType.TryGetValue(t, out var f))
            {
                result.Add(new CellLayer(f.Fill, f.Sprite, LayerMode.Full, NumberOf(f.Fill), LabelOf(f.Fill)));
                if (IsBlocker(t)) hasBlocker = true;
            }
        }

        // --- Группа 2: Exit / Sword / Key ---
        var items = new List<FillWithSprite>(3);
        foreach (var t in ItemOrder)
            if (byType.TryGetValue(t, out var f))
                items.Add(f);

        if (items.Count == 0) return result;

        if (hasBlocker)
        {
            for (int i = 0; i < items.Count; i++)
                result.Add(new CellLayer(items[i].Fill, items[i].Sprite,
                    CornerCycle[i % CornerCycle.Length], NumberOf(items[i].Fill), LabelOf(items[i].Fill)));
        }
        else
        {
            result.Add(new CellLayer(items[0].Fill, items[0].Sprite, LayerMode.Full,
                NumberOf(items[0].Fill), LabelOf(items[0].Fill)));
            for (int i = 1; i < items.Count; i++)
                result.Add(new CellLayer(items[i].Fill, items[i].Sprite,
                    CornerCycle[i % CornerCycle.Length], NumberOf(items[i].Fill), LabelOf(items[i].Fill)));
        }

        return result;
    }

    private static bool IsBlocker(Type t)
        => t == typeof(RubberRoom) || t == typeof(Portal) || t == typeof(Dragon);
}