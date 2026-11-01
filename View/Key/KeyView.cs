using UnityEngine;

[CreateAssetMenu(fileName = "KeyView", menuName = "Scriptable Objects/KeyView")]
public class KeyView : FillViewBase
{
    public override ICellFill CreateFill(CellFillCreationContext ctx)
        => new Key(NumberAllocator.NextKeyNumber(ctx.Map));
}