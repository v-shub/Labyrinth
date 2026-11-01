using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private Text turnMessagesText;
    [SerializeField] private Text historyText;
    [SerializeField] private ScrollRect historyScroll;

    public void ShowTurnMessages(List<string> msgs)
        => turnMessagesText.text = string.Join(" ", msgs);

    public void AppendHistory(string entry)
    {
        historyText.text += (historyText.text.Length > 0 ? "\n" : "") + entry;
        Canvas.ForceUpdateCanvases();
        historyScroll.verticalNormalizedPosition = 0f;
    }
}