public class Sword : CellFillBase
{
    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        ctx.Player.AddSword();
        ctx.AddMessage("Вы нашли меч.");

        // Меч перешёл в инвентарь — убираем его с клетки (runtime, map.json не меняется).
        // PickedUpThisTurn — чтобы UI показал меч в момент подбора.
        ctx.Map[row, col].Remove(this);
        ctx.PickedUpThisTurn.Add((row, col, this));
    }
}