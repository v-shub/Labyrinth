using UnityEngine;

[CreateAssetMenu(fileName = "DragonView", menuName = "Scriptable Objects/DragonView")]
public class DragonView : FillViewBase
{
    public override ICellFill CreateFill(CellFillCreationContext ctx) => new Dragon();
}
