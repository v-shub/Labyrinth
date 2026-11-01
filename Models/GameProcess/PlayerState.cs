public class PlayerState
{
    public int PlayerNum { get; }

    public int Row { get; set; }
    public int Col { get; set; }

    public bool HasKey { get; private set; }
    public int SwordCount { get; private set; }
    public bool HasSword => SwordCount > 0;

    public bool SkipNextTurn { get; set; }
    public bool MustMoveAgain { get; set; }

    // Резинка: активна ли она и откуда игрок вошёл.
    public bool OnRubber { get; private set; }
    public Direction? RubberEntryFrom { get; private set; }

    public PlayerState(int playerNum, int row, int col)
    {
        PlayerNum = playerNum;
        Row = row;
        Col = col;
    }

    public void AddKey() => HasKey = true;
    public void RemoveKey() => HasKey = false;
    public void AddSword() => SwordCount++;
    public void RemoveSword() { if (SwordCount > 0) SwordCount--; }

    /// <summary>Активировать резинку. from == null — резинка не ограничивает.</summary>
    public void EnterRubber(Direction? from)
    {
        OnRubber = true;
        RubberEntryFrom = from;
    }

    public void LeaveRubber()
    {
        OnRubber = false;
        RubberEntryFrom = null;
    }

    /// <summary>Разрешён ли ход в направлении desired с учётом резинки.</summary>
    public bool RubberAllowsMove(Direction desired)
    {
        if (!OnRubber) return true;
        if (RubberEntryFrom == null) return true;
        return desired == DirectionUtil.Opposite(RubberEntryFrom.Value);
    }
}