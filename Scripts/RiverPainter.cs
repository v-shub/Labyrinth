using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RiverPainter : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private MapController mapController;
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private RiverView riverView;

    [Header("Режим")]
    [SerializeField] private bool startActive = false;
    [Tooltip("Порог в пикселях, после которого нажатие считается протяжкой, а не кликом.")]
    [SerializeField] private float dragThresholdPx = 8f;

    [Header("Отладка")]
    [SerializeField] private bool debugLogs = false;

    private Cell[,] _cells;
    private bool _active;

    private bool _dragging;
    private bool _dragMoved;
    private Cell _pressCell;
    private Vector2 _pressPos;
    private readonly List<(int r, int c)> _session = new();
    private readonly HashSet<(int r, int c)> _sessionSet = new();

    public bool IsActive => _active;

    public void SetActive(bool value)
    {
        if (_active == value) return;
        _active = value;
        if (!_active) CancelSession();
    }

    public void ToggleActive() => SetActive(!_active);

    private void Awake()
    {
        if (mapController == null) mapController = FindFirstObjectByType<MapController>();
        if (gridGenerator == null) gridGenerator = FindFirstObjectByType<GridGenerator>();
    }

    private void Start()
    {
        if (gridGenerator.Cells == null) gridGenerator.Generate();
        _cells = gridGenerator.Cells;
        _ = mapController.Map;
        _active = startActive;
    }

    private void Update()
    {
        if (!_active) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 pos = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame) OnPress(pos);
        else if (mouse.leftButton.isPressed && _dragging) OnHold(pos);
        else if (mouse.leftButton.wasReleasedThisFrame && _dragging) OnRelease(pos);
    }

    private void OnPress(Vector2 pos)
    {
        if (EventSystem.current == null) return;
        var cell = RaycastCellUI(pos);
        if (cell == null) return;

        _dragging = true;
        _dragMoved = false;
        _pressCell = cell;
        _pressPos = pos;

        _session.Clear();
        _sessionSet.Clear();
        PushToSession(cell.Y, cell.X);
    }

    private void OnHold(Vector2 pos)
    {
        if (!_dragMoved && (pos - _pressPos).sqrMagnitude >= dragThresholdPx * dragThresholdPx)
            _dragMoved = true;

        if (!_dragMoved) return;

        var cell = RaycastCellUI(pos);
        if (cell != null) TryExtendSession(cell.Y, cell.X);
    }

    private void OnRelease(Vector2 pos)
    {
        var cell = RaycastCellUI(pos);

        if (!_dragMoved)
        {
            CancelSession();
            if (cell != null) HandleClick(cell);
            return;
        }

        if (cell != null) TryExtendSession(cell.Y, cell.X);

        CommitSession();
        CancelSession();
    }

    private void CancelSession()
    {
        _dragging = false;
        _dragMoved = false;
        _pressCell = null;
        _session.Clear();
        _sessionSet.Clear();
    }

    private void HandleClick(Cell cell)
    {
        int r = cell.Y, c = cell.X;
        if (!HasRiver(r, c)) return;

        RemoveRiverAt(r, c);
        mapController.RefreshAllVisuals();
    }

    private void PushToSession(int r, int c)
    {
        if (_sessionSet.Add((r, c)))
            _session.Add((r, c));
    }

    private void TryExtendSession(int r, int c)
    {
        if (_session.Count == 0) { PushToSession(r, c); return; }

        var last = _session[_session.Count - 1];
        if (last == (r, c)) return;

        var path = BuildPath(last, (r, c));
        if (path == null) return;

        for (int i = 1; i < path.Count; i++)
            PushToSession(path[i].r, path[i].c);
    }

    private static List<(int r, int c)> BuildPath((int r, int c) from, (int r, int c) to)
    {
        var path = new List<(int r, int c)> { from };
        var cur = from;
        while (cur != to)
        {
            int dr = Math.Sign(to.r - cur.r);
            int dc = Math.Sign(to.c - cur.c);

            if (Math.Abs(to.r - cur.r) >= Math.Abs(to.c - cur.c) && dr != 0)
                cur = (cur.r + dr, cur.c);
            else if (dc != 0)
                cur = (cur.r, cur.c + dc);
            else if (dr != 0)
                cur = (cur.r + dr, cur.c);
            else
                return null;

            path.Add(cur);
            if (path.Count > 512) return null;
        }
        return path;
    }

    private void CommitSession()
    {
        if (_session.Count == 0) return;

        int validEnd = ComputeValidEndIndex();
        if (validEnd < 0) return;

        var newSegment = new List<(int r, int c)>();
        var isOldCell = new HashSet<(int r, int c)>();

        for (int i = 0; i <= validEnd; i++)
        {
            var p = _session[i];
            newSegment.Add(p);
            if (HasRiver(p.r, p.c)) isOldCell.Add(p);
        }

        if (newSegment.Count == 0) return;

        bool connectStart = false;
        bool connectEnd = false;
        int startLinks = 0, endLinks = 0;

        if (newSegment.Count > 1 && isOldCell.Contains(newSegment[0]))
        {
            var pStart = newSegment[0];
            var existing = GetRiver(pStart.r, pStart.c);
            if (CanAttach(existing, pStart, isStart: true))
            {
                connectStart = true;
                var (inL, outL) = GetExistingLinks(existing, pStart);
                startLinks = (inL ? 1 : 0) + (outL ? 1 : 0);
            }
        }

        if (newSegment.Count > 1 && isOldCell.Contains(newSegment[newSegment.Count - 1]))
        {
            var pEnd = newSegment[newSegment.Count - 1];
            var existing = GetRiver(pEnd.r, pEnd.c);
            if (CanAttach(existing, pEnd, isStart: false))
            {
                connectEnd = true;
                var (inL, outL) = GetExistingLinks(existing, pEnd);
                endLinks = (inL ? 1 : 0) + (outL ? 1 : 0);
            }
        }

        if (debugLogs)
        {
            Debug.Log($"[RiverPainter] newSegment={newSegment.Count}, " +
                      $"connectStart={connectStart}(links={startLinks}), " +
                      $"connectEnd={connectEnd}(links={endLinks})");
        }

        // Одиночная старая клетка — ничего не делаем
        if (newSegment.Count == 1 && isOldCell.Contains(newSegment[0]))
            return;

        // 1. Создаем новые River
        for (int i = 0; i < newSegment.Count; i++)
        {
            var p = newSegment[i];
            if (isOldCell.Contains(p)) continue;

            var container = mapController.Map[p.r, p.c];
            var river = new River(Direction.Left, Direction.Right);
            if (!container.CanAdd(river)) continue;

            mapController.TryPlace(p.r, p.c, river);
            GetCell(p.r, p.c).AddFill(river, null);
        }

        // 2. Назначаем направления НОВЫМ клеткам
        for (int i = 0; i < newSegment.Count; i++)
        {
            var p = newSegment[i];
            if (isOldCell.Contains(p)) continue;

            var river = GetRiver(p.r, p.c);
            if (river == null) continue;

            Direction flowIn, flowOut;

            if (i == 0)
            {
                flowOut = GetDirectionBetween(p, newSegment[1]);
                flowIn = PickFreeFlowIn(p, flowOut);
                river.End = IsWall(p.r, p.c, flowIn) ? RiverEnd.Source : RiverEnd.None;
            }
            else if (i == newSegment.Count - 1)
            {
                flowIn = GetDirectionBetween(p, newSegment[i - 1]);
                flowOut = PickFreeFlowOut(p, flowIn);
                river.End = IsWall(p.r, p.c, flowOut) ? RiverEnd.Mouth : RiverEnd.None;
            }
            else
            {
                flowIn = GetDirectionBetween(p, newSegment[i - 1]);
                flowOut = GetDirectionBetween(p, newSegment[i + 1]);
                river.End = RiverEnd.None;
            }

            river.FlowIn = flowIn;
            river.FlowOut = flowOut;
            UpdateVisual(p.r, p.c, river);
        }

        // 3. Обновляем СТАРЫЕ клетки на краях
        if (connectStart)
        {
            ApplyConnectStart(newSegment, startLinks);
        }

        if (connectEnd)
        {
            ApplyConnectEnd(newSegment, endLinks);
        }

        mapController.RefreshAllVisuals();
    }

    private void ApplyConnectStart(List<(int r, int c)> newSegment, int startLinks)
    {
        var pStart = newSegment[0];
        var river = GetRiver(pStart.r, pStart.c);
        if (river == null) return;

        var dirToNew = GetDirectionBetween(pStart, newSegment[1]);

        if (startLinks == 0)
        {
            // Одиночная старая клетка становится началом новой реки.
            river.FlowOut = dirToNew;
            river.FlowIn = PickFreeFlowIn(pStart, river.FlowOut);
            river.End = IsWall(pStart.r, pStart.c, river.FlowIn) ? RiverEnd.Source : RiverEnd.None;
        }
        else
        {
            // links == 1: у старой клетки занят FlowIn, свободен FlowOut.
            // Стыкуем начало нового куска: flowOut = направление к новой части.
            river.FlowOut = dirToNew;
            // Старая клетка перестаёт быть концом (Mouth) — теперь она середина.
            river.End = RiverEnd.None;
        }

        if (debugLogs)
        {
            Debug.Log($"[RiverPainter] Start {pStart}: FlowIn={river.FlowIn}, " +
                      $"FlowOut={river.FlowOut}, End={river.End}, links={startLinks}");
        }

        UpdateVisual(pStart.r, pStart.c, river);
    }

    private void ApplyConnectEnd(List<(int r, int c)> newSegment, int endLinks)
    {
        var pEnd = newSegment[newSegment.Count - 1];
        var river = GetRiver(pEnd.r, pEnd.c);
        if (river == null) return;

        var dirToNew = GetDirectionBetween(pEnd, newSegment[newSegment.Count - 2]);

        if (endLinks == 0)
        {
            // Одиночная старая клетка становится концом новой реки.
            river.FlowIn = dirToNew;
            river.FlowOut = PickFreeFlowOut(pEnd, river.FlowIn);
            river.End = IsWall(pEnd.r, pEnd.c, river.FlowOut) ? RiverEnd.Mouth : RiverEnd.None;
        }
        else
        {
            // links == 1: у старой клетки занят FlowOut, свободен FlowIn.
            // Стыкуем конец нового куска: flowIn = направление к новой части.
            river.FlowIn = dirToNew;
            // Старая клетка перестаёт быть началом (Source) — теперь она середина.
            river.End = RiverEnd.None;
        }

        if (debugLogs)
        {
            Debug.Log($"[RiverPainter] End {pEnd}: FlowIn={river.FlowIn}, " +
                      $"FlowOut={river.FlowOut}, End={river.End}, links={endLinks}");
        }

        UpdateVisual(pEnd.r, pEnd.c, river);
    }

    private Direction PickFreeFlowIn((int r, int c) pos, Direction flowOut)
    {
        var walls = GetWallDirections(pos.r, pos.c);
        foreach (var w in walls) if (w != flowOut) return w;
        return Opposite(flowOut);
    }

    private Direction PickFreeFlowOut((int r, int c) pos, Direction flowIn)
    {
        var walls = GetWallDirections(pos.r, pos.c);
        foreach (var w in walls) if (w != flowIn) return w;
        return Opposite(flowIn);
    }

    private (bool inLinked, bool outLinked) GetExistingLinks(River existing, (int r, int c) pos)
    {
        bool inLinked = false, outLinked = false;

        var nIn = GetNeighbor(pos.r, pos.c, existing.FlowIn);
        if (HasRiver(nIn.r, nIn.c))
        {
            var nb = GetRiver(nIn.r, nIn.c);
            if (nb.FlowOut == Opposite(existing.FlowIn)) inLinked = true;
        }

        var nOut = GetNeighbor(pos.r, pos.c, existing.FlowOut);
        if (HasRiver(nOut.r, nOut.c))
        {
            var nb = GetRiver(nOut.r, nOut.c);
            if (nb.FlowIn == Opposite(existing.FlowOut)) outLinked = true;
        }

        return (inLinked, outLinked);
    }

    private bool CanAttach(River existing, (int r, int c) pos, bool isStart)
    {
        var (inLinked, outLinked) = GetExistingLinks(existing, pos);
        int links = (inLinked ? 1 : 0) + (outLinked ? 1 : 0);

        if (links == 2) return false;
        if (links == 0) return true;

        if (inLinked && !outLinked) return isStart;
        if (outLinked && !inLinked) return !isStart;

        return false;
    }

    private int ComputeValidEndIndex()
    {
        int last = _session.Count - 1;
        if (last == 0) return 0;

        for (int i = 0; i <= last; i++)
        {
            var p = _session[i];
            if (!HasRiver(p.r, p.c)) continue;

            if (i == 0)
            {
                var existing = GetRiver(p.r, p.c);
                if (!CanAttach(existing, p, isStart: true))
                    return -1;
                continue;
            }

            if (i == last)
            {
                var existing = GetRiver(p.r, p.c);
                if (!CanAttach(existing, p, isStart: false))
                    return i - 1;
                continue;
            }

            return i - 1;
        }
        return last;
    }

    private void RemoveRiverAt(int r, int c)
    {
        var river = GetRiver(r, c);
        if (river == null) return;

        var cell = GetCell(r, c);
        mapController.RemoveFill(cell, river);
        cell.RemoveTopFill();

        foreach (var n in RiverNeighbors(r, c))
        {
            var chain = CollectChain(n.r, n.c);
            if (chain.Count > 0) RebuildChainDirections(chain);
        }
    }

    private void RebuildChainDirections(List<(int r, int c)> chain)
    {
        if (chain.Count == 0) return;

        var ends = new List<(int r, int c)>();
        foreach (var p in chain)
            if (CountRiverNeighbors(p.r, p.c) == 1) ends.Add(p);

        if (ends.Count == 0) return;

        var ordered = OrderChain(ends[0], ends.Count > 1 ? ends[1] : (-1, -1), chain);
        if (ordered == null || ordered.Count == 0) return;

        for (int i = 0; i < ordered.Count; i++)
        {
            var p = ordered[i];
            var river = GetRiver(p.r, p.c);
            if (river == null) continue;

            Direction flowIn, flowOut;

            if (i == 0)
            {
                flowOut = GetDirectionBetween(p, ordered[i + 1]);
                flowIn = PickFreeFlowIn(p, flowOut);
                river.End = IsWall(p.r, p.c, flowIn) ? RiverEnd.Source : RiverEnd.None;
            }
            else if (i == ordered.Count - 1)
            {
                flowIn = GetDirectionBetween(p, ordered[i - 1]);
                flowOut = PickFreeFlowOut(p, flowIn);
                river.End = IsWall(p.r, p.c, flowOut) ? RiverEnd.Mouth : RiverEnd.None;
            }
            else
            {
                flowIn = GetDirectionBetween(p, ordered[i - 1]);
                flowOut = GetDirectionBetween(p, ordered[i + 1]);
                river.End = RiverEnd.None;
            }

            river.FlowIn = flowIn;
            river.FlowOut = flowOut;
            UpdateVisual(p.r, p.c, river);
        }
    }

    private static Direction GetDirectionBetween((int r, int c) a, (int r, int c) b)
    {
        int dr = b.r - a.r;
        int dc = b.c - a.c;
        return (dr, dc) switch
        {
            (-1, 0) => Direction.Up,
            (1, 0) => Direction.Down,
            (0, -1) => Direction.Left,
            (0, 1) => Direction.Right,
            _ => throw new Exception("Клетки не соседние")
        };
    }

    private static Direction Opposite(Direction d) => d switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => d
    };

    private static (int r, int c) GetNeighbor(int r, int c, Direction dir)
    {
        return dir switch
        {
            Direction.Up => (r - 1, c),
            Direction.Down => (r + 1, c),
            Direction.Left => (r, c - 1),
            Direction.Right => (r, c + 1),
            _ => (-1, -1)
        };
    }

    private List<Direction> GetWallDirections(int r, int c)
    {
        var walls = new List<Direction>(2);
        if (r == 0) walls.Add(Direction.Up);
        if (r == Rows - 1) walls.Add(Direction.Down);
        if (c == 0) walls.Add(Direction.Left);
        if (c == Cols - 1) walls.Add(Direction.Right);
        return walls;
    }

    private bool IsWall(int r, int c, Direction dir)
    {
        return dir switch
        {
            Direction.Up => r == 0,
            Direction.Down => r == Rows - 1,
            Direction.Left => c == 0,
            Direction.Right => c == Cols - 1,
            _ => false
        };
    }

    private void UpdateVisual(int r, int c, River river)
    {
        var cell = GetCell(r, c);
        var sprite = riverView != null ? riverView.GetSprite(river) : null;
        cell.ReplaceSprite(river, sprite);
    }

    private List<(int r, int c)> CollectChain(int r, int c)
    {
        var result = new List<(int r, int c)>();
        var visited = new HashSet<(int r, int c)>();
        var stack = new Stack<(int r, int c)>();
        stack.Push((r, c));

        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (!visited.Add(cur)) continue;
            if (!HasRiver(cur.r, cur.c)) continue;
            result.Add(cur);
            foreach (var n in RiverNeighbors(cur.r, cur.c))
                if (!visited.Contains(n)) stack.Push(n);
        }
        return result;
    }

    private List<(int r, int c)> RiverNeighbors(int r, int c)
    {
        var list = new List<(int r, int c)>(4);
        if (r > 0 && HasRiver(r - 1, c)) list.Add((r - 1, c));
        if (r < Rows - 1 && HasRiver(r + 1, c)) list.Add((r + 1, c));
        if (c > 0 && HasRiver(r, c - 1)) list.Add((r, c - 1));
        if (c < Cols - 1 && HasRiver(r, c + 1)) list.Add((r, c + 1));
        return list;
    }

    private int CountRiverNeighbors(int r, int c) => RiverNeighbors(r, c).Count;

    private static List<(int r, int c)> OrderChain(
        (int r, int c) start, (int r, int c) end, List<(int r, int c)> chain)
    {
        var ordered = new List<(int r, int c)>(chain.Count);
        var set = new HashSet<(int, int)>(chain);
        var prev = (-1, -1);
        var cur = start;

        while (true)
        {
            ordered.Add(cur);
            if (end != (-1, -1) && cur == end) break;
            if (end == (-1, -1) && ordered.Count == chain.Count) break;

            (int r, int c) next = (-1, -1);
            foreach (var n in GetNeighbors(cur.r, cur.c))
            {
                if (n == prev) continue;
                if (!set.Contains(n)) continue;
                next = n;
                break;
            }

            if (next == (-1, -1)) return null;
            prev = cur;
            cur = next;
        }
        return ordered;
    }

    private static IEnumerable<(int r, int c)> GetNeighbors(int r, int c)
    {
        yield return (r - 1, c);
        yield return (r + 1, c);
        yield return (r, c - 1);
        yield return (r, c + 1);
    }

    private int Rows => mapController.Map.Rows;
    private int Cols => mapController.Map.Cols;
    private Cell GetCell(int r, int c) => _cells[c, r];

    private bool HasRiver(int r, int c)
    {
        if (r < 0 || r >= Rows || c < 0 || c >= Cols) return false;
        foreach (var f in mapController.Map[r, c].Fills)
            if (f is River) return true;
        return false;
    }

    private River GetRiver(int r, int c)
    {
        foreach (var f in mapController.Map[r, c].Fills)
            if (f is River river) return river;
        return null;
    }

    private static Cell RaycastCellUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return null;
        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            var cell = r.gameObject.GetComponentInParent<Cell>();
            if (cell != null) return cell;
        }
        return null;
    }
}