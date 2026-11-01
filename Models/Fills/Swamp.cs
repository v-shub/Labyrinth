public class Swamp : CellFillBase
{
    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        ctx.Player.SkipNextTurn = true;
        ctx.AddMessage("Вы попали в болото. Вы пропускаете свой следующий ход.");
    }
}