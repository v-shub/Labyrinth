using System.Linq;

/// <summary>
/// Единственное место, где задана последовательность хода:
/// начало хода → проверки (резинка, стена, река) → перемещение → эффекты → FinishTurn.
/// </summary>
public class TurnController
{
    private readonly Map _map;
    private readonly GameContext _ctx;

    private bool _firstTurnDone;

    /// <summary>Порядок активации «не-специальных» эффектов на клетке.</summary>
    private static readonly System.Type[] EffectOrder =
    {
        typeof(Key), typeof(Sword), typeof(Exit),     // предметы
        typeof(Portal),                                // портал (приоритет над рекой)
        typeof(River),                                 // река
        typeof(Dragon), typeof(RubberRoom),            // остальные
    };

    public TurnController(Map map, GameContext ctx)
    {
        _map = map;
        _ctx = ctx;

        // Мосты из GameContext обратно в TurnController.
        _ctx.TeleportAction = TeleportAndApply;
        _ctx.RiverCarryAction = RiverCarryAndApply;
    }

    // ============================================================
    //  Публичный API хода
    // ============================================================

    /// <summary>
    /// Старт игры: сразу применяем эффекты стартовой клетки, не дожидаясь
    /// нажатия кнопки движения. После этого игрок может делать обычные ходы.
    /// </summary>
    public void StartGame()
    {
        if (_ctx.IsWon) return;

        _ctx.Messages.Clear();

        int startRow = _ctx.Player.Row;
        int startCol = _ctx.Player.Col;

        // Приветственное сообщение о стартовой клетке.
        string cellName = ColumnLabelUtil.GetCellName(startRow, startCol);
        _ctx.AddMessage($"Вы приземлились на клетку {cellName}.");

        // Запоминаем количество сообщений после приветствия — чтобы понять,
        // добавили ли эффекты стартовой клетки хоть что-то.
        int before = _ctx.Messages.Count;

        // Применяем эффекты стартовой клетки (один раз за игру).
        // suppressEmptyMessage: true — «Вы прошли.» при необходимости добавим сами,
        // чтобы не задвоить и не вывести его при телепорте/реке.
        _firstTurnDone = true;
        ApplyEffects(startRow, startCol, Arrival.Start, enteredFrom: null,
                     suppressEmptyMessage: true);

        // Если эффекты стартовой клетки не добавили ни одного сообщения —
        // на клетке ничего нет, выводим «Вы прошли.».
        if (_ctx.Messages.Count == before)
            _ctx.AddMessage("Вы прошли.");

        FinishTurn();
    }
    /// <summary>
    /// Потратить ход на пропуск (болото). Вызывается из UI, когда SkipNextTurn == true.
    /// Если игрок обязан двигаться (MustMoveAgain) — пропуск запрещён.
    /// </summary>
    public void BeginTurn()
    {
        if (_ctx.IsWon) return;

        _ctx.Messages.Clear();

        if (_ctx.Player.MustMoveAgain)
        {
            _ctx.AddMessage("Вы должны сделать ход.");
            FinishTurn();
            return;
        }

        switch (_ctx.SwampPhase)
        {
            case SwampPhase.None:
                // Нечего пропускать — просто завершаем ход.
                FinishTurn();
                return;

            case SwampPhase.SkipPending:
                // Ход 2: показываем пропуск. SwampPhase остаётся SkipPending —
                // болото видно, кнопки заблокированы. Переходим в AwaitingEffects.
                _ctx.SwampPhase = SwampPhase.AwaitingEffects;
                _ctx.AddMessage("Вы пропускаете этот ход.");
                FinishTurn();
                return;

            case SwampPhase.AwaitingEffects:
                // Ход 3: выходим из болота и применяем отложенные эффекты.
                // Игрок не двигается по желанию — только эффекты.
                _ctx.SwampPhase = SwampPhase.None;
                _ctx.AddMessage("Вы выбрались из болота.");

                if (_ctx.DelayedCell.HasValue)
                {
                    var (r, c, arr, from) = _ctx.DelayedCell.Value;
                    _ctx.DelayedCell = null;
                    ApplyOtherEffects(r, c, arr, from, suppressEmptyMessage: true);
                }
                else
                {
                    _ctx.AddMessage("Вы прошли.");
                }

                FinishTurn();
                return;
        }
    }

