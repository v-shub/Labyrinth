using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RiverView", menuName = "Scriptable Objects/RiverView")]
public class RiverView : FillViewBase
{
    [System.Serializable]
    public struct Entry
    {
        public Direction FlowIn;
        public Direction FlowOut;
        public Sprite Sprite;
    }

    [SerializeField] private List<Entry> _entries = new();

    private Dictionary<(Direction, Direction), Sprite> _map;

    public Sprite GetSprite(River river)
    {
        _map ??= BuildMap();
        return _map.TryGetValue((river.FlowIn, river.FlowOut), out var s) ? s : null;
    }

    // Реализация абстрактного метода. 
    // Возвращает базовый сегмент (например, если захотите кинуть реку из палитры).
    public override ICellFill CreateFill(CellFillCreationContext ctx)
    {
        return new River(Direction.Left, Direction.Right);
    }

    private Dictionary<(Direction, Direction), Sprite> BuildMap()
    {
        var map = new Dictionary<(Direction, Direction), Sprite>(_entries.Count);
        foreach (var e in _entries)
            map[(e.FlowIn, e.FlowOut)] = e.Sprite;
        return map;
    }
}