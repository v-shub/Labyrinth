public class Sword : CellFillBase
{
    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        ctx.Player.AddSword();
        ctx.AddMessage("Вы нашли меч.");
    }
}