public class Dragon : CellFillBase
{
    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        var player = ctx.Player;
        ctx.AddMessage("Вы встретили дракона.");

        bool dragonHasKey = DragonStorage.HoldsKeyOf(ctx.Map, player.PlayerNum);
        if (dragonHasKey)
            ctx.AddMessage("Вы видите, что ваш ключ у дракона.");

        // Случай 1: ключ игрока у дракона + есть меч — предложить бой.
        if (dragonHasKey && player.HasSword)
        {
            ctx.RequestDragonFight(row, col);
            return;
        }

        // Случай 2: есть ключ, нет меча — дракон забирает ключ.
        if (player.HasKey && !player.HasSword)
        {
            player.RemoveKey();
            DragonStorage.TakeKey(ctx.Map, row, col, player.PlayerNum);
            ctx.AddMessage("Дракон забрал ваш ключ.");
            player.MustMoveAgain = true;
            return;
        }

        // Случай 3: есть и ключ, и меч — выбор, что отдать.
        if (player.HasKey && player.HasSword)
        {
            ctx.RequestDragonChoice(row, col);
            return;
        }

        // Иначе — просто «передвинься ещё раз».
        player.MustMoveAgain = true;
    }
}