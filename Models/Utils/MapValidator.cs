using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================
//  Исключение валидации
// ============================================================

public class MapValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public MapValidationException(IReadOnlyList<string> errors)
        : base("Карта не прошла валидацию:\n - " + string.Join("\n - ", errors))
    {
        Errors = errors;
    }
}

// C# 9: record struct недоступен, используем обычный struct.
// Entry == null означает «нет направления входа» (старт или телепорт).
public readonly struct State : IEquatable<State>
{
    public int Row { get; }
    public int Col { get; }
    public Direction? Entry { get; }
    public Arrival Arrival { get; }

    public State(int row, int col, Direction? entry, Arrival arrival)
    {
        Row = row;
        Col = col;
        Entry = entry;
        Arrival = arrival;
    }

    public bool Equals(State other) =>
        Row == other.Row &&
        Col == other.Col &&
        Entry == other.Entry &&
        Arrival == other.Arrival;

    public override bool Equals(object obj) => obj is State s && Equals(s);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Row;
            hash = (hash * 397) ^ Col;
            hash = (hash * 397) ^ (Entry.HasValue ? (int)Entry.Value + 1 : 0);
            hash = (hash * 397) ^ (int)Arrival;
            return hash;
        }
    }

    public override string ToString() =>
        $"({Row},{Col},{Entry?.ToString() ?? "null"},{Arrival})";
}

// ============================================================
//  Валидатор карты
// ============================================================

public static class MapValidator
{
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 512;
    public const string DefaultName = "Без названия";

    public static void Validate(Map map)
    {
        var errors = new List<string>();

        ValidateMetadata(map, errors);
        ValidateKeys(map, errors);
        ValidateExits(map, errors);
        ValidateFillPairs(map, errors);
        ValidateRivers(map, errors);
        ValidatePortals(map, errors);

        if (errors.Count == 0)
            ValidateConnectivity(map, errors);

        if (errors.Count > 0)
            throw new MapValidationException(errors);
    }

    private static void ValidateMetadata(Map map, List<string> errors)
    {
        var name = map.Name?.Trim() ?? string.Empty;
        var desc = map.Description?.Trim() ?? string.Empty;

        if (name.Length == 0)
            errors.Add($"У карты должно быть название (поле Name не может быть пустым)");
        else if (name.Length > MaxNameLength)
            errors.Add($"Название карты длиннее {MaxNameLength} символов (сейчас {name.Length})");

        if (desc.Length > MaxDescriptionLength)
            errors.Add($"Описание карты длиннее {MaxDescriptionLength} символов (сейчас {desc.Length})");
    }

    // ---------- 1. Ключи: >= 1, номера непрерывны 1..N ----------

    private static void ValidateKeys(Map map, List<string> errors)
    {
        var nums = AllKeys(map).Select(k => k.PlayerNum).Distinct().OrderBy(n => n).ToList();

        if (nums.Count == 0)
        {
            errors.Add("На карте должен быть хотя бы один ключ");
            return;
        }

        if (!nums.SequenceEqual(Enumerable.Range(1, nums.Count)))
            errors.Add($"Номера ключей должны быть непрерывными 1..{nums.Count}; найдено: {string.Join(",", nums)}");
    }

    // ---------- 2. Выход: ровно 1 ----------

    private static void ValidateExits(Map map, List<string> errors)
    {
        int exits = map.Cells.SelectMany(c => c.Fills).OfType<Exit>().Count();
        if (exits != 1)
            errors.Add($"Выход должен быть ровно 1, найдено {exits}");
    }

    // ---------- 3. Совместимость наполнений в клетке ----------

    private static void ValidateFillPairs(Map map, List<string> errors)
    {
        foreach (var (r, c) in AllCells(map))
        {
            var fills = map[r, c].Fills;
            for (int i = 0; i < fills.Count; i++)
                for (int j = i + 1; j < fills.Count; j++)
                {
                    if (!FillPairRules.AreCompatible(fills[i].FillType, fills[j].FillType))
                        errors.Add(
                            $"Несовместимая пара в ({r},{c}): " +
                            $"{fills[i].FillType.Name} + {fills[j].FillType.Name}");
                }
        }
    }

    // ---------- 4. Непрерывность рек ----------

