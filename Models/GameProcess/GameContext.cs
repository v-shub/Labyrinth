using System;
using System.Collections.Generic;
using System.Linq;

public class GameContext
{
    public Map Map { get; }
    public PlayerState Player { get; }

    /// <summary>Направление последнего хода (для сообщений реки и резинки).</summary>
    public Direction? LastMoveDirection { get; set; }

    /// <summary>
    /// Состояние болота. Заменяет прежний PlayerState.SkipNextTurn и SwampSkipShown.
    /// None — не в болоте; SkipPending — ждём Ход 2; AwaitingEffects — ждём Ход 3.
    /// </summary>
    public SwampPhase SwampPhase { get; set; } = SwampPhase.None;
    /// <summary>
    /// Отложенные эффекты Swamp: клетка, контекст входа (arrival + направление)
    /// для обработки на ходе после пропущенного.
    /// </summary>
    public (int row, int col, Arrival arrival, Direction? enteredFrom)? DelayedCell { get; set; }
    /// <summary>
    /// Предметы, подобранные в текущем ходу: (row, col, fill). Нужны для отрисовки:
    /// из CellContainer.Fills они уже удалены, но игрок должен увидеть, что подобрал.
    /// Очищается в TurnController.FinishTurn после отрисовки.
    /// </summary>
    public List<(int row, int col, ICellFill fill)> PickedUpThisTurn { get; } = new();

    // ----- Сообщения -----
    public List<string> Messages { get; } = new();   // текущий ход
    public List<string> History { get; } = new();    // вся история

    public void AddMessage(string msg) => Messages.Add(msg);

    public void FlushTurnMessages()
    {
        if (Messages.Count == 0) return;
        History.Add(string.Join(" ", Messages));
        Messages.Clear();
    }

    // ----- События для UI -----
    public Action<string> Logger { get; set; } = _ => { };
    public void Log(string msg) => Logger(msg);

    public Action OnTurnCompleted { get; set; }
    public Action OnWin { get; set; }

    public bool IsWon { get; private set; }
    public void Win()
    {
        if (IsWon || IsLost) return;
        IsWon = true;
        OnWin?.Invoke();
    }

    // ----- Поражение -----
    public Action OnLose { get; set; }
    public bool IsLost { get; private set; }

    public void Lose()
    {
        if (IsLost || IsWon) return;
        IsLost = true;
        AddMessage("Вы проиграли! Ваш ключ у дракона, а мечей больше не осталось.");
        OnLose?.Invoke();
    }

    /// <summary>
    /// Поражение: ключ игрока лежит у какого-то дракона,
    /// и при этом ни у игрока, ни на карте нет ни одного меча.
    /// </summary>
    public bool CheckDefeat()
    {
        if (IsWon || IsLost) return false;
        if (Player.HasKey) return false;                            // ключ у игрока — не поражение
        if (!DragonStorage.HoldsKeyOf(Map, Player.PlayerNum)) return false;
        if (Player.HasSword) return false;

        for (int r = 0; r < Map.Rows; r++)
            for (int c = 0; c < Map.Cols; c++)
                if (Map[r, c].Fills.OfType<Sword>().Any())
                    return false;

        return true;
    }

    // ----- Мосты к TurnController (телепорт/река) -----
    public Action<int, int> TeleportAction { get; set; }
    public Action<int, int, Direction> RiverCarryAction { get; set; }

    public void TeleportTo(int row, int col)
    {
        if (TeleportAction == null)
            throw new InvalidOperationException("TeleportAction не задан");
        TeleportAction(row, col);
    }

    public void RiverCarry(int row, int col, Direction flowOut)
    {
        if (RiverCarryAction == null)
            throw new InvalidOperationException("RiverCarryAction не задан");
        RiverCarryAction(row, col, flowOut);
    }

    // ----- Дракон -----

    /// <summary>Ожидается асинхронный ответ UI по дракону (выбор/бой).</summary>
    public bool PendingDragonChoice { get; set; }

    /// <summary>«Бежать» уже добавлено в этом эпизоде дракона (защита от дубля).</summary>
    private bool _dragonFleeMessageAdded;

    /// <summary>
    /// Завершает эпизод дракона: гарантированно один раз добавляет
    /// «Вы должны бежать от дракона.» и ставит обязательный доп. ход.
    /// Вызывать последним — после всех сообщений дракона и результата выбора.
    /// </summary>
    public void FinishDragonEncounter()
    {
        Player.MustMoveAgain = true;
        if (_dragonFleeMessageAdded) return;
        _dragonFleeMessageAdded = true;
        AddMessage("Вы должны бежать от дракона.");
    }

    /// <summary>Сброс эпизода дракона (в начале нового эпизода / нового хода с драконом).</summary>
    public void ResetDragonEncounter()
    {
        _dragonFleeMessageAdded = false;
        PendingDragonChoice = false;
    }

    /// <summary>
    /// Вызывается после того, как игрок ответил в UI дракона (выбор/бой).
    /// Показывает сообщения и завершает «микро-ход» — чтобы UI отрисовал результат.
    /// </summary>
    public Action OnDragonResolved { get; set; }

    private void ResolveDragonChoice()
    {
        OnDragonResolved?.Invoke();
        FlushTurnMessages();
    }

    public void OnDragonChoice(int row, int col, bool giveKey)
    {
        Messages.Clear();

        if (giveKey)
        {
            Player.RemoveKey();
            DragonStorage.TakeKey(Map, row, col, Player.PlayerNum);
            AddMessage("Вы оставили дракону ключ.");
        }
        else
        {
            Player.RemoveSword();
            AddMessage("Вы оставили дракону меч.");
        }

        PendingDragonChoice = false;
        FinishDragonEncounter();   // «бежать» — последним, один раз
        ResolveDragonChoice();
    }

    public void OnDragonFight(int row, int col, bool fight)
    {
        Messages.Clear();

        if (fight)
        {
            Player.RemoveSword();
            DragonStorage.ReleaseKey(Map, row, col);
            Player.AddKey();
            AddMessage("Вы сразились с драконом и забрали свой ключ.");
        }
        else
        {
            AddMessage("Вы решили не сражаться с драконом.");
        }

        PendingDragonChoice = false;
        FinishDragonEncounter();   // «бежать» — последним, один раз
        ResolveDragonChoice();
    }

    // ----- Дракон: запросы UI -----
    public Action<DragonChoiceRequest> DragonChoiceHandler { get; set; }
    public Action<DragonFightRequest> DragonFightHandler { get; set; }
    public void RequestDragonChoice(int row, int col)
    {
        var req = new DragonChoiceRequest(row, col, Player.HasKey, Player.HasSword);
        PendingDragonChoice = true;
        if (DragonChoiceHandler == null)
        {
            UnityEngine.Debug.LogWarning("[Dragon] DragonChoiceHandler не задан — fallback: отдать ключ.");
            OnDragonChoice(row, col, giveKey: true);
            return;
        }
        DragonChoiceHandler(req);
    }

    public void RequestDragonFight(int row, int col)
    {
        var req = new DragonFightRequest(row, col);
        PendingDragonChoice = true;
        if (DragonFightHandler == null)
        {
            UnityEngine.Debug.LogWarning("[Dragon] DragonFightHandler не задан — fallback: сражаться.");
            OnDragonFight(row, col, fight: true);
            return;
        }
        DragonFightHandler(req);
    }

    public GameContext(Map map, PlayerState player)
    {
        Map = map;
        Player = player;
    }
}

public class DragonChoiceRequest
{
    public int Row { get; }
    public int Col { get; }
    public bool HasKey { get; }
    public bool HasSword { get; }
    public DragonChoiceRequest(int row, int col, bool hasKey, bool hasSword)
    { Row = row; Col = col; HasKey = hasKey; HasSword = hasSword; }
}

public class DragonFightRequest
{
    public int Row { get; }
    public int Col { get; }
    public DragonFightRequest(int row, int col) { Row = row; Col = col; }
}