    /// <summary>Ход игрока в направлении.</summary>
    public void Move(Direction dir)
    {
        if (_ctx.IsWon) return;

        _ctx.Messages.Clear();

        // Начало хода: защита от пропуска, эффекты стартовой клетки (один раз),
        // отложенные эффекты болота. Если начало хода само завершило ход — выходим.
        if (HandleTurnStart()) return;

        _ctx.AddMessage($"Вы выбрали пойти {DirectionUtil.ToRussian(dir)}.");

        // 1. Резинка.
        if (!_ctx.Player.RubberAllowsMove(dir))
        {
            _ctx.AddMessage("Вы прошли."); // маскировка
            FinishTurn();
            return;
        }

        // 2. Стена (до реки — нельзя «выйти из реки» в стену).
        var (dr, dc) = DirectionUtil.Offset(dir);
        int nr = _ctx.Player.Row + dr;
        int nc = _ctx.Player.Col + dc;

        if (nr < 0 || nr >= _map.Rows || nc < 0 || nc >= _map.Cols)
        {
            _ctx.AddMessage("Там стена.");
            FinishTurn();
            return;
        }

        // 3. Река на текущей клетке: против течения / по течению / выход.
        var currentRiver = _map[_ctx.Player.Row, _ctx.Player.Col].Fills.OfType<River>().FirstOrDefault();
        if (currentRiver != null)
        {
            if (dir == currentRiver.FlowIn)
            {
                _ctx.AddMessage("Вы не можете пойти против течения.");
                FinishTurn();
                return;
            }
            if (dir == currentRiver.FlowOut)
                _ctx.AddMessage("Вы пошли по течению.");
            else
                _ctx.AddMessage("Вы вышли из реки.");
        }

        // 4. Уходим с резинки — только когда ход точно состоится.
        if (_ctx.Player.OnRubber)
        {
            _ctx.Player.LeaveRubber();
            _ctx.AddMessage("Вы вышли из резинки.");
        }

        // 5. Перемещение и эффекты.
        _ctx.Player.Row = nr;
        _ctx.Player.Col = nc;
        ApplyEffects(nr, nc, Arrival.FromMove, enteredFrom: dir);

        FinishTurn();
    }

    /// <summary>Особое действие «нырок в портал» (только на клетке портала).</summary>
    public void DiveIntoPortal()
    {
        if (_ctx.IsWon) return;

        _ctx.Messages.Clear();

        if (HandleTurnStart()) return;

        _ctx.AddMessage("Вы выбрали нырнуть в портал.");

        var portal = _map[_ctx.Player.Row, _ctx.Player.Col].Fills.OfType<Portal>().FirstOrDefault();
        if (portal == null)
        {
            _ctx.AddMessage("Здесь нет портала.");
            FinishTurn();
            return;
        }

        var next = PortalNavigator.Next(_map, portal.Num);
        if (next == null)
        {
            _ctx.AddMessage("Портал не работает.");
            FinishTurn();
            return;
        }

        _ctx.Player.LeaveRubber();
        _ctx.AddMessage("Вы попали в портал. Вы вышли из портала.");
        TeleportAndApply(next.Value.row, next.Value.col);

        FinishTurn();
    }

    // ============================================================
    //  Мосты телепорта/реки
    // ============================================================

    public void TeleportAndApply(int row, int col)
    {
        _ctx.Player.Row = row;
        _ctx.Player.Col = col;
        ApplyEffects(row, col, Arrival.FromPortal, enteredFrom: null);
    }

    public void RiverCarryAndApply(int row, int col, Direction flowOut)
    {
        var (dr, dc) = DirectionUtil.Offset(flowOut);
        int nr = row + dr;
        int nc = col + dc;

        if (nr < 0 || nr >= _map.Rows || nc < 0 || nc >= _map.Cols) return;

        _ctx.Player.Row = nr;
        _ctx.Player.Col = nc;
        // Сообщение «Вас смыло по течению.» уже добавлено в River.OnEnter — здесь не дублируем.
        ApplyEffects(nr, nc, Arrival.FromRiver, enteredFrom: DirectionUtil.Opposite(flowOut));
    }