    private static void ValidateRivers(Map map, List<string> errors)
    {
        var riverCells = AllCells(map)
            .Select(rc => (rc.r, rc.c, river: map[rc.r, rc.c].Fills.OfType<River>().FirstOrDefault()))
            .Where(x => x.river != null)
            .ToList();

        foreach (var (r, c, river) in riverCells)
        {
            var inNeighbor = Neighbor(map, r, c, river.FlowIn);
            var outNeighbor = Neighbor(map, r, c, river.FlowOut);

            // 1. Вход (FlowIn)
            if (river.End == RiverEnd.Source)
            {
                if (inNeighbor != null) // Исток обязан упираться в стену
                    errors.Add($"Исток ({r},{c}): FlowIn должен упираться в стенку");
            }
            else
            {
                // У всех остальных (включая устье) вход должен стыковаться с выходом соседа
                bool inIsRiverOk = inNeighbor is { } n1
                    && map[n1.Row, n1.Col].Fills.OfType<River>()
                        .Any(rv => rv.FlowOut == DirectionUtil.Opposite(river.FlowIn));

                if (!inIsRiverOk)
                    errors.Add($"Река ({r},{c}): FlowIn не стыкуется с соседней рекой");
            }

            // 2. Выход (FlowOut)
            if (river.End == RiverEnd.Mouth)
            {
                if (outNeighbor != null) // Устье обязано упираться в стену
                    errors.Add($"Устье ({r},{c}): FlowOut должен упираться в стенку");
            }
            else
            {
                // У всех остальных (включая исток) выход должен стыковаться с входом соседа
                bool outIsRiverOk = outNeighbor is { } n2
                    && map[n2.Row, n2.Col].Fills.OfType<River>()
                        .Any(rv => rv.FlowIn == DirectionUtil.Opposite(river.FlowOut));

                if (!outIsRiverOk)
                    errors.Add($"Река ({r},{c}): FlowOut не стыкуется с соседней рекой");
            }
        }
    }

    // ---------- 5. Порталы: >= 2, номера непрерывны 1..N ----------

    private static void ValidatePortals(Map map, List<string> errors)
    {
        var nums = PortalNavigator.AllPortals(map).Select(p => p.num).Distinct().OrderBy(n => n).ToList();

        if (nums.Count == 0) return; // 0 порталов — ок

        if (nums.Count == 1)
        {
            errors.Add("На карте только один портал — телепорт некуда");
            return;
        }

        if (!nums.SequenceEqual(Enumerable.Range(1, nums.Count)))
            errors.Add($"Номера порталов должны быть непрерывными 1..{nums.Count}; найдено: {string.Join(",", nums)}");
    }

    // ---------- 6. Связность и отсутствие ловушек ----------

    private static void ValidateConnectivity(Map map, List<string> errors)
    {
        // Строим граф всех состояний.
        var states = new HashSet<State>();
        foreach (var (r, c) in AllCells(map))
        {
            foreach (var entry in AllDirections)
            {
                states.Add(new State(r, c, entry, Arrival.FromMove));
                states.Add(new State(r, c, entry, Arrival.FromPortal));
                states.Add(new State(r, c, entry, Arrival.FromRiver));
            }
            // Добавляем состояния без направления входа
            states.Add(new State(r, c, null, Arrival.Start));
            states.Add(new State(r, c, null, Arrival.FromPortal));
        }

        var adj = new Dictionary<State, List<State>>();
        foreach (var s in states)
            adj[s] = GetNextStates(map, s).ToList();

        // 6.1 Тупики: состояния без переходов, кроме легальных терминалов.
        foreach (var s in states)
        {
            if (adj[s].Count != 0) continue;

            var cell = map[s.Row, s.Col];

            bool isTerminal =
                cell.Fills.OfType<Exit>().Any() ||
                cell.Fills.OfType<River>().Any(rv => rv.End == RiverEnd.Mouth);

            if (!isTerminal)
                errors.Add($"Тупик в клетке ({s.Row},{s.Col}) при входе {s.Entry?.ToString() ?? "null"}, режим {s.Arrival}");
        }

        // 6.2 Ловушки: SCC без достижимого Exit.
        var exitStates = states
            .Where(s => map[s.Row, s.Col].Fills.OfType<Exit>().Any())
            .ToHashSet();

        var sccs = TarjanSCC(adj);

        foreach (var scc in sccs)
        {
            // Одиночное состояние без self-loop не считаем ловушкой
            // (тупики уже обработаны выше).
            if (scc.Count == 1 && !adj[scc[0]].Contains(scc[0]))
                continue;

            if (!SccReachesExit(scc, adj, exitStates))
            {
                errors.Add(
                    "Замкнутый цикл без выхода: " +
                    string.Join(", ", scc.Select(s => s.ToString())));
            }
        }
    }

    private static bool SccReachesExit(
        List<State> scc,
        Dictionary<State, List<State>> adj,
        HashSet<State> exitStates)
    {
        var visited = new HashSet<State>(scc);
        var queue = new Queue<State>(scc);

        while (queue.Count > 0)
        {
            var v = queue.Dequeue();
            if (exitStates.Contains(v)) return true;

            foreach (var w in adj[v])
                if (visited.Add(w))
                    queue.Enqueue(w);
        }
        return false;
    }

    private static List<List<State>> TarjanSCC(Dictionary<State, List<State>> adj)
    {
        var index = new Dictionary<State, int>();
        var low = new Dictionary<State, int>();
        var onStack = new HashSet<State>();
        var stack = new Stack<State>();
        var result = new List<List<State>>();
        int counter = 0;

        void StrongConnect(State v)
        {
            index[v] = low[v] = counter++;
            stack.Push(v);
            onStack.Add(v);

            foreach (var w in adj[v])
            {
                if (!index.ContainsKey(w))
                {
                    StrongConnect(w);
                    low[v] = Math.Min(low[v], low[w]);
                }
                else if (onStack.Contains(w))
                {
                    low[v] = Math.Min(low[v], index[w]);
                }
            }

            if (low[v] == index[v])
            {
                var scc = new List<State>();
                State w;
                do
                {
                    w = stack.Pop();
                    onStack.Remove(w);
                    scc.Add(w);
                } while (!w.Equals(v));
                result.Add(scc);
            }
        }

        foreach (var v in adj.Keys)
            if (!index.ContainsKey(v))
                StrongConnect(v);

        return result;
    }

