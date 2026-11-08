using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Визуал текущей клетки игрока. Без drag&drop и без номеров/подписей.
/// </summary>
public class GameCell : MonoBehaviour
{
    [SerializeField] private RectTransform layersRoot;
    [SerializeField] private FillPalette palette;

    [Header("Corner layout")]
    [SerializeField] private float cornerScale = 0.45f;
    [SerializeField] private float cornerPadding = 2f;

    public void Render(CellContainer cell, GameContext ctx, int row, int col, bool revealRubberOnExit)
    {
        ClearVisual();
        if (cell == null) return;

        // Логические fills + подобранные в этом ходу (для отрисовки «момента подбора»).
        var allFills = new List<ICellFill>(cell.Fills.Count + ctx.PickedUpThisTurn.Count);
        allFills.AddRange(cell.Fills);
        foreach (var (r, c, fill) in ctx.PickedUpThisTurn)
            if (r == row && c == col)
                allFills.Add(fill);

        var fills = new List<FillWithSprite>(allFills.Count);
        foreach (var f in allFills)
            fills.Add(new FillWithSprite(f, palette.ResolveSprite(f)));

        var layers = GameCellVisualBuilder.Build(fills, ctx, revealRubberOnExit);

        foreach (var layer in layers)
            SpawnLayer(layer);
    }

    private void SpawnLayer(CellLayer layer)
    {
        var go = new GameObject($"Layer_{layer.Fill.FillType.Name}",
            typeof(RectTransform), typeof(Image));
        go.transform.SetParent(layersRoot, false);

        var img = go.GetComponent<Image>();
        img.sprite = layer.Sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;

        ApplyLayout(go.GetComponent<RectTransform>(), layer.Mode);
    }

    private void ApplyLayout(RectTransform rt, LayerMode mode)
    {
        switch (mode)
        {
            case LayerMode.Full:
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                break;
            case LayerMode.CornerTopRight: SetupCorner(rt, new Vector2(1, 1)); break;
            case LayerMode.CornerTopLeft: SetupCorner(rt, new Vector2(0, 1)); break;
            case LayerMode.CornerBottomRight: SetupCorner(rt, new Vector2(1, 0)); break;
            case LayerMode.CornerBottomLeft: SetupCorner(rt, new Vector2(0, 0)); break;
        }
    }

    private void SetupCorner(RectTransform rt, Vector2 anchor)
    {
        var size = layersRoot.rect.size * cornerScale;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        var dir = new Vector2(anchor.x == 1 ? -1 : 1, anchor.y == 1 ? -1 : 1);
        rt.anchoredPosition = dir * cornerPadding;
    }

    private void ClearVisual()
    {
        for (int i = layersRoot.childCount - 1; i >= 0; i--)
        {
            var child = layersRoot.GetChild(i).gameObject;
            child.transform.SetParent(null, false);
            Destroy(child);
        }
    }
}