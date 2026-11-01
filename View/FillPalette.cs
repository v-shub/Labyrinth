using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Единый источник правды по вьюхам наполнений.
/// Один ассет — один список. Все потребители (PaletteItem, MapController)
/// ссылаются на этот ассет, а не заводят собственные списки.
/// </summary>
[CreateAssetMenu(fileName = "FillPalette", menuName = "Scriptable Objects/FillPalette")]
public class FillPalette : ScriptableObject
{
    [SerializeField] private List<FillViewBase> _views = new();

    private Dictionary<Type, FillViewBase> _byType;
    private Dictionary<string, FillViewBase> _byViewTypeName;

    public IReadOnlyList<FillViewBase> Views => _views;

    public T Get<T>() where T : FillViewBase
    {
        EnsureLookup();
        return _byType.TryGetValue(typeof(T), out var v) ? v as T : null;
    }

    /// <summary>Ищет вьюху по имени её C#-класса, например "PortalView".</summary>
    public FillViewBase GetByViewTypeName(string viewTypeName)
    {
        if (string.IsNullOrEmpty(viewTypeName)) return null;
        EnsureLookup();
        return _byViewTypeName.TryGetValue(viewTypeName, out var v) ? v : null;
    }

    /// <summary>
    /// Возвращает спрайт для конкретного наполнения.
    /// Для River — через RiverView.GetSprite (зависит от FlowIn/FlowOut).
    /// Для остальных — FillViewBase.Sprite.
    /// </summary>
    public Sprite ResolveSprite(ICellFill fill)
    {
        if (fill == null) return null;
        EnsureLookup();

        if (fill is River river)
        {
            if (_byType.TryGetValue(typeof(River), out var rv) && rv is RiverView riverView)
                return riverView.GetSprite(river);
            return null;
        }

        if (fill.FillType != null &&
            _byType.TryGetValue(fill.FillType, out var view) && view != null)
            return view.Sprite;

        return null;
    }

    private void EnsureLookup()
    {
        if (_byType != null && _byType.Count == _views.Count) return;

        _byType = new Dictionary<Type, FillViewBase>();
        _byViewTypeName = new Dictionary<string, FillViewBase>();

        foreach (var v in _views)
        {
            if (v == null) continue;

            var viewTypeName = v.GetType().Name;
            _byViewTypeName[viewTypeName] = v;

            // RiverView -> River, PortalView -> Portal, ExitView -> Exit, ...
            var fillTypeName = viewTypeName.EndsWith("View")
                ? viewTypeName.Substring(0, viewTypeName.Length - "View".Length)
                : viewTypeName;

            var fillType = Type.GetType(fillTypeName);
            if (fillType != null)
                _byType[fillType] = v;
            else
                Debug.LogWarning(
                    $"FillPalette: не удалось найти C#-тип '{fillTypeName}' для {viewTypeName}. " +
                    $"Проверьте, что класс наполнения называется так же, как вьюха без суффикса View.");
        }
    }

    private void OnValidate() => _byType = null; // сброс кэша при правках в инспекторе
}