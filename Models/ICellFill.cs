using System;

public interface ICellFill
{
    Type FillType { get; }

    /// <summary>Активируется, когда игрок оказывается на клетке с этим наполнением.</summary>
    void OnEnter(GameContext ctx, int row, int col, Arrival arrival);
}