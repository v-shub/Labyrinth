using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

[Serializable]
public class FillCountBinding
{
    [Tooltip("Имя из FillTypeRegistry: River, Key, Exit, Portal, Swamp, Dragon, Sword")]
    public string FillTypeName;
    public TMP_Text Target;
}

public class MapInfo : MonoBehaviour
{
    [Header("Источник карты")]
    [Tooltip("Имя файла в Application.dataPath. Пусто — map.json.")]
    [SerializeField] private string mapFileName = "map.json";

    [Header("Метаданные карты")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Счётчики по типам")]
    [SerializeField] private List<FillCountBinding> bindings = new();

    private void Start()
    {
        LoadAndShow();
    }

    public void LoadAndShow()
    {
        try
        {
            string path = Path.Combine(Application.dataPath,
                string.IsNullOrEmpty(mapFileName) ? "map.json" : mapFileName);

            if (!File.Exists(path))
            {
                Debug.LogError($"[MapStatsView] Файл не найден: {path}");
                return;
            }

            var map = Map.LoadFromJson(path);
            Show(map);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MapStatsView] Ошибка загрузки карты: {ex.Message}");
        }
    }

    public void Show(Map map)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));

        if (nameText) nameText.text = map.Name ?? string.Empty;
        if (descriptionText) descriptionText.text = map.Description ?? string.Empty;

        var counts = MapFillCounter.Count(map);

        foreach (var b in bindings)
        {
            if (b?.Target == null) continue;

            if (!string.IsNullOrEmpty(b.FillTypeName) && counts.TryGetValue(b.FillTypeName, out int n))
                b.Target.text = n.ToString();
            else
                b.Target.text = "0";
        }
    }
}