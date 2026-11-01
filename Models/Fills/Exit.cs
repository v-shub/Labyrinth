public class Exit : CellFillBase
{
    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        if (!ctx.Player.HasKey) return; // «не виден», обработается как пустая клетка
        ctx.AddMessage("Вы нашли выход.");
        ctx.Win();
    }
}