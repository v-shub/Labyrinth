public class RubberRoom : CellFillBase
{
    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        // Старт/телепорт — направление входа неизвестно, резинка не активируется.
        if (arrival == Arrival.Start || arrival == Arrival.FromPortal) return;

        ctx.Player.EnterRubber(ctx.LastMoveDirection);
        ctx.AddMessage("Вы прошли."); // резинка маскируется под пустую клетку
    }
}