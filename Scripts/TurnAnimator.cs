using System.Collections;
using UnityEngine;

public class TurnAnimator : MonoBehaviour
{
    [SerializeField] private GameBootstrap game;
    [SerializeField] private GameCell cellView;

    [Header("Тайминги (сек)")]
    [SerializeField] private float pressDuration = 0.10f;
    [SerializeField] private float rubberRevealDuration = 0.60f;
    [SerializeField] private float arrivalDuration = 0.10f;

    private bool _busy;
    public bool Busy => _busy;

    private void Start()
    {
        // Первичная отрисовка при старте сцены (если GameBootstrap еще не сделал этого)
        RefreshCellOnly();
    }

    public void RequestMove(Direction dir)
    {
        if (_busy || game.Context.IsWon) return;
        StartCoroutine(MoveRoutine(dir));
    }

    public void RequestDive()
    {
        if (_busy || game.Context.IsWon) return;
        StartCoroutine(DiveRoutine());
    }

    public void RequestBeginTurn()
    {
        if (_busy || game.Context.IsWon) return;
        StartCoroutine(BeginTurnRoutine());
    }

    private IEnumerator MoveRoutine(Direction dir)
    {
        _busy = true;

        // 1. Если выходим из резинки удачно — показываем её на время анимации.
        if (WillExitRubber(dir))
        {
            var p = game.Context.Player;
            cellView.Render(CurrentCell(), game.Context, p.Row, p.Col,
                            revealRubberOnExit: true);
            yield return new WaitForSeconds(rubberRevealDuration);
        }

        // 2. Фаза нажатия (анимация).
        yield return PlayPressFeedback();

        // 3. Логика хода. Внутри Move сработает OnTurnCompleted, который перерисует клетку и текст.
        game.Turns.Move(dir);

        // 4. Фаза прибытия (анимация).
        yield return PlayArrivalFeedback();
        _busy = false;
    }

    private IEnumerator DiveRoutine()
    {
        _busy = true;
        yield return PlayPressFeedback();

        game.Turns.DiveIntoPortal();

        yield return PlayArrivalFeedback();
        _busy = false;
    }

    private IEnumerator BeginTurnRoutine()
    {
        _busy = true;
        yield return PlayPressFeedback();

        game.Turns.BeginTurn();

        _busy = false;
    }

    private IEnumerator PlayPressFeedback()
    {
        if (pressDuration > 0f) yield return new WaitForSeconds(pressDuration);
        else yield return null;
    }

    private IEnumerator PlayArrivalFeedback()
    {
        if (arrivalDuration > 0f) yield return new WaitForSeconds(arrivalDuration);
        else yield return null;
    }

    private CellContainer CurrentCell()
        => game.Context.Map[game.Context.Player.Row, game.Context.Player.Col];

    private bool WillExitRubber(Direction dir)
    {
        var p = game.Context.Player;
        return p.OnRubber && p.RubberAllowsMove(dir);
    }

    // Метод только для перерисовки клетки, без текста.
    private void RefreshCellOnly()
    {
        var p = game.Context.Player;
        cellView.Render(CurrentCell(), game.Context, p.Row, p.Col,
                        revealRubberOnExit: false);
    }

    public void ForceRedraw(CellContainer cell, GameContext ctx)
    {
        cellView.Render(cell, ctx, ctx.Player.Row, ctx.Player.Col,
                        revealRubberOnExit: false);
    }
}