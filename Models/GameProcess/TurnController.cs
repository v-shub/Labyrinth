using System.Linq;

/// <summary>
/// Единственное место, где задана последовательность хода:
/// ввод → проверки (резинка, стена, река) → перемещение → эффекты → FinishTurn.
/// </summary>
public class TurnController
{
    private readonly Map _map;
    private readonly GameContext _ctx;

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

    /// <summary>Начать ход: пропуск (болото) и отложенные эффекты болота.</summary>
    public void BeginTurn()
    {
        if (_ctx.IsWon) return;

        _ctx.Messages.Clear();
        _ctx.AddMessage("Ход."); // можно заменить на «Подтвердить ход»

        if (_ctx.Player.SkipNextTurn)
        {
            _ctx.Player.SkipNextTurn = false;
            _ctx.AddMessage("Вы пропускаете этот ход.");
            FinishTurn();
            return;
        }

        // Swamp: остальные эффекты клетки срабатывают на ходе после пропущенного.
        if (_ctx.DelayedCell.HasValue)
        {
            var (r, c) = _ctx.DelayedCell.Value;
            _ctx.DelayedCell = null;
            ApplyOtherEffects(r, c, Arrival.Start, enteredFrom: null);
        }

        FinishTurn();
    }

    /// <summary>Ход игрока в направлении.</summary>
    public void Move(Direction dir)
    {
        if (_ctx.IsWon) return;

        _ctx.Messages.Clear();
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
            if (dir != currentRiver.FlowOut)
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
        _ctx.AddMessage("Вас смыло по течению.");
        ApplyEffects(nr, nc, Arrival.FromRiver, enteredFrom: DirectionUtil.Opposite(flowOut));
    }

    // ============================================================
    //  Внутреннее
    // ============================================================

    private void FinishTurn()
    {
        _ctx.FlushTurnMessages();
        _ctx.OnTurnCompleted?.Invoke();
    }

    private void ApplyEffects(int row, int col, Arrival arrival, Direction? enteredFrom)
    {
        _ctx.LastMoveDirection = enteredFrom;
        var fills = _map[row, col].Fills.ToList();

        // Swamp — особый: ставит флаг и откладывает остальные эффекты.
        var swamp = fills.OfType<Swamp>().FirstOrDefault();
        if (swamp != null)
        {
            swamp.OnEnter(_ctx, row, col, arrival);
            if (fills.Any(f => !(f is Swamp)))
                _ctx.DelayedCell = (row, col);
            return;
        }

        ApplyOtherEffects(row, col, arrival, enteredFrom);
    }

    private void ApplyOtherEffects(int row, int col, Arrival arrival, Direction? enteredFrom)
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

            // Портал и река приоритетны: если сработали — дальше не идём.
            if (type == typeof(Portal) && fills.OfType<Portal>().Any())
                return;
            if (type == typeof(River) && fills.OfType<River>().Any())
                return;
        }

        // «Невидимые» эффекты (чужой ключ, выход без ключа) не добавили сообщения —
        // значит, для игрока клетка выглядит пустой.
        if (_ctx.Messages.Count == before)
            _ctx.AddMessage("Вы прошли.");
    }
}