    // ============================================================
    //  Внутреннее
    // ============================================================

    /// <summary>
    /// Начало хода: защита от пропуска, эффекты стартовой клетки (один раз за игру),
    /// отложенные эффекты болота. Возвращает true, если ход уже завершён
    /// (пропуск, победа или эффекты переместили игрока).
    /// </summary>
    private bool HandleTurnStart()
    {
        // «Обязан двигаться» действует только на текущий ход — сбрасываем в начале.
        // (Дракон на новой клетке может снова установить флаг в этом же ходу.)
        _ctx.Player.MustMoveAgain = false;

        // Защита: если UI не перехватил пропуск — обрабатываем здесь.
        // Защита: если UI не перехватил пропуск — обрабатываем здесь.
        if (_ctx.SwampPhase != SwampPhase.None)
        {
            BeginTurn();
            return true;
        }

        int startRow = _ctx.Player.Row;
        int startCol = _ctx.Player.Col;

        if (!_firstTurnDone)
        {
            // Первый ход: применяем эффекты стартовой клетки (все, кроме резинки —
            // резинка сама игнорирует Arrival.Start).
            _firstTurnDone = true;
            ApplyEffects(startRow, startCol, Arrival.Start, enteredFrom: null,
                         suppressEmptyMessage: true);
        }
        else if (_ctx.DelayedCell.HasValue)
        {
            // Отложенные эффекты болота срабатывают на ходе после пропущенного.
            var (r, c, arr, from) = _ctx.DelayedCell.Value;
            _ctx.DelayedCell = null;
            ApplyOtherEffects(r, c, arr, from, suppressEmptyMessage: true);
        }

        if (_ctx.IsWon)
        {
            FinishTurn();
            return true;
        }

        // Эффекты начала хода переместили игрока — действие игрока отменяется.
        if (_ctx.Player.Row != startRow || _ctx.Player.Col != startCol)
        {
            FinishTurn();
            return true;
        }

        return false;
    }

    private void FinishTurn()
    {
        _ctx.OnTurnCompleted?.Invoke();   // UI рисует: Fills + PickedUpThisTurn

        // После отрисовки — убираем «призраки» подобранных предметов.
        _ctx.PickedUpThisTurn.Clear();

        _ctx.FlushTurnMessages();
    }

    private void ApplyEffects(int row, int col, Arrival arrival, Direction? enteredFrom,
                              bool suppressEmptyMessage = false)
    {
        _ctx.LastMoveDirection = enteredFrom;
        var fills = _map[row, col].Fills.ToList();

        // Swamp — особый: ставит флаг и откладывает остальные эффекты.
        var swamp = fills.OfType<Swamp>().FirstOrDefault();
        if (swamp != null)
        {
            swamp.OnEnter(_ctx, row, col, arrival);
            if (fills.Any(f => !(f is Swamp)))
                _ctx.DelayedCell = (row, col, arrival, enteredFrom);
            return;
        }

        ApplyOtherEffects(row, col, arrival, enteredFrom, suppressEmptyMessage);
    }

    private void ApplyOtherEffects(int row, int col, Arrival arrival, Direction? enteredFrom,
                                   bool suppressEmptyMessage = false)
    {
        _ctx.LastMoveDirection = enteredFrom;
        var fills = _map[row, col].Fills.ToList();

        int before = _ctx.Messages.Count;

        // Эффекты в порядке EffectOrder, по одному экземпляру каждого типа.
        // Портал и река — эксклюзивны по порядку (после предметов).
        foreach (var type in EffectOrder)
        {
            foreach (var fill in fills)
            {
                if (fill.GetType() != type) continue;
                fill.OnEnter(_ctx, row, col, arrival);
                if (_ctx.IsWon) return;
            }

            if (type == typeof(Portal)
                && fills.OfType<Portal>().Any()
                && arrival != Arrival.FromPortal)
                return;

            if (type == typeof(River) && fills.OfType<River>().Any())
                return;
        }

        // «Невидимые» эффекты (чужой ключ, выход без ключа) не добавили сообщения —
        // значит, для игрока клетка выглядит пустой.
        if (!suppressEmptyMessage && _ctx.Messages.Count == before)
            _ctx.AddMessage("Вы прошли.");
    }
}