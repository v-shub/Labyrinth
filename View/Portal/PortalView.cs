using UnityEngine;

[CreateAssetMenu(fileName = "PortalView", menuName = "Scriptable Objects/PortalView")]
public class PortalView : FillViewBase
{
    public override ICellFill CreateFill(CellFillCreationContext ctx)
        => new Portal(NumberAllocator.NextPortalNumber(ctx.Map));
}