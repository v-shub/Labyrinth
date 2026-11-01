using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI выбора: ключ или меч отдать дракону.
/// </summary>
public class DragonChoiceUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button keyButton;
    [SerializeField] private Button swordButton;

    private Action<bool> _callback; // true = отдать ключ

    private void Awake()
    {
        panel.SetActive(false);
        keyButton.onClick.AddListener(() => Confirm(true));
        swordButton.onClick.AddListener(() => Confirm(false));
    }

    public void Show(DragonChoiceRequest req, Action<bool> callback)
    {
        _callback = callback;
        panel.SetActive(true);
        // Можно настроить тексты кнопок в зависимости от req.HasKey/HasSword.
    }

    private void Confirm(bool giveKey)
    {
        panel.SetActive(false);
        var cb = _callback;
        _callback = null;
        cb?.Invoke(giveKey);
    }
}