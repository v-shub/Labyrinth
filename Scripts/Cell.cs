using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Cell : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform layersRoot;
    [SerializeField] private float cornerScale = 0.45f;
    [SerializeField] private float cornerPadding = 2f;

    [Header("Number badge")]
    [SerializeField] private TMP_FontAsset digitFont;
    [SerializeField] private Color digitColor = Color.white;
    [SerializeField] private float digitScale = 0.25f;
    [SerializeField] private Vector2 digitPadding = new(4f, 4f);

    [Header("Drag")]
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private float dragGhostSize = 64f;

    public int X { get; private set; }
    public int Y { get; private set; }

    private readonly List<FillWithSprite> _fills = new();
    public IReadOnlyList<FillWithSprite> Fills => _fills;

    private GameObject _dragGhost;

    public void Init(int x, int y)
    {
        X = x;
        Y = y;
        name = $"Cell ({x},{y})";
        ClearVisual();
    }

    public void AddFill(ICellFill fill, Sprite sprite)
    {
        if (fill == null) return;
        _fills.Add(new FillWithSprite(fill, sprite));
        RebuildVisual();
    }
    /// <summary>
    /// Заменяет спрайт уже добавленного наполнения (например, после смены FlowIn/FlowOut у River).
    /// Если такого наполнения нет — ничего не делает.
    /// </summary>
    public void ReplaceSprite(ICellFill fill, Sprite sprite)
    {
        for (int i = 0; i < _fills.Count; i++)
        {
            if (ReferenceEquals(_fills[i].Fill, fill))
            {
                _fills[i] = new FillWithSprite(fill, sprite);
                RebuildVisual();
                return;
            }
        }
    }
    /// <summary>
    /// Возвращает визуально верхний слой — последний в порядке отрисовки CellVisualBuilder.
    /// Это и есть "обратный порядок размещения из CellVisualBuilder".
    /// </summary>
    public FillWithSprite? GetTopVisualFill()
    {
        if (_fills.Count == 0) return null;

        var layers = CellVisualBuilder.Build(_fills);
        if (layers.Count == 0) return null;

        var topLayer = layers[layers.Count - 1]; // верхний визуально
        // Находим соответствующий FillWithSprite по ссылке на Fill.
        for (int i = 0; i < _fills.Count; i++)
            if (ReferenceEquals(_fills[i].Fill, topLayer.Fill))
                return _fills[i];

        return null;
    }

    /// <summary>
    /// Удаляет визуально верхний слой (последний по CellVisualBuilder).
    /// </summary>
    public FillWithSprite? RemoveTopFill()
    {
        var top = GetTopVisualFill();
        if (!top.HasValue) return null;

        for (int i = 0; i < _fills.Count; i++)
        {
            if (ReferenceEquals(_fills[i].Fill, top.Value.Fill))
            {
                _fills.RemoveAt(i);
                break;
            }
        }
        RebuildVisual();
        return top;
    }

    public void RefreshVisual() => RebuildVisual();

    // ---------- Drag ----------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_fills.Count == 0) return;

        var top = GetTopVisualFill();
        if (!top.HasValue) return;

        var canvas = dragCanvas != null ? dragCanvas : GetComponentInParent<Canvas>();
        _dragGhost = new GameObject("CellDragGhost", typeof(RectTransform), typeof(Image));
        _dragGhost.transform.SetParent(canvas.transform, false);
        _dragGhost.transform.SetAsLastSibling();

        var img = _dragGhost.GetComponent<Image>();
        img.sprite = top.Value.Sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;

        var rt = _dragGhost.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(dragGhostSize, dragGhostSize);

        UpdateGhost(eventData);
    }

    public void OnDrag(PointerEventData eventData) => UpdateGhost(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragGhost != null) Destroy(_dragGhost);
        if (_fills.Count == 0) return;

        var mapController = Object.FindFirstObjectByType<MapController>();
        if (mapController == null) return;

        var hit = eventData.pointerCurrentRaycast.gameObject;
        var target = hit != null ? hit.GetComponentInParent<Cell>() : null;

        // Верхний визуально слой (по CellVisualBuilder), а не _fills[^1].
        var top = GetTopVisualFill();
        if (!top.HasValue) return;

        // 1) Вынесли за пределы карты — удаление + пересчёт номеров.
        if (target == null)
        {
            RemoveTopFill();
            mapController.RemoveFill(this, top.Value.Fill);
            NumberAllocator.ReindexAfterRemoval(mapController.Map);
            mapController.RefreshAllVisuals();
            return;
        }

        // 2) Отпустили на ту же клетку — ничего.
        if (target == this) return;

        // 3) Перенос на другую клетку. Номер сохраняется.
        var container = mapController.Map[target.Y, target.X];

        if (!container.CanAdd(top.Value.Fill))
        {
            Debug.LogWarning($"Перенос невозможен: конфликт в ({target.Y},{target.X})");
            return; // верхний остался в исходной клетке
        }

        // Убираем из источника (логика + визуал).
        RemoveTopFill();
        mapController.RemoveFill(this, top.Value.Fill);

        if (mapController.TryPlace(target.Y, target.X, top.Value.Fill))
        {
            target.AddFill(top.Value.Fill, top.Value.Sprite);
        }
        else
        {
            // Откат.
            mapController.TryPlace(this.Y, this.X, top.Value.Fill);
            AddFill(top.Value.Fill, top.Value.Sprite);
        }
    }

    private void UpdateGhost(PointerEventData e)
    {
        if (_dragGhost == null) return;
        var canvas = _dragGhost.transform.parent as RectTransform;
        if (canvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas, e.position, e.pressEventCamera, out var local);
        _dragGhost.transform.localPosition = local;
    }

    // ---------- Визуал ----------

    private void ClearVisual()
    {
        for (int i = layersRoot.childCount - 1; i >= 0; i--)
            Destroy(layersRoot.GetChild(i).gameObject);
    }

    private void RebuildVisual()
    {
        ClearVisual();

        foreach (var layer in CellVisualBuilder.Build(_fills))
        {
            var go = new GameObject(
                $"Layer_{layer.Fill.FillType.Name}",
                typeof(RectTransform), typeof(Image));
            go.transform.SetParent(layersRoot, false);

            var img = go.GetComponent<Image>();
            img.sprite = layer.Sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;

            var rt = go.GetComponent<RectTransform>();
            ApplyLayout(rt, layer.Mode);

            if (layer.Number > 0)
                BuildNumber(rt, layer.Number);
            else if (!string.IsNullOrEmpty(layer.Label))
                BuildLabel(rt, layer.Label);
        }
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

    private void BuildNumber(RectTransform parent, int number)
    {
        var go = new GameObject("Number", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.sizeDelta = new Vector2(parent.rect.width * 0.6f, parent.rect.width * 0.4f);
        rt.anchoredPosition = new Vector2(-digitPadding.x, digitPadding.y);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = number.ToString();
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.color = digitColor;
        tmp.fontSize = parent.rect.width * digitScale;
        if (digitFont != null) tmp.font = digitFont;
    }
    private void BuildLabel(RectTransform parent, string label)
    {
        var go = new GameObject("RiverEndLabel", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0); // правый нижний угол
        rt.pivot = new Vector2(1, 0);
        rt.sizeDelta = new Vector2(parent.rect.width * 0.5f, parent.rect.width * 0.5f);
        rt.anchoredPosition = new Vector2(-digitPadding.x, digitPadding.y);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.color = digitColor;
        tmp.fontSize = parent.rect.width * 0.5f; // крупнее цифр
        if (digitFont != null) tmp.font = digitFont;
    }
    public void OnDrop(PointerEventData eventData) { }
    public void ClearAll()
    {
        _fills.Clear();
        RebuildVisual();
    }
}