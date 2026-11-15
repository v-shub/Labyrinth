public class Dragon : CellFillBase
{
    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        var player = ctx.Player;

        // Новый эпизод дракона — сбрасываем «бежать уже добавлено».
        ctx.ResetDragonEncounter();

        // Обязательный доп. ход ставим сразу; сообщение «бежать» — в конце.
        player.MustMoveAgain = true;

        ctx.AddMessage("Вы встретили дракона.");

        // ВАЖНО: проверяем ключ именно у ЭТОГО дракона.
        bool dragonHasKey = DragonStorage.HoldsKeyOf(ctx.Map, row, col, player.PlayerNum);
        if (dragonHasKey)
            ctx.AddMessage("Вы видите, что ваш ключ у дракона.");

        // Ветка 1: ключ игрока у ЭТОГО дракона + есть меч — предложить бой.
        if (dragonHasKey && player.HasSword)
        {
            ctx.RequestDragonFight(row, col);
            return; // «бежать» добавит OnDragonFight после ответа UI
        }

        // Ветка 2: есть и ключ, и меч — выбор, что отдать.
        if (player.HasKey && player.HasSword)
        {
            ctx.RequestDragonChoice(row, col);
            return; // «бежать» добавит OnDragonChoice после ответа UI
        }

        // Ветка 3: есть ключ, нет меча — дракон забирает ключ.
        if (player.HasKey && !player.HasSword)
        {
            player.RemoveKey();
            DragonStorage.TakeKey(ctx.Map, row, col, player.PlayerNum);
            ctx.AddMessage("Дракон забрал ваш ключ.");
            ctx.FinishDragonEncounter();
            return;
        }

        // Ветка 4: нечего выбирать (нет ключа у игрока и нет ключа у дракона).
        ctx.FinishDragonEncounter();
    }
}