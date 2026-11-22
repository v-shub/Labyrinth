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

    [Header("End game")]
    [Tooltip("Задержка перед выходом из сцены после победы/поражения, сек.")]
    [SerializeField] private float endGameDelay = 3f;
    [Tooltip("Загрузчик сцены для выхода после победы/поражения.")]
    [SerializeField] private SceneLoader sceneLoader;

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

        // Общий обработчик завершения хода и разрешения выбора дракона.
        System.Action renderAndHistory = () =>
        {
            hud.ShowTurnMessages(Context.Messages);

            var currentCell = Map[Player.Row, Player.Col];
            var animator = FindFirstObjectByType<TurnAnimator>();
            if (animator != null)
                animator.ForceRedraw(currentCell, Context);

            if (Context.Messages.Count > 0)
                hud.AppendHistory(string.Join(" ", Context.Messages));
        };

        Context.OnTurnCompleted = renderAndHistory;
        Context.OnDragonResolved = renderAndHistory;

        // Победа: сообщение добавляется после «Вы нашли выход.»,
        // затем через задержку — выход из сцены.
        Context.OnWin = () =>
        {
            Context.AddMessage("Вы победили!");
            StartCoroutine(ExitAfterDelay());
        };

        // Поражение: сообщение уже добавлено в GameContext.Lose(),
        // здесь только запускаем задержку выхода из сцены.
        Context.OnLose = () =>
        {
            StartCoroutine(ExitAfterDelay());
        };

        // Старт игры: сразу применяем эффекты стартовой клетки.
        Turns.StartGame();
    }

    private System.Collections.IEnumerator ExitAfterDelay()
    {
        yield return new WaitForSeconds(endGameDelay);

        if (sceneLoader != null)
            sceneLoader.LoadScene();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private static string FileFromTextAsset(TextAsset ta)
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, ta.name + ".json");
        System.IO.File.WriteAllText(path, ta.text);
        return path;
    }
}