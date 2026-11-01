using UnityEngine;

[CreateAssetMenu(fileName = "SwampView", menuName = "Scriptable Objects/SwampView")]
public class SwampView : FillViewBase
{
    public override ICellFill CreateFill(CellFillCreationContext ctx) => new Swamp();
}
