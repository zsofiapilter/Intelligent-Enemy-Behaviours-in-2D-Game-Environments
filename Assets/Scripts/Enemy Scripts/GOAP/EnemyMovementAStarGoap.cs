using System.Collections.Generic;
using UnityEngine;

public class EnemyMovementAStarGoap : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("World-space cell size (meters per cell).")]
    public float cellSize = 1.0f;

    [Tooltip("Grid half extents in cells on X and Y.")]
    public Vector2Int gridHalfExtents = new Vector2Int(40, 40);

    [Tooltip("Blocking layers.")]
    public LayerMask obstacleMask;

    [Tooltip("Box overlap fraction of a cell to test block.")]
    [Range(0.2f, 1.0f)] public float occupancyFraction = 0.8f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    [Tooltip("Distance to a waypoint before advancing.")]
    public float waypointThreshold = 0.2f;
    [Tooltip("Repath when goal moved by at least this world distance.")]
    public float repathWhenGoalMovesBy = 0.5f;
    [Tooltip("Repath every N seconds regardless.")]
    public float hardRepathInterval = 0.75f;
    [SerializeField] private Transform _followTarget;
    public float agentRadius = 0.25f;

    [Header("Stuck Handling")]
    [Tooltip("Minimum progress per second before triggering a repath.")]
    public float minProgressPerSecond = 0.02f;
    [Tooltip("Window in seconds to measure progress.")]
    public float progressWindowSeconds = 0.5f;

    [Header("A* Performance")]
    [Tooltip("Allow diagonals (8-neighbor).")]
    public bool allowDiagonal = true;
    [Tooltip("Maximum expanded nodes per search. 0 = unlimited.")]
    public int maxExpandedNodesPerSearch = 5000;

    [Header("Corner Avoidance (follow)")]
    public float avoidProbe = 0.9f;
    public float sideProbe = 0.5f;
    public float avoidGain = 0.9f;

    private EnemyGoap _enemy;
    private readonly List<Vector2> _path = new List<Vector2>();
    private int _pathIndex = 0;

    private bool _hasGoal;
    private Vector2 _goalWorld;
    private Vector2Int _goalGridPrev;
    private float _lastRepathTime = -999f;

    private Vector2 _lastProgressPos;
    private float _progressTimer;

    private readonly List<Vector2Int> _neighbors = new List<Vector2Int>(8);

    void Awake()
    {
        _enemy = GetComponent<EnemyGoap>();
        if (!_enemy) Debug.LogError($"{name}: EnemyMovementAStar requires Enemy.");
    }

    void OnEnable()
    {
        _path.Clear();
        _pathIndex = 0;
        _hasGoal = false;
    }

    void Update()
    {
        if (_followTarget != null)
        {
            _goalWorld = _followTarget.position;
        }

        if (!_enemy || !_enemy.CanMove)
        {
            _enemy?.moveEnemy(Vector2.zero);
            return;
        }

        if (!_hasGoal)
        {
            _enemy.moveEnemy(Vector2.zero);
            return;
        }

        bool needRepath = false;

        Vector2Int goalGrid = WorldToGrid(_goalWorld);
        if (goalGrid != _goalGridPrev)
        {
            if (Vector2.Distance(GridToWorld(goalGrid), GridToWorld(_goalGridPrev)) >= repathWhenGoalMovesBy)
                needRepath = true;
        }

        if (Time.time - _lastRepathTime > hardRepathInterval)
            needRepath = true;

        _progressTimer += Time.deltaTime;
        float traveled = Vector2.Distance(transform.position, _lastProgressPos);
        if (_progressTimer >= progressWindowSeconds)
        {
            float rate = traveled / _progressTimer;
            if (rate < minProgressPerSecond) needRepath = true;
            _progressTimer = 0f;
            _lastProgressPos = transform.position;
        }

        if (needRepath || _path.Count == 0 || _pathIndex >= _path.Count)
        {
            ComputePathTo(_goalWorld);
        }

        if (_path.Count == 0)
        {
            _enemy.moveEnemy(Vector2.zero);
            return;
        }

        Vector2 wp = _path[_pathIndex];
        Vector2 toWp = wp - (Vector2)transform.position;
        float dist = toWp.magnitude;

        if (dist <= waypointThreshold)
        {
            _pathIndex++;
            if (_pathIndex >= _path.Count)
            {
                _enemy.moveEnemy(Vector2.zero);
                return;
            }
            wp = _path[_pathIndex];
            toWp = wp - (Vector2)transform.position;
        }
        TrySkipWaypoints();

        Vector2 dir = toWp.normalized;
        dir = SteerWithAvoidance(dir);
        _enemy.moveEnemy(dir * moveSpeed);
    }

    Vector2 SteerWithAvoidance(Vector2 desiredDir)
    {
        Vector2 pos = transform.position;

        bool hitFwd = Physics2D.Raycast(pos, desiredDir, avoidProbe, obstacleMask);
        if (!hitFwd) return desiredDir;

        Vector2 left = new Vector2(-desiredDir.y, desiredDir.x);
        Vector2 right = -left;

        bool hitL = Physics2D.Raycast(pos, (desiredDir + left * sideProbe).normalized, avoidProbe, obstacleMask);
        bool hitR = Physics2D.Raycast(pos, (desiredDir + right * sideProbe).normalized, avoidProbe, obstacleMask);

        Vector2 steer = desiredDir;
        if (hitL && !hitR) steer += right * avoidGain;
        else if (!hitL && hitR) steer += left * avoidGain;
        else steer += (Random.value < 0.5f ? left : right) * avoidGain;

        return steer.normalized;
    }

    void TrySkipWaypoints()
    {
        if (_pathIndex + 1 >= _path.Count) return;

        Vector2 me = transform.position;

        int skipTo = _pathIndex + 1;
        Vector2 candidate = _path[skipTo];

        bool blocked = Physics2D.Linecast(me, candidate, obstacleMask);
        if (!blocked)
        {
            _pathIndex = skipTo;
            return;
        }

        if (_pathIndex + 2 < _path.Count)
        {
            skipTo = _pathIndex + 2;
            candidate = _path[skipTo];
            blocked = Physics2D.Linecast(me, candidate, obstacleMask);
            if (!blocked)
                _pathIndex = skipTo;
        }
    }

    private bool IsWalkable(Vector2Int cell)
    {
        Vector2 center = GridToWorld(cell);
        Vector2 size = Vector2.one * (cellSize * occupancyFraction) + Vector2.one * (agentRadius * 2f);
        return !Physics2D.OverlapBox(center, size, 0f, obstacleMask);
    }

    public void SetGoalPosition(Vector2 worldPos)
    {
        _hasGoal = true;
        _goalWorld = worldPos;
        _goalGridPrev = WorldToGrid(worldPos);
        _lastRepathTime = -999f;
        _lastProgressPos = transform.position;
        _progressTimer = 0f;
    }

    public void ClearGoal()
    {
        _hasGoal = false;
        _path.Clear();
        _pathIndex = 0;
        _enemy?.moveEnemy(Vector2.zero);
    }

    private void ComputePathTo(Vector2 goalWorld)
    {
        _path.Clear();
        _pathIndex = 0;

        Vector2Int start = WorldToGrid(transform.position);
        Vector2Int goal = WorldToGrid(goalWorld);

        BoundsInt bounds = LocalGridBounds(start);

        if (!IsWalkable(goal)) goal = FindNearestWalkable(goal, 4, bounds);
        if (!IsWalkable(start)) start = FindNearestWalkable(start, 2, bounds);

        if (start == goal)
        {
            _path.Add(GridToWorld(goal));
            _lastRepathTime = Time.time;
            return;
        }

        var open = new BinaryHeap<Vector2Int>(128);
        var g = new Dictionary<Vector2Int, float>(256);
        var f = new Dictionary<Vector2Int, float>(256);
        var came = new Dictionary<Vector2Int, Vector2Int>(256);

        g[start] = 0f;
        f[start] = Heuristic(start, goal);
        open.Push(start, f[start]);

        int expanded = 0;

        while (open.Count > 0)
        {
            var current = open.PopMin();
            expanded++;
            if (maxExpandedNodesPerSearch > 0 && expanded > maxExpandedNodesPerSearch)
                break;

            if (current == goal)
            {
                BuildPath(came, current);
                _lastRepathTime = Time.time;
                return;
            }

            GetNeighbors(current, bounds, _neighbors);
            for (int i = 0; i < _neighbors.Count; i++)
            {
                var nb = _neighbors[i];
                if (!IsWalkable(nb)) continue;

                float step = (nb.x != current.x && nb.y != current.y) ? 1.41421356f : 1f;
                float tentativeG = g[current] + step;

                if (!g.TryGetValue(nb, out float gExisting) || tentativeG < gExisting)
                {
                    came[nb] = current;
                    g[nb] = tentativeG;
                    float newF = tentativeG + Heuristic(nb, goal);
                    f[nb] = newF;
                    open.PushOrDecreaseKey(nb, newF);
                }
            }
        }

        _lastRepathTime = Time.time;
    }

    private void BuildPath(Dictionary<Vector2Int, Vector2Int> came, Vector2Int end)
    {
        var rev = new List<Vector2Int>(64) { end };
        var cur = end;
        while (came.TryGetValue(cur, out var prev))
        {
            rev.Add(prev);
            cur = prev;
        }
        rev.Reverse();

        _path.Clear();
        for (int i = 0; i < rev.Count; i++)
        {
            Vector2 wp = GridToWorld(rev[i]);
            if (_path.Count >= 2)
            {
                Vector2 last = _path[_path.Count - 1];
                Vector2 d1 = (last - _path[_path.Count - 2]).normalized;
                Vector2 d2 = (wp - last).normalized;
                if (Vector2.Dot(d1, d2) > 0.999f)
                {
                    _path[_path.Count - 1] = wp;
                    continue;
                }
            }
            _path.Add(wp);
        }

        _pathIndex = Mathf.Min(1, _path.Count - 1);
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        if (allowDiagonal)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            int min = Mathf.Min(dx, dy);
            int max = Mathf.Max(dx, dy);
            return 1.41421356f * min + (max - min);
        }
        else
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }

    private void GetNeighbors(Vector2Int n, BoundsInt bounds, List<Vector2Int> outList)
    {
        outList.Clear();
        TryAdd(n + Vector2Int.up, bounds, outList);
        TryAdd(n + Vector2Int.down, bounds, outList);
        TryAdd(n + Vector2Int.left, bounds, outList);
        TryAdd(n + Vector2Int.right, bounds, outList);

        if (!allowDiagonal) return;

        TryAddDiag(n + new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(0, 1), bounds, outList);
        TryAddDiag(n + new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(0, -1), bounds, outList);
        TryAddDiag(n + new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 1), bounds, outList);
        TryAddDiag(n + new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(0, -1), bounds, outList);
    }

    private void TryAdd(Vector2Int c, BoundsInt b, List<Vector2Int> list)
    {
        if (c.x < b.xMin || c.x >= b.xMax || c.y < b.yMin || c.y >= b.yMax) return;
        list.Add(c);
    }

    private void TryAddDiag(Vector2Int diag, Vector2Int ax1, Vector2Int ax2, BoundsInt b, List<Vector2Int> list)
    {
        if (diag.x < b.xMin || diag.x >= b.xMax || diag.y < b.yMin || diag.y >= b.yMax) return;
        var n1 = diag - ax1;
        var n2 = diag - ax2;
        if (IsWalkable(n1) && IsWalkable(n2))
            list.Add(diag);
    }

    private BoundsInt LocalGridBounds(Vector2Int center)
    {
        return new BoundsInt(
            center.x - gridHalfExtents.x, center.y - gridHalfExtents.y, 0,
            gridHalfExtents.x * 2 + 1, gridHalfExtents.y * 2 + 1, 1
        );
    }

    private Vector2Int FindNearestWalkable(Vector2Int origin, int maxRadius, BoundsInt clamp)
    {
        if (IsWalkable(origin)) return origin;

        for (int r = 1; r <= maxRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    var c = new Vector2Int(origin.x + dx, origin.y + dy);
                    if (c.x < clamp.xMin || c.x >= clamp.xMax || c.y < clamp.yMin || c.y >= clamp.yMax) continue;
                    if (IsWalkable(c)) return c;
                }
        }
        return origin;
    }

    private Vector2Int WorldToGrid(Vector2 world)
    {
        float inv = 1f / Mathf.Max(0.0001f, cellSize);
        return new Vector2Int(Mathf.RoundToInt(world.x * inv), Mathf.RoundToInt(world.y * inv));
    }

    private Vector2 GridToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x * cellSize, cell.y * cellSize);
    }

    private class BinaryHeap<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly List<float> _keys = new List<float>();
        public int Count => _items.Count;

        public BinaryHeap(int capacity)
        {
            _items.Capacity = capacity;
            _keys.Capacity = capacity;
        }

        public void Push(T item, float key)
        {
            _items.Add(item);
            _keys.Add(key);
            SiftUp(_items.Count - 1);
        }

        public T PopMin()
        {
            int last = _items.Count - 1;
            T min = _items[0];
            _items[0] = _items[last];
            _keys[0] = _keys[last];
            _items.RemoveAt(last);
            _keys.RemoveAt(last);
            if (_items.Count > 0) SiftDown(0);
            return min;
        }

        public void PushOrDecreaseKey(T item, float key)
        {
            int idx = _items.IndexOf(item);
            if (idx >= 0)
            {
                if (key < _keys[idx])
                {
                    _keys[idx] = key;
                    SiftUp(idx);
                }
            }
            else
            {
                Push(item, key);
            }
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (_keys[i] >= _keys[p]) break;
                Swap(i, p);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            int n = _items.Count;
            while (true)
            {
                int l = i * 2 + 1;
                int r = l + 1;
                int s = i;

                if (l < n && _keys[l] < _keys[s]) s = l;
                if (r < n && _keys[r] < _keys[s]) s = r;
                if (s == i) break;
                Swap(i, s);
                i = s;
            }
        }

        private void Swap(int a, int b)
        {
            (_items[a], _items[b]) = (_items[b], _items[a]);
            (_keys[a], _keys[b]) = (_keys[b], _keys[a]);
        }
    }

    public void Follow(Transform t)
    {
        _followTarget = t;
        if (t != null)
        {
            _hasGoal = true;
            _goalWorld = t.position;
            _goalGridPrev = WorldToGrid(_goalWorld);
            _lastRepathTime = -999f;
            _lastProgressPos = transform.position;
            _progressTimer = 0f;
        }
        else
        {
            ClearGoal();
        }
    }

    public void StopFollowing()
    {
        _followTarget = null;
        ClearGoal();
    }
}
