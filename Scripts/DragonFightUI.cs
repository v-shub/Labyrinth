using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI выбора: сражаться ли с драконом за ключ.
/// </summary>
public class DragonFightUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button fightButton;
    [SerializeField] private Button skipButton;

    private Action<bool> _callback;
    private bool _initialized;

    private void Awake() => Init();

    private void Init()
    {
        if (_initialized) return;
        _initialized = true;

        if (panel != null) panel.SetActive(false);

        if (fightButton != null)
        {
            fightButton.onClick.RemoveAllListeners();
            fightButton.onClick.AddListener(() => Confirm(true));
        }
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => Confirm(false));
        }
    }

    public void Show(DragonFightRequest req, Action<bool> callback)
    {
        Init();

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panel != null) panel.SetActive(true);

        _callback = callback;
    }

    private void Confirm(bool fight)
    {
        if (panel != null) panel.SetActive(false);
        var cb = _callback;
        _callback = null;
        cb?.Invoke(fight);
    }
}