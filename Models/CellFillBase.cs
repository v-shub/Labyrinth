using Newtonsoft.Json;
using System;

public abstract class CellFillBase : ICellFill
{
    private readonly Type _fillType;
    protected CellFillBase() => _fillType = GetType();

    [JsonIgnore]
    public Type FillType => _fillType;

    public abstract void OnEnter(GameContext ctx, int row, int col, Arrival arrival);
}