using System;
using UnityEngine;

/// <summary>
/// Точка сборки игры: создаёт Map, PlayerState, GameContext,
/// TurnController и подписывается на запросы UI.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private TextAsset mapJson;
    [SerializeField] private int playerNum = 1;
    [SerializeField] private int startRow = 0;
    [SerializeField] private int startCol = 0;

    [SerializeField] private DragonChoiceUI dragonChoiceUI; // см. ниже
    [SerializeField] private DragonFightUI dragonFightUI;   // см. ниже
    [SerializeField] private HudController hud;

    public TurnController Turns { get; private set; }
    public GameContext Context { get; private set; }

    private void Awake()
    {
        Map map = Map.LoadFromJson(FileFromTextAsset(mapJson));

        var player = new PlayerState(playerNum, startRow, startCol);
        Context = new GameContext(map, player);
        Context.Logger = Debug.Log;

        Turns = new TurnController(map, Context);

        // Телепорт и река маршрутизируем во внутренние методы TurnController.
        Context.TeleportAction = (r, c) => TeleportViaTurns(r, c);
        Context.RiverCarryAction = (r, c, d) => RiverViaTurns(r, c, d);

        // UI дракона.
        Context.DragonChoiceHandler = req =>
            dragonChoiceUI.Show(req, giveKey => Context.OnDragonChoice(req.Row, req.Col, giveKey));

        Context.DragonFightHandler = req =>
            dragonFightUI.Show(req, fight => Context.OnDragonFight(req.Row, req.Col, fight));

        Context.OnTurnCompleted = () =>
        {
            hud.ShowTurnMessages(Context.Messages);          // полотно текста за ход
            hud.AppendHistory(Context.History[^1]);          // общая история
        };
    }

    private void TeleportViaTurns(int r, int c)
    {
        // Небольшая хитрость: TeleportAction вызывается из Portal.OnEnter,
        // а сам TurnController уже в ApplyEffects. Чтобы не рекурсировать
        // неправильно, телепорт применяем через рефлексию приватного метода
        // или добавляем публичный метод. Ниже — публичный метод в TurnController
        // (см. примечание).
        Turns.TeleportAndApply(r, c);
    }

    private void RiverViaTurns(int r, int c, Direction d)
    {
        Turns.RiverCarryAndApply(r, c, d);
    }

    private static string FileFromTextAsset(TextAsset ta)
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, ta.name + ".json");
        System.IO.File.WriteAllText(path, ta.text);
        return path;
    }
}