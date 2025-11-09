using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Chase-LineOfSight", menuName = "Enemy Logic/Chase Logic/Line Of Sight")]
public class EnemyChaseLineOfSight : EnemyChaseSOBase
{
    [Header("Vision & Layers")]
    [SerializeField, Min(0.01f)] private float _viewDistance = 8f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private Transform _homePosition;

    [Header("Attack Approach")]
    [Tooltip("How far inside the attack radius we aim so A* doesn't stop short.")]
    [SerializeField, Min(0.01f)] private float _attackInnerFactor = 0.35f;
    [SerializeField, Min(0f)] private float _attackEpsilon = 0.05f;

    [Header("Dot Breadcrumbing")]
    [Tooltip("Distance at which we consider the current dot reached and immediately pick the next one.")]
    [SerializeField, Min(0.05f)] private float _dotReachThreshold = 0.35f;

    [Tooltip("Minimum time between forced retargets, to avoid thrashing.")]
    [SerializeField, Min(0f)] private float _repathInterval = 0.20f;

    [Tooltip("Weighting towards the player when picking a dot (higher = more 'towards player').")]
    [SerializeField, Min(0f)] private float _weightToPlayer = 1.0f;

    [Tooltip("Secondary tie-breaker: prefer dots closer to us (keeps steps short/smooth).")]
    [SerializeField, Min(0f)] private float _weightToEnemy = 0.15f;

    private EnemyMovementAStar _astar;

    private Vector3 _currentDotGoal;
    private bool _hasDotGoal = false;
    private float _lastRepathAt = -999f;

    public float ViewDistance
    {
        get => _viewDistance;
        set => _viewDistance = Mathf.Max(0.01f, value);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        if (enemy.animator) enemy.animator.SetBool("isChasing", true);

        if (_homePosition == null && enemy.homePosition != null)
            _homePosition = enemy.homePosition;

        _astar = enemy.GetComponent<EnemyMovementAStar>();
        if (_astar == null)
            Debug.LogWarning($"Enemy '{enemy.name}' is missing EnemyMovementAStar component.");

        _hasDotGoal = false;
        _lastRepathAt = -999f;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        if (enemy.animator) enemy.animator.SetBool("isChasing", false);
        _astar?.ClearGoal();
        _hasDotGoal = false;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (playerTransform == null)
        {
            var p = PlayerRegistry.GetClosestPlayer(transform.position);
            if (p != null) playerTransform = p.transform; else return;
        }

        Vector3 enemyPos = enemy.transform.position;
        Vector3 playerPos = playerTransform.position;
        float distEP = Vector3.Distance(enemyPos, playerPos);

        float aggroDist = GetAggroDistanceFallback(6f);
        float attackDist = GetAttackDistanceFallback(1.5f);

        if (distEP <= aggroDist)
        {
            Vector3 target = ComputeInsideAttackTarget(enemyPos, playerPos, attackDist, _attackInnerFactor);
            _astar?.SetGoalPosition(target);

            if (distEP <= (attackDist + _attackEpsilon) || enemy.IsWithinStrikingDistance)
                enemy.enemyStateMachine.changeState(enemy.attackState);

            _hasDotGoal = false;
            return;
        }

        if (LosManager.Instance != null)
        {
            if (!_hasDotGoal ||
                Vector3.Distance(enemyPos, _currentDotGoal) <= _dotReachThreshold ||
                (Time.time - _lastRepathAt) >= _repathInterval)
            {
                if (TryPickBestDotTowardsPlayer(enemyPos, playerPos, out Vector3 nextDot))
                {
                    _currentDotGoal = nextDot;
                    _hasDotGoal = true;
                    _lastRepathAt = Time.time;

                    _astar?.SetGoalPosition(_currentDotGoal);
                }
                else
                {
                    _hasDotGoal = false;
                }
            }
            else
            {
                _astar?.SetGoalPosition(_currentDotGoal);
            }

            if (distEP <= (attackDist + _attackEpsilon) || enemy.IsWithinStrikingDistance)
                enemy.enemyStateMachine.changeState(enemy.attackState);

            if (_hasDotGoal) return;
        }

        if (_homePosition != null)
        {
            float distHome = Vector3.Distance(enemyPos, _homePosition.position);
            if (distHome > 0.1f) _astar?.SetGoalPosition(_homePosition.position);
            else _astar?.ClearGoal();
        }
        else
        {
            _astar?.ClearGoal();
        }
    }

    private Vector3 ComputeInsideAttackTarget(Vector3 enemyPos, Vector3 playerPos, float attackDist, float innerFactor)
    {
        float d = Vector3.Distance(enemyPos, playerPos);
        if (d <= attackDist) return playerPos;

        Vector3 dirPlayerToEnemy = (enemyPos - playerPos).normalized;
        float offset = Mathf.Max(attackDist * Mathf.Clamp01(innerFactor), 0.05f);
        return playerPos + dirPlayerToEnemy * offset;
    }

    private bool TryPickBestDotTowardsPlayer(Vector3 enemyPos, Vector3 playerPos, out Vector3 best)
    {
        best = default;
        var los = LosManager.Instance;
        if (los == null) return false;

        var tiles = los.GetVisibleTileWorldPositions();
        if (tiles == null || tiles.Count == 0) return false;

        float vd = _viewDistance;
        float bestScore = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < tiles.Count; i++)
        {
            Vector3 tile = tiles[i];

            Vector3 toTile = tile - enemyPos;
            float dist = toTile.magnitude;
            if (dist > vd || dist <= Mathf.Epsilon) continue;

            if (Physics2D.Raycast(enemyPos, toTile.normalized, dist, _obstacleMask))
                continue;

            float s =
                (tile - playerPos).sqrMagnitude * _weightToPlayer +
                (tile - enemyPos).sqrMagnitude * _weightToEnemy;

            if (s < bestScore)
            {
                bestScore = s;
                best = tile;
                found = true;
            }
        }

        return found;
    }

    private float GetAggroDistanceFallback(float fallback)
    {
        var ranges = enemy.GetComponent<IEnemyRanges>();
        if (ranges != null && ranges.AggroDistance > 0f) return ranges.AggroDistance;

        var fi = typeof(Enemy).GetField("aggroDistance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fi != null)
        {
            try { float v = (float)fi.GetValue(enemy); if (v > 0f) return v; } catch { }
        }
        return fallback;
    }

    private float GetAttackDistanceFallback(float fallback)
    {
        var ranges = enemy.GetComponent<IEnemyRanges>();
        if (ranges != null && ranges.AttackDistance > 0f) return ranges.AttackDistance;

        var fi = typeof(Enemy).GetField("attackDistance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fi != null)
        {
            try { float v = (float)fi.GetValue(enemy); if (v > 0f) return v; } catch { }
        }
        return fallback;
    }
}
