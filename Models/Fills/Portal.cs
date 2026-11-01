using Newtonsoft.Json;
using System;

public class Portal : CellFillBase
{
    public int Num = 0;

    [JsonConstructor]
    public Portal(int num)
    {
        if (num <= 0)
            throw new Exception("Нельзя создать портал с номером меньше 1");
        Num = num;
    }

    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        // Телепорт не срабатывает, если только что прибыли из портала.
        if (arrival == Arrival.FromPortal) return;

        var next = PortalNavigator.Next(ctx.Map, Num);
        if (next == null) return;

        ctx.AddMessage("Вы попали в портал. Вы вышли из портала.");
        ctx.TeleportTo(next.Value.row, next.Value.col);
    }
}