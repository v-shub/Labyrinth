using UnityEngine;

[CreateAssetMenu(fileName = "RubberRoomView", menuName = "Scriptable Objects/RubberRoomView")]
public class RubberRoomView : FillViewBase
{
    public override ICellFill CreateFill(CellFillCreationContext ctx) => new RubberRoom();
}
