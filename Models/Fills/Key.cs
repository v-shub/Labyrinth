using Newtonsoft.Json;
using System;
using System.Linq;

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
        if (ctx.Player.HasKey) return;                 // ключ уже в инвентаре

        // Ключ на клетке с драконом — это хранилище дракона, а не «ключ на земле».
        // Его судьбой управляет Dragon.OnEnter (бой / забрать / выбор).
        if (ctx.Map[row, col].Fills.OfType<Dragon>().Any()) return;

        ctx.Player.AddKey();
        ctx.AddMessage("Вы нашли свой ключ.");

        // Ключ перешёл в инвентарь — убираем его с клетки (runtime, map.json не меняется).
        ctx.Map[row, col].Remove(this);
        ctx.PickedUpThisTurn.Add((row, col, this));
    }
}