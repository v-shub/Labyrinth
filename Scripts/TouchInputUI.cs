using UnityEngine;
using UnityEngine.UI;

public class TouchInputUI : MonoBehaviour
{
    [SerializeField] private GameBootstrap game;
    [SerializeField] private Button upButton, downButton, leftButton, rightButton;
    [SerializeField] private Button diveButton;
    [SerializeField] private Button nextTurnButton;

    private void Awake()
    {
        upButton.onClick.AddListener(() => DoMove(Direction.Up));
        downButton.onClick.AddListener(() => DoMove(Direction.Down));
        leftButton.onClick.AddListener(() => DoMove(Direction.Left));
        rightButton.onClick.AddListener(() => DoMove(Direction.Right));
        diveButton.onClick.AddListener(() => DoDive());
        nextTurnButton.onClick.AddListener(() => game.Turns.BeginTurn());
    }

    private void DoMove(Direction dir)
    {
        if (game.Context.IsWon) return;
        game.Turns.Move(dir);
    }

    private void DoDive()
    {
        if (game.Context.IsWon) return;
        game.Turns.DiveIntoPortal();
    }
}