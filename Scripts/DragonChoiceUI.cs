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
    private bool _initialized;

    private void Awake() => Init();

    private void Init()
    {
        if (_initialized) return;
        _initialized = true;

        if (panel != null) panel.SetActive(false);

        if (keyButton != null)
        {
            keyButton.onClick.RemoveAllListeners();
            keyButton.onClick.AddListener(() => Confirm(true));
        }
        if (swordButton != null)
        {
            swordButton.onClick.RemoveAllListeners();
            swordButton.onClick.AddListener(() => Confirm(false));
        }
    }

    public void Show(DragonChoiceRequest req, Action<bool> callback)
    {
        // На случай, если Awake не выполнился (объект был выключен в сцене).
        Init();

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panel != null) panel.SetActive(true);

        _callback = callback;
    }

    private void Confirm(bool giveKey)
    {
        if (panel != null) panel.SetActive(false);
        var cb = _callback;
        _callback = null;
        cb?.Invoke(giveKey);
    }
}