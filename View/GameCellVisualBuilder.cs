using System;
using System.Collections.Generic;

/// <summary>
/// Визуал клетки для сцены игрового процесса.
/// Отличия от CellVisualBuilder:
///  - номера порталов/ключей не рисуются;
///  - направление реки не рисуется;
///  - выход/резинка/болото скрыты по правилам осведомлённости;
///  - резинка может быть показана эфемерно (revealRubberOnExit).
/// </summary>
public static class GameCellVisualBuilder
{
    private static readonly Type[] FullAlwaysOrder =
    {
        typeof(Swamp),
        typeof(River),
        typeof(RubberRoom),
        typeof(Portal),
        typeof(Dragon)
    };

    private static readonly Type[] ItemOrder = { typeof(Exit), typeof(Sword), typeof(Key) };

    private static readonly LayerMode[] CornerCycle =
    {
        LayerMode.CornerTopRight,
        LayerMode.CornerTopLeft,
        LayerMode.CornerBottomRight,
        LayerMode.CornerBottomLeft
    };

    public static List<CellLayer> Build(
        IReadOnlyList<FillWithSprite> fills,
        GameContext ctx,
        bool revealRubberOnExit)
    {
        var result = new List<CellLayer>(4);
        if (fills == null || fills.Count == 0) return result;

        var byType = new Dictionary<Type, FillWithSprite>(fills.Count);
        foreach (var f in fills)
        {
            if (f.Fill?.FillType == null) continue;
            if (!byType.ContainsKey(f.Fill.FillType)) byType[f.Fill.FillType] = f;
        }

        // Болото-режим: из клетки видно только болото.
        if (ctx.Player.SkipNextTurn)
        {
            if (byType.TryGetValue(typeof(Swamp), out var sw))
                result.Add(new CellLayer(sw.Fill, sw.Sprite, LayerMode.Full, 0, null));
            return result;
        }

        bool hasBlocker = false;

        foreach (var t in FullAlwaysOrder)
        {
            if (!byType.TryGetValue(t, out var f)) continue;

            bool visible;
            if (t == typeof(RubberRoom))
                visible = revealRubberOnExit;
            else
                visible = CellVisibility.IsVisibleToPlayer(f.Fill, ctx);

            if (!visible) continue;

            result.Add(new CellLayer(f.Fill, f.Sprite, LayerMode.Full, 0, null));

            if (t == typeof(RubberRoom) || t == typeof(Portal) || t == typeof(Dragon))
                hasBlocker = true;
        }

        var items = new List<FillWithSprite>(3);
        foreach (var t in ItemOrder)
        {
            if (!byType.TryGetValue(t, out var f)) continue;
            if (!CellVisibility.IsVisibleToPlayer(f.Fill, ctx)) continue;
            items.Add(f);
        }
        if (items.Count == 0) return result;

        if (hasBlocker)
        {
            for (int i = 0; i < items.Count; i++)
                result.Add(new CellLayer(items[i].Fill, items[i].Sprite,
                    CornerCycle[i % CornerCycle.Length], 0, null));
        }
        else
        {
            result.Add(new CellLayer(items[0].Fill, items[0].Sprite, LayerMode.Full, 0, null));
            for (int i = 1; i < items.Count; i++)
                result.Add(new CellLayer(items[i].Fill, items[i].Sprite,
                    CornerCycle[i % CornerCycle.Length], 0, null));
        }

        return result;
    }
}