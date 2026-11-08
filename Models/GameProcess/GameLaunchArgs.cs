/// <summary>
/// Мост параметров между сценами: номер игрока и стартовая клетка.
/// Одноразовый: Consume сбрасывает HasData.
/// </summary>
public static class GameLaunchArgs
{
    public static int PlayerNum { get; private set; } = 1;
    public static int StartRow { get; private set; } = 0;
    public static int StartCol { get; private set; } = 0;
    public static bool HasData { get; private set; } = false;

    public static void Set(int playerNum, int startRow, int startCol)
    {
        PlayerNum = playerNum;
        StartRow = startRow;
        StartCol = startCol;
        HasData = true;
    }

    public static void Consume(out int playerNum, out int startRow, out int startCol)
    {
        playerNum = HasData ? PlayerNum : 1;
        startRow = HasData ? StartRow : 0;
        startCol = HasData ? StartCol : 0;
        HasData = false;
    }
}