using System;
using System.IO;
using Newtonsoft.Json;

public class Map
{
    [JsonProperty] public int Rows { get; private set; }
    [JsonProperty] public int Cols { get; private set; }
    [JsonProperty] public CellContainer[] Cells { get; private set; }

    [JsonProperty] public string Name { get; private set; }
    [JsonProperty] public string Description { get; private set; }

    public Map() : this(10, 10) { }

    public Map(int rows, int cols) : this(rows, cols, null, null) { }

    public Map(int rows, int cols, string name, string description)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));

        Rows = rows;
        Cols = cols;
        Name = name;
        Description = description;

        Cells = new CellContainer[rows * cols];
        for (int i = 0; i < Cells.Length; i++)
            Cells[i] = new CellContainer();
    }

    // Простое присваивание. Валидация — в MapValidator.
    public void SetMetadata(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public CellContainer this[int row, int col]
    {
        get
        {
            CheckBounds(row, col);
            return Cells[row * Cols + col];
        }
        set
        {
            CheckBounds(row, col);
            Cells[row * Cols + col] = value ?? new CellContainer();
        }
    }

    public void changeCell(int row, int col, CellContainer cell) => this[row, col] = cell;

    private void CheckBounds(int row, int col)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols)
            throw new Exception("Попытка изменить несуществующую ячейку");
    }

    public void SaveToJson(string filePath)
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static Map LoadFromJson(string filePath)
    {
        string json = File.ReadAllText(filePath);
        var map = JsonConvert.DeserializeObject<Map>(json);
        if (map == null)
            throw new JsonSerializationException("Не удалось десериализовать Map (null).");

        // Защита от старых/битых JSON — это НЕ валидация, а восстановление.
        map.Name ??= string.Empty;
        map.Description ??= string.Empty;

        // Защита: массив мог не восстановиться (старые файлы или битый JSON).
        if (map.Cells == null || map.Cells.Length != map.Rows * map.Cols)
        {
            int expected = map.Rows * map.Cols;
            var restored = new CellContainer[expected];
            for (int i = 0; i < expected; i++)
                restored[i] = map.Cells != null && i < map.Cells.Length && map.Cells[i] != null
                    ? map.Cells[i]
                    : new CellContainer();
            map.Cells = restored;
        }
        else
        {
            for (int i = 0; i < map.Cells.Length; i++)
                map.Cells[i] ??= new CellContainer();
        }

        return map;
    }
}