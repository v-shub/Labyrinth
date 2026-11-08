using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Точка сборки игры: создаёт Map, PlayerState, GameContext,
/// TurnController и подписывается на запросы UI.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private TextAsset mapJson;

    [SerializeField] private DragonChoiceUI dragonChoiceUI;
    [SerializeField] private DragonFightUI dragonFightUI;
    [SerializeField] private HudController hud;

    public TurnController Turns { get; private set; }
    public GameContext Context { get; private set; }

    public Map Map => Context?.Map;
    public PlayerState Player => Context?.Player;

    private void Awake()
    {
        Map map = Map.LoadFromJson(FileFromTextAsset(mapJson));

        GameLaunchArgs.Consume(out int pNum, out int sRow, out int sCol);
        var player = new PlayerState(pNum, sRow, sCol);

        Context = new GameContext(map, player);
        Context.Logger = Debug.Log;

        Turns = new TurnController(map, Context);

        Context.DragonChoiceHandler = req =>
            dragonChoiceUI.Show(req, giveKey => Context.OnDragonChoice(req.Row, req.Col, giveKey));

        Context.DragonFightHandler = req =>
            dragonFightUI.Show(req, fight => Context.OnDragonFight(req.Row, req.Col, fight));

        // OnTurnCompleted вызывается ДО FlushTurnMessages, поэтому:
        //   * Context.Messages  — сообщения текущего хода (ещё не очищены);
        //   * Context.History   — уже завершённые ходы (текущего там ещё нет).
        Context.OnTurnCompleted = () =>
        {
            // 1. Текст текущего хода.
            hud.ShowTurnMessages(Context.Messages);

            // 2. Визуал клетки.
            var currentCell = Map[Player.Row, Player.Col];
            var animator = FindFirstObjectByType<TurnAnimator>();
            if (animator != null)
                animator.ForceRedraw(currentCell, Context);

            // 3. История: текущий ход ещё не в History — добавляем его вручную
            //    ровно так же, как это сделает FlushTurnMessages.
            if (Context.Messages.Count > 0)
                hud.AppendHistory(string.Join(" ", Context.Messages));
        };
    }

    private static string FileFromTextAsset(TextAsset ta)
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, ta.name + ".json");
        System.IO.File.WriteAllText(path, ta.text);
        return path;
    }
}