    // ============================================================
    //  Переходы
    // ============================================================

    public static IEnumerable<State> GetNextStates(Map map, State s)
    {
        var fills = map[s.Row, s.Col].Fills;

        bool portalCanFire =
            s.Arrival is Arrival.Start or Arrival.FromMove or Arrival.FromRiver;
        bool riverCanFire =
            s.Arrival is Arrival.Start or Arrival.FromMove or Arrival.FromPortal;

        // 1) Портал — приоритет над рекой, если срабатывает.
        var portal = fills.OfType<Portal>().FirstOrDefault();
        if (portal != null && portalCanFire)
        {
            foreach (var (r, c) in NextPortals(map, portal.Num))
            {
                if (r == s.Row && c == s.Col) continue; // не в себя
                yield return new State(r, c, null, Arrival.FromPortal);
            }
            yield break;
        }

        // 2) Река — смыв по FlowOut; вход не важен.
        var river = fills.OfType<River>().FirstOrDefault();
        if (river != null && riverCanFire)
        {
            if (river.End == RiverEnd.Mouth) yield break;

            var next = Step(map, s.Row, s.Col, river.FlowOut);
            if (next.HasValue)
            {
                var n = next.Value;
                yield return new State(n.Row, n.Col, n.Entry, Arrival.FromRiver);
            }
            yield break;
        }

        // 3) Резинка — старт: любой выход; иначе только откуда зашёл.
        if (fills.OfType<RubberRoom>().Any())
        {
            if (s.Entry == null)
            {
                foreach (var dir in AllDirections)
                {
                    var next = Step(map, s.Row, s.Col, dir);
                    if (next.HasValue)
                    {
                        var n = next.Value;
                        yield return new State(n.Row, n.Col, n.Entry, Arrival.FromMove);
                    }
                }
            }
            else
            {
                var next = Step(map, s.Row, s.Col, s.Entry.Value);
                if (next.HasValue)
                {
                    var n = next.Value;
                    yield return new State(n.Row, n.Col, n.Entry, Arrival.FromMove);
                }
            }
            yield break;
        }

        // 4) Обычная клетка (меч, болото, дракон, пустая) — все 4 стороны.
        foreach (var dir in AllDirections)
        {
            var next = Step(map, s.Row, s.Col, dir);
            if (next.HasValue)
            {
                var n = next.Value;
                yield return new State(n.Row, n.Col, n.Entry, Arrival.FromMove);
            }
        }
    }

    // ============================================================
    //  Геометрия
    // ============================================================

    private static State? Step(Map map, int row, int col, Direction dir)
    {
        var (dr, dc) = DirectionUtil.Offset(dir);
        int nr = row + dr, nc = col + dc;

        if (nr < 0 || nr >= map.Rows || nc < 0 || nc >= map.Cols)
            return null;

        return new State(nr, nc, DirectionUtil.Opposite(dir), Arrival.FromMove);
    }

    private static (int Row, int Col)? Neighbor(Map map, int row, int col, Direction dir)
    {
        var (dr, dc) = DirectionUtil.Offset(dir);
        int nr = row + dr, nc = col + dc;

        if (nr < 0 || nr >= map.Rows || nc < 0 || nc >= map.Cols)
            return null;

        return (nr, nc);
    }

    // ============================================================
    //  Утилиты
    // ============================================================

    private static readonly Direction[] AllDirections =
    {
        Direction.Up, Direction.Right, Direction.Down, Direction.Left
    };

    private static IEnumerable<(int r, int c)> AllCells(Map map)
    {
        for (int r = 0; r < map.Rows; r++)
            for (int c = 0; c < map.Cols; c++)
                yield return (r, c);
    }

    private static IEnumerable<(int Row, int Col, int PlayerNum)> AllKeys(Map map)
    {
        foreach (var (r, c) in AllCells(map))
            foreach (var k in map[r, c].Fills.OfType<Key>())
                yield return (r, c, k.PlayerNum);
    }

    private static IEnumerable<(int Row, int Col)> NextPortals(Map map, int currentNum)
    {
        var portals = PortalNavigator.AllPortals(map).ToList();
        var nums = portals.Select(p => p.num).Distinct().OrderBy(n => n).ToList();
        if (nums.Count == 0) yield break;

        int idx = nums.IndexOf(currentNum);
        if (idx < 0) yield break; // портал с таким номером не найден — не должно случаться

        int nextNum = nums[(idx + 1) % nums.Count];

        foreach (var p in portals.Where(p => p.num == nextNum))
            yield return (p.row, p.col);
    }
}