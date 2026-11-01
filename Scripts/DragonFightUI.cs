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

    private void Awake()
    {
        panel.SetActive(false);
        fightButton.onClick.AddListener(() => Confirm(true));
        skipButton.onClick.AddListener(() => Confirm(false));
    }

    public void Show(DragonFightRequest req, Action<bool> callback)
    {
        _callback = callback;
        panel.SetActive(true);
    }

    private void Confirm(bool fight)
    {
        panel.SetActive(false);
        var cb = _callback;
        _callback = null;
        cb?.Invoke(fight);
    }
}