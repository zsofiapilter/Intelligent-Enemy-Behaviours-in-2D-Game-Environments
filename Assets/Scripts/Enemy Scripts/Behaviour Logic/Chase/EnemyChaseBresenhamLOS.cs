using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Chase-BresenhamLOS", menuName = "Enemy Logic/Chase Logic/Bresenham LOS")]
public class EnemyChaseBresenhamLOS : EnemyChaseSOBase
{
    [SerializeField] private float _viewDistance = 10f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private Transform _homePosition;

    private Vector3? _lastKnownPlayerPosition = null;
    private EnemyPathFinding _pathfindingMover;

    public float ViewDistance => _viewDistance;
    public LayerMask ObstacleMask => _obstacleMask;
    public Vector3? LastKnownPlayerPosition => _lastKnownPlayerPosition;

    public Transform Debug_LastTarget { get; private set; }

    public float Debug_ViewRadiusOrFallback => _viewDistance > 0f ? _viewDistance :
    (enemy != null && enemy.ViewDistance > 0f ? enemy.ViewDistance : 8f);


    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        if (enemy.animator != null)
            enemy.animator.SetBool("isChasing", true);

        if (_homePosition == null && enemy.homePosition != null)
            _homePosition = enemy.homePosition;

        _pathfindingMover = enemy.GetComponent<EnemyPathFinding>();
        if (_pathfindingMover == null)
            Debug.LogWarning($"Enemy '{enemy.name}' is missing EnemyPathFinding component.");
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        _lastKnownPlayerPosition = null;

        if (enemy.animator != null)
            enemy.animator.SetBool("isChasing", false);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (_pathfindingMover == null)
        {
            _pathfindingMover = enemy.GetComponent<EnemyPathFinding>();
            if (_pathfindingMover == null) return;
        }

        Transform target = FindWeakestPlayerInView(enemy.transform.position, _viewDistance);

        if (target == null)
            target = PlayerRegistry.GetClosestPlayer(enemy.transform.position);

        if (target == null)
        {
            _pathfindingMover.SetTarget(null);
            return;
        }

        Vector3 enemyPos = enemy.transform.position;
        float distanceToTarget = Vector3.Distance(enemyPos, target.position);

        if (distanceToTarget <= _viewDistance && EnemyCanSeePlayerBresenham(enemyPos, target.position))
        {
            _pathfindingMover.SetTarget(target);
            _lastKnownPlayerPosition = target.position;

            if (enemy.IsWithinStrikingDistance)
                enemy.enemyStateMachine.changeState(enemy.attackState);
        }
        else if (_lastKnownPlayerPosition.HasValue)
        {
            GameObject temp = new GameObject("TempLastSeen");
            temp.transform.position = _lastKnownPlayerPosition.Value;
            _pathfindingMover.SetTarget(temp.transform);

            if (Vector3.Distance(enemyPos, _lastKnownPlayerPosition.Value) < 0.5f)
            {
                _lastKnownPlayerPosition = null;
                GameObject.Destroy(temp);
            }
        }
        else
        {
            _pathfindingMover.SetTarget(_homePosition ? _homePosition : null);
        }
    }

    private Transform FindWeakestPlayerInView(Vector3 from, float viewR)
    {
        var all = Object.FindObjectsByType<MultiplayerHealth>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (all == null || all.Length == 0)
            return null;

        float viewR2 = viewR * viewR;

        var candidates = new List<(Transform tr, MultiplayerHealth hp, float sqrDist)>(all.Length);

        foreach (var mh in all)
        {
            if (mh == null) continue;
            var tr = mh.transform;
            float sq = (tr.position - from).sqrMagnitude;
            if (sq > viewR2) continue;

            if (!EnemyCanSeePlayerBresenham(from, tr.position))
                continue;

            candidates.Add((tr, mh, sq));
        }

        if (candidates.Count == 0)
            return null;

        var best = candidates
            .OrderBy(c => c.hp.currentHealth)
            .ThenBy(c => c.sqrDist)
            .First();

        return best.tr;
    }

    private bool EnemyCanSeePlayerBresenham(Vector3 enemyPos, Vector3 playerPos)
    {
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(enemyPos.x), Mathf.RoundToInt(enemyPos.y));
        Vector2Int end = new Vector2Int(Mathf.RoundToInt(playerPos.x), Mathf.RoundToInt(playerPos.y));

        foreach (Vector2Int point in GetLine(start, end))
        {
            if (point == start || point == end) continue;

            Vector3 worldPoint = new Vector3(point.x, point.y, 0f);
            Collider2D hit = Physics2D.OverlapCircle(worldPoint, 0.4f, _obstacleMask);
            if (hit != null)
                return false;
        }
        return true;
    }

    private List<Vector2Int> GetLine(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new();

        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector2Int(x0, y0));
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
        return line;
    }

    public bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        return EnemyCanSeePlayerBresenham(from, to);
    }

    public List<Vector2Int> SampleLinePoints(Vector3 from, Vector3 to)
    {
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.y));
        Vector2Int end = new Vector2Int(Mathf.RoundToInt(to.x), Mathf.RoundToInt(to.y));
        return GetLine(start, end);
    }

    public Transform DebugPickTarget()
    {
        if (enemy == null) return null;

        float viewR = Debug_ViewRadiusOrFallback;

        Transform weakest = null;
        float weakestHp = float.MaxValue;

        var players = Object.FindObjectsByType<MultiplayerHealth>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector3.Distance(enemy.transform.position, p.transform.position);
            if (d > viewR) continue;
            if (!HasLineOfSight(enemy.transform.position, p.transform.position)) continue;

            if (p.currentHealth < weakestHp)
            {
                weakestHp = p.currentHealth;
                weakest = p.transform;
            }
        }

        Transform target = weakest;
        if (target == null)
        {
            target = PlayerRegistry.GetClosestPlayer(enemy.transform.position);
        }

        Debug_LastTarget = target;
        return target;
    }

    public List<Transform> DebugPlayersInViewLOS()
    {
        var list = new List<Transform>(8);
        if (enemy == null) return list;

        float viewR = Debug_ViewRadiusOrFallback;
        var players = Object.FindObjectsByType<MultiplayerHealth>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector3.Distance(enemy.transform.position, p.transform.position);
            if (d > viewR) continue;
            if (!HasLineOfSight(enemy.transform.position, p.transform.position)) continue;
            list.Add(p.transform);
        }
        return list;
    }
}
