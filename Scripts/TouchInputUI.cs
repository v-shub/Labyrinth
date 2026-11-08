using UnityEngine;
using UnityEngine.UI;

public class TouchInputUI : MonoBehaviour
{
    [SerializeField] private TurnAnimator animator;
    [SerializeField] private GameBootstrap game;

    [Header("Skip-turn")]
    [Tooltip("Прозрачный Image на весь экран. Активен только когда SkipNextTurn.")]
    [SerializeField] private GameObject skipLayer;

    [SerializeField] private Button upButton, downButton, leftButton, rightButton;
    [SerializeField] private Button diveButton;

    private void Awake()
    {
        upButton.onClick.AddListener(() => DoMove(Direction.Up));
        downButton.onClick.AddListener(() => DoMove(Direction.Down));
        leftButton.onClick.AddListener(() => DoMove(Direction.Left));
        rightButton.onClick.AddListener(() => DoMove(Direction.Right));
        diveButton.onClick.AddListener(() => DoDive());
    }

    private void Update()
    {
        bool busy = (animator != null && animator.Busy) || game.Context.IsWon;
        bool skip = game.Context.Player.SkipNextTurn;
        bool blocked = busy || skip;

        upButton.interactable = !blocked;
        downButton.interactable = !blocked;
        leftButton.interactable = !blocked;
        rightButton.interactable = !blocked;
        diveButton.interactable = !blocked;

        if (skipLayer != null && skipLayer.activeSelf != skip)
            skipLayer.SetActive(skip);
    }

    // SkipLayer вызывает это.
    public void DoSkipTurn()
    {
        if (animator == null || animator.Busy) return;
        if (!game.Context.Player.SkipNextTurn) return;
        animator.RequestBeginTurn();
    }

    private void DoMove(Direction dir)
    {
        if (animator == null || animator.Busy || game.Context.IsWon) return;
        if (TryConsumeSkipTurn()) return;
        animator.RequestMove(dir);
    }

    private void DoDive()
    {
        if (animator == null || animator.Busy || game.Context.IsWon) return;
        if (TryConsumeSkipTurn()) return;
        animator.RequestDive();
    }

    private bool TryConsumeSkipTurn()
    {
        if (!game.Context.Player.SkipNextTurn) return false;
        animator.RequestBeginTurn();
        return true;
    }
}