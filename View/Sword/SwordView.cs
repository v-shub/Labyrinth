using UnityEngine;

[CreateAssetMenu(fileName = "SwordView", menuName = "Scriptable Objects/SwordView")]
public class SwordView : FillViewBase
{
    public override ICellFill CreateFill(CellFillCreationContext ctx) => new Sword();
}
