using UnityEngine;

public abstract class FillViewBase : ScriptableObject
{
    public Sprite Sprite;
    public abstract ICellFill CreateFill(CellFillCreationContext ctx);
}

public readonly struct CellFillCreationContext
{
    public readonly Map Map;
    public readonly int Row;
    public readonly int Col;

    public CellFillCreationContext(Map map, int row, int col)
    {
        Map = map;
        Row = row;
        Col = col;
    }
}