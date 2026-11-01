using Newtonsoft.Json;
using System;

public enum RiverEnd
{
    None,
    Source,   // исток
    Mouth     // устье
}

public class River : CellFillBase
{
    public Direction FlowIn;
    public Direction FlowOut;
    public RiverEnd End = RiverEnd.None;

    [JsonConstructor]
    public River(Direction flowIn, Direction flowOut)
    {
        if (flowIn == flowOut)
            throw new Exception("Река не может течь в обратную сторону в той же клетке, и одна клетка не может быть одновременно истоком и устьем");
        FlowIn = flowIn;
        FlowOut = flowOut;
    }

    public override void OnEnter(GameContext ctx, int row, int col, Arrival arrival)
    {
        // Сообщение о характере входа (кроме смыва — там уже «Вас смыло по течению.»).
        if (arrival != Arrival.FromRiver)
        {
            bool cameAlongFlow = ctx.LastMoveDirection == FlowIn;
            if (cameAlongFlow) ctx.AddMessage("Вы пошли по течению.");
            else ctx.AddMessage("Вы вошли в реку.");
        }

        // Исток/устье — всегда после входа/смыва.
        if (End == RiverEnd.Source) ctx.AddMessage("Вы находитесь в истоке реки.");
        else if (End == RiverEnd.Mouth) ctx.AddMessage("Вы находитесь в устье реки.");

        // Смыв не повторяется.
        if (arrival == Arrival.FromRiver) return;
        if (End == RiverEnd.Mouth) return;

        ctx.AddMessage("Вас смыло по течению.");
        ctx.RiverCarry(row, col, FlowOut);
    }
}