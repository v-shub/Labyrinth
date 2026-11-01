using UnityEngine;
using TMPro;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform gridParent;
    [SerializeField] private int rows = 10;
    [SerializeField] private int cols = 10;

    [Header("Labels")]
    [SerializeField] private Transform topLabelsParent;
    [SerializeField] private Transform leftLabelsParent;
    [SerializeField] private float labelFontSize = 24f;
    [SerializeField] private Color labelColor = Color.black;

    // Алфавит для подписей столбцов.
    // Порядок: без Ё, но с Ё на нужной позиции — задаём арифметикой + спецслучай.
    private const char FirstLetter = 'А';
    private const char YoLetter = 'Ё';
    private const int YoIndex = 6; // 0-й = А, 6-й = Ё (А,Б,В,Г,Д,Е,Ё,...)

    public Cell[,] Cells { get; private set; }

    private void Start()
    {
        if (Cells == null) Generate();
    }

    public void Generate()
    {
        GenerateGrid();
        GenerateColumnLabels();
        GenerateRowLabels();
    }

    private void GenerateGrid()
    {
        Cells = new Cell[cols, rows];
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var go = Instantiate(cellPrefab, gridParent);
                var cell = go.GetComponent<Cell>();
                cell.Init(x, y);
                Cells[x, y] = cell;
            }
        }
    }

    private void GenerateColumnLabels()
    {
        for (int x = 0; x < cols; x++)
        {
            CreateLabel(topLabelsParent, GetColumnLetter(x).ToString());
        }
    }

    private void GenerateRowLabels()
    {
        for (int y = 0; y < rows; y++)
        {
            CreateLabel(leftLabelsParent, (y + 1).ToString());
        }
    }

    private void CreateLabel(Transform parent, string value)
    {
        var go = new GameObject($"Label_{value}", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = labelFontSize;
        text.color = labelColor;
    }

    /// <summary>
    /// Возвращает букву для столбца по индексу.
    /// 0 → А, 1 → Б, ..., 6 → Ё, 7 → Ж, ...
    /// Работает для любого количества столбцов, пока хватает алфавита.
    /// </summary>
    private static char GetColumnLetter(int index)
    {
        if (index < 0) return '?';

        // Позиция Ё — отдаём вручную.
        if (index == YoIndex) return YoLetter;

        // До Ё — обычная арифметика (А..Е).
        // После Ё — сдвигаем индекс на 1 назад, потому что Ё "вставлена" между Е и Ж.
        int shift = index > YoIndex ? -1 : 0;
        char letter = (char)(FirstLetter + index + shift);

        if (letter > 'Я')
        {
            Debug.LogWarning($"GridGenerator: закончились буквы для столбца {index}.");
            return '?';
        }

        return letter;
    }
}