using System;
using System.Collections.Generic;

public class GameContext
{
    public Map Map { get; }
    public PlayerState Player { get; }

    /// <summary>Направление последнего хода (для сообщений реки и резинки).</summary>
    public Direction? LastMoveDirection { get; set; }

    /// <summary>Отложенные эффекты Swamp: клетка для обработки на ходе после пропущенного.</summary>
    public (int row, int col)? DelayedCell { get; set; }

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
        if (IsWon) return;
        IsWon = true;
        OnWin?.Invoke();
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

    // ----- Дракон: запросы UI -----
    public Action<DragonChoiceRequest> DragonChoiceHandler { get; set; }
    public Action<DragonFightRequest> DragonFightHandler { get; set; }

    public void RequestDragonChoice(int row, int col)
    {
        var req = new DragonChoiceRequest(row, col, Player.HasKey, Player.HasSword);
        if (DragonChoiceHandler == null)
        {
            OnDragonChoice(row, col, giveKey: true); // fallback
            return;
        }
        DragonChoiceHandler(req);
    }

    public void OnDragonChoice(int row, int col, bool giveKey)
    {
        if (giveKey)
        {
            Player.RemoveKey();
            DragonStorage.TakeKey(Map, row, col, Player.PlayerNum);
            AddMessage("Вы отдали ключ дракону.");
        }
        else
        {
            Player.RemoveSword();
            AddMessage("Вы отдали меч дракону.");
        }
        Player.MustMoveAgain = true;
    }

    public void RequestDragonFight(int row, int col)
    {
        var req = new DragonFightRequest(row, col);
        if (DragonFightHandler == null)
        {
            OnDragonFight(row, col, fight: true);
            return;
        }
        DragonFightHandler(req);
    }

    public void OnDragonFight(int row, int col, bool fight)
    {
        if (fight)
        {
            Player.RemoveSword();
            DragonStorage.ReleaseKey(Map, row, col);
            Player.AddKey();
            AddMessage("Вы сразились с драконом и забрали ключ. Меч сломан.");
        }
        Player.MustMoveAgain = true;
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