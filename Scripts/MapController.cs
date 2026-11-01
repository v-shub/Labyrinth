using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MapController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GridGenerator gridGenerator;

    [Header("Палитра")]
    [Tooltip("Единый список вьюх. Используется для восстановления спрайтов при загрузке. " +
             "Тот же ассет должен быть назначен в PaletteItem.")]
    [SerializeField] private FillPalette fillPalette;

    [Header("Clear")]
    [Tooltip("Перезагружать сцену при очистке карты. " +
             "Для сцены-редактора карты — самый простой и надёжный способ.")]
    [SerializeField] private bool clearByReloadingScene = true;

    [Header("Load")]
    [Tooltip("Имя файла в Application.dataPath. Пусто — map.json.")]
    [SerializeField] private string mapFileName = "map.json";

    [Header("Метаданные (UI)")]
    [Tooltip("Поле ввода названия карты. Character Limit рекомендуется = 64.")]
    [SerializeField] private TMP_InputField nameInput;

    [Tooltip("Поле ввода описания карты. Line Type = Multi Line Newline, Character Limit = 512.")]
    [SerializeField] private TMP_InputField descriptionInput;

    [Header("Сообщения")]
    [Tooltip("Компонент, который показывает сообщения об ошибках/успехе на сцене.")]
    [SerializeField] private MessageView messageView;

    private Map _map;

    public Map Map
    {
        get { EnsureMap(); return _map; }
    }

    void Awake()
    {
        if (gridGenerator == null)
            gridGenerator = FindFirstObjectByType<GridGenerator>();
    }

    private void EnsureMap()
    {
        if (_map != null) return;
        if (gridGenerator.Cells == null) gridGenerator.Generate();

        int rows = gridGenerator.Cells.GetLength(1);
        int cols = gridGenerator.Cells.GetLength(0);

        _map = new Map(rows, cols);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                _map[r, c] = new CellContainer();
    }

    public bool TryPlace(int row, int col, ICellFill fill)
    {
        EnsureMap();
        var container = _map[row, col];
        if (!container.CanAdd(fill))
        {
            Debug.LogWarning($"Нельзя добавить {fill.FillType.Name} в ({row},{col}): конфликт правил.");
            return false;
        }
        container.Add(fill);
        return true;
    }

    public void RemoveFill(Cell cell, ICellFill fill)
    {
        EnsureMap();
        _map[cell.Y, cell.X].Remove(fill);
    }

    public void RefreshAllVisuals()
    {
        if (gridGenerator == null || gridGenerator.Cells == null) return;
        int rows = gridGenerator.Cells.GetLength(1);
        int cols = gridGenerator.Cells.GetLength(0);
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
                gridGenerator.Cells[x, y]?.RefreshVisual();
    }

    // ---------- Сообщения ----------

    private void ShowError(string message)
    {
        Debug.LogError(message);
        if (messageView != null) messageView.ShowError(message);
    }

    private void ShowSuccess(string message)
    {
        Debug.Log(message);
        if (messageView != null) messageView.ShowSuccess(message);
    }

    // ---------- Очистка ----------

    public void ClearMap()
    {
        if (clearByReloadingScene)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }
        ClearMapManual();
    }

    private void ClearMapManual()
    {
        try
        {
            EnsureMap();
            if (gridGenerator == null || gridGenerator.Cells == null)
            {
                ShowError("Очистка невозможна: сетка не создана.");
                return;
            }

            int h = gridGenerator.Cells.GetLength(1);
            int w = gridGenerator.Cells.GetLength(0);

            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                    _map[r, c] = new CellContainer();

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    gridGenerator.Cells[x, y]?.ClearAll();

            // Метаданные тоже сбрасываем — карта пустая, значит и название/описание неактуальны.
            _map.SetMetadata(string.Empty, string.Empty);
            if (nameInput != null) nameInput.text = string.Empty;
            if (descriptionInput != null) descriptionInput.text = string.Empty;

            ShowSuccess("Карта очищена.");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка очистки: {ex.Message}");
        }
    }

    // ---------- Сохранение ----------

    public void SaveMap()
    {
        try
        {
            EnsureMap();

            // 1) Забираем введённые значения (Trim — чтобы JSON был аккуратным).
            //    Политика «сколько максимум» и «можно ли пустое» — в MapValidator.
            var name = nameInput != null ? nameInput.text?.Trim() : null;
            var desc = descriptionInput != null ? descriptionInput.text?.Trim() : null;
            _map.SetMetadata(name, desc);

            // 2) Валидация — включая метаданные.
            try
            {
                MapValidator.Validate(_map);
            }
            catch (MapValidationException ex)
            {
                ShowError(ex.Message);
                return;
            }

            // 3) Сохранение.
            string path = Path.Combine(Application.dataPath, ResolveFileName());
            try
            {
                _map.SaveToJson(path);
            }
            catch (Exception ex)
            {
                ShowError($"Не удалось записать файл: {ex.Message}");
                return;
            }

            ShowSuccess($"Карта сохранена: {path}");
        }
        catch (Exception ex)
        {
            // Страховка от любых неожиданных исключений — вылета быть не должно.
            ShowError($"Ошибка сохранения: {ex.Message}");
        }
    }

    // ---------- Загрузка ----------

    public void LoadMap()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, ResolveFileName());
            if (!File.Exists(path))
            {
                ShowError($"Файл не найден: {path}");
                return;
            }

            Map loaded;
            try
            {
                loaded = Map.LoadFromJson(path);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка чтения карты: {ex.Message}");
                return;
            }

            if (loaded == null)
            {
                ShowError("Не удалось загрузить карту (loaded == null).");
                return;
            }

            if (gridGenerator.Cells == null) gridGenerator.Generate();
            int sceneCols = gridGenerator.Cells.GetLength(0);
            int sceneRows = gridGenerator.Cells.GetLength(1);

            if (loaded.Rows != sceneRows || loaded.Cols != sceneCols)
            {
                ShowError($"Размер не совпадает: JSON {loaded.Rows}x{loaded.Cols}, сцена {sceneRows}x{sceneCols}");
                return;
            }

            if (fillPalette == null)
            {
                ShowError("fillPalette не назначена — нельзя восстановить спрайты.");
                return;
            }

            // Валидируем до применения, чтобы не портить текущее состояние.
            try
            {
                MapValidator.Validate(loaded);
            }
            catch (MapValidationException ex)
            {
                ShowError(ex.Message);
                return;
            }

            // --- Применение ---
            try
            {
                // Чистим сцену.
                for (int y = 0; y < sceneRows; y++)
                    for (int x = 0; x < sceneCols; x++)
                        gridGenerator.Cells[x, y]?.ClearAll();

                // Только теперь присваиваем _map.
                _map = loaded;

                // Заполняем визуал/логику из loaded.
                for (int y = 0; y < sceneRows; y++)
                {
                    for (int x = 0; x < sceneCols; x++)
                    {
                        var cell = gridGenerator.Cells[x, y];
                        if (cell == null) continue;

                        var container = loaded[y, x]; // индексатор безопаснее прямого доступа к Cells

                        foreach (var fill in container.Fills)
                        {
                            if (fill == null) continue;

                            var sprite = fillPalette.ResolveSprite(fill);
                            if (sprite == null)
                                Debug.LogWarning($"Нет спрайта для {fill.FillType?.Name} в ({y},{x})");

                            cell.AddFill(fill, sprite);
                        }
                    }
                }

                // Синхронизируем UI с загруженной картой.
                if (nameInput != null) nameInput.text = loaded.Name ?? string.Empty;
                if (descriptionInput != null) descriptionInput.text = loaded.Description ?? string.Empty;

                ShowSuccess($"Карта загружена: {path}");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка применения карты: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка загрузки: {ex.Message}");
        }
    }

    private string ResolveFileName()
        => string.IsNullOrEmpty(mapFileName) ? "map.json" : mapFileName;
}