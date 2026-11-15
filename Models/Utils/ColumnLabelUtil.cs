// ColumnLabelUtil.cs
public static class ColumnLabelUtil
{
    private const char FirstLetter = 'А';
    private const char YoLetter = 'Ё';
    private const int YoIndex = 6;

    public static char GetColumnLetter(int index)
    {
        if (index < 0) return '?';
        if (index == YoIndex) return YoLetter;
        int shift = index > YoIndex ? -1 : 0;
        char letter = (char)(FirstLetter + index + shift);
        return letter > 'Я' ? '?' : letter;
    }

    public static string GetCellName(int row, int col)
        => $"{GetColumnLetter(col)}{row + 1}";
}