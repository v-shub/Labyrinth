using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Показывает сообщения об успехе/ошибке на сцене.
/// Успех — зелёным, ошибка — красным.
/// </summary>
public class MessageView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Текст, в который выводится сообщение. Если пусто — берётся TMP_Text с этого же объекта.")]
    [SerializeField] private TMP_Text label;

    [Header("Длительность показа (сек)")]
    [SerializeField] private float successDuration = 2f;
    [SerializeField] private float errorDuration = 6f;

    [Header("Цвета")]
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color errorColor = new Color(0.9f, 0.2f, 0.2f);

    private Coroutine _hideRoutine;

    void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (label != null) label.text = string.Empty;
    }

    public void ShowSuccess(string message) => Show(message, successColor, successDuration);

    public void ShowError(string message) => Show(message, errorColor, errorDuration);

    private void Show(string message, Color color, float duration)
    {
        if (label == null)
        {
            Debug.Log(message);
            return;
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        label.text = message;
        label.color = color;
        label.gameObject.SetActive(true);

        _hideRoutine = StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (label != null) label.text = string.Empty;
        _hideRoutine = null;
    }
}