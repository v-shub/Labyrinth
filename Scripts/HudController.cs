using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private TMP_Text turnMessagesText;
    [SerializeField] private TMP_Text historyText;
    [SerializeField] private ScrollRect historyScroll;

    public void ShowTurnMessages(List<string> msgs)
    {
        turnMessagesText.text = string.Join(" ", msgs);
    }

    public void AppendHistory(string entry)
    {
        historyText.text += (historyText.text.Length > 0 ? "\n" : "") + entry;
        Canvas.ForceUpdateCanvases();
        historyScroll.verticalNormalizedPosition = 0f;
    }
}