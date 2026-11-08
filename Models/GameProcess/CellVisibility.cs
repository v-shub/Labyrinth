/// <summary>
/// «Что игрок знает про наполнение клетки прямо сейчас».
/// Правила повторяют OnEnter-и: невидимое логикой — невидимо и визуально.
/// </summary>
public static class CellVisibility
{
    public static bool IsVisibleToPlayer(ICellFill fill, GameContext ctx)
    {
        var p = ctx.Player;

        switch (fill)
        {
            // Чужой ключ не виден. Свой — виден только пока не подобран.
            case Key k:
                if (k.PlayerNum != p.PlayerNum) return false;
                if (p.HasKey) return false;
                return true;

            // Выход виден только при наличии своего ключа.
            case Exit _:
                return p.HasKey;

            // Резинка статически невидима; эфемерный показ — на уровне билдера.
            case RubberRoom _:
                return false;

            // Болото видно, только когда игрок «в болоте» (пропускает ход).
            case Swamp _:
                return p.SkipNextTurn;

            // Река, портал, дракон, меч — игрок знает всегда.
            default:
                return true;
        }
    }
}