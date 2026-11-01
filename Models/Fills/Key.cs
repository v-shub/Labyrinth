using Newtonsoft.Json;
using System;

public class Key : CellFillBase
{
    public int PlayerNum = 0;

    [JsonConstructor]
    public Key(int playerNum)
    {
        if (playerNum < 1)
            throw new Exception("Нельзя создать ключ с номером пользователя меньше 1");
        PlayerNum = playerNum;
    }

    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        if (PlayerNum != ctx.Player.PlayerNum) return; // чужой ключ не виден
        if (ctx.Player.HasKey) return;
        ctx.Player.AddKey();
        ctx.AddMessage("Вы нашли свой ключ.");
    }
}