using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Источник вьюхи")]
    [Tooltip("Единая палитра. Если задана — вьюха берётся из неё по viewTypeName.")]
    [SerializeField] private FillPalette palette;

    [Tooltip("Имя C#-класса вьюхи внутри палитры, например 'PortalView'.")]
    [SerializeField] private string viewTypeName;

    [Tooltip("Legacy: прямая ссылка на вьюху. Имеет приоритет над палитрой, " +
             "если задана. Можно оставить пустой.")]
    [SerializeField] private FillViewBase fillView;

    private Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();

        var view = ResolveView();
        var img = GetComponent<Image>();
        if (img != null && view != null && view.Sprite != null)
            img.sprite = view.Sprite;
    }

    private FillViewBase ResolveView()
    {
        if (fillView != null) return fillView;
        if (palette == null || string.IsNullOrEmpty(viewTypeName)) return null;
        return palette.GetByViewTypeName(viewTypeName);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var view = ResolveView();
        if (view == null)
        {
            Debug.LogError($"PaletteItem: не удалось получить FillViewBase " +
                           $"(palette={(palette ? palette.name : "null")}, " +
                           $"viewTypeName='{viewTypeName}')");
            return;
        }

        _dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        _dragGhost.transform.SetParent(_canvas.transform, false);
        _dragGhost.transform.SetAsLastSibling();

        var img = _dragGhost.GetComponent<Image>();
        img.sprite = view.Sprite;
        img.raycastTarget = false;

        var rt = _dragGhost.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);
        UpdateGhost(eventData);
    }

    private GameObject _dragGhost;

    public void OnDrag(PointerEventData eventData) => UpdateGhost(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragGhost != null) Destroy(_dragGhost);
        _dragGhost = null;

        var view = ResolveView();
        if (view == null) return;

        var hit = eventData.pointerCurrentRaycast.gameObject;
        if (hit == null) return;

        var cell = hit.GetComponentInParent<Cell>();
        if (cell == null) return;

        var mapController = Object.FindFirstObjectByType<MapController>();
        if (mapController == null) return;

        var map = mapController.Map;

        if (view is KeyView && !NumberAllocator.CanAddKey(map))
        {
            Debug.LogWarning("На карте уже 4 ключа — больше нельзя");
            return;
        }

        ICellFill fill;
        try
        {
            fill = view.CreateFill(new CellFillCreationContext(map, cell.Y, cell.X));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Не удалось создать наполнение: {e.Message}");
            return;
        }

        if (mapController.TryPlace(cell.Y, cell.X, fill))
            cell.AddFill(fill, view.Sprite);
    }

    private void UpdateGhost(PointerEventData e)
    {
        if (_dragGhost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform, e.position, _canvas.worldCamera, out var local);
        _dragGhost.transform.localPosition = local;
    }
}