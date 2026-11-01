using UnityEngine;

[CreateAssetMenu(fileName = "ExitView", menuName = "Scriptable Objects/ExitView")]
public class ExitView : FillViewBase
{
    public override ICellFill CreateFill(CellFillCreationContext ctx) => new Exit();
}
