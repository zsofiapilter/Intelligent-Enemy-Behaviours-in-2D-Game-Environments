using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackGather", menuName = "Enemy Logic/Attack Logic/Gather Surround")]
public class EnemyAttackGather : EnemyAttackSOBase
{
    [Header("Ring")]
    [SerializeField] private float desiredRingRadius = 3f;
    [SerializeField] private float minRingRadius = 1.2f;
    [SerializeField] private float shrinkSpeed = 0.4f;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float separationRadius = 0.8f;
    [SerializeField] private float slotArrivalThreshold = 1f;

    [Header("Flocking Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float cohesionWeight = 0.6f;
    [SerializeField] private float alignWeight = 0.4f;
    [SerializeField] private float slotWeight = 2.0f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float attackRangeBonus = 0.3f;

    [System.NonSerialized] private bool _defaultsCaptured;
    [System.NonSerialized] private float d_desiredRingRadius, d_minRingRadius, d_shrinkSpeed;
    [System.NonSerialized] private float d_maxSpeed, d_separationRadius, d_slotArrivalThreshold;
    [System.NonSerialized] private float d_separationWeight, d_cohesionWeight, d_alignWeight, d_slotWeight;
    [System.NonSerialized] private float d_attackDamage, d_attackCooldown, d_attackRangeBonus;


    private float atkTimer;

    // Sqad handle
    private static readonly Dictionary<Transform, List<Enemy>> squadsByPlayer = new();
    private static readonly Dictionary<Enemy, int> slotIndexByEnemy = new();
    private static readonly HashSet<Transform> dirtyPlayers = new();

    private Transform lastPlayerForMembership;

    #region Private runtime state
    private Vector2 debugSlotTarget;
    private Vector2 steering;
    private float currentRingRadius;
    #endregion

    #region Tunable by UI (get/set)
    public float DesiredRingRadius
    {
        get => desiredRingRadius;
        set { desiredRingRadius = Mathf.Max(0.1f, value); EnsureConsistency(); }
    }
    public float MinRingRadius
    {
        get => minRingRadius;
        set { minRingRadius = Mathf.Max(0.05f, Mathf.Min(value, desiredRingRadius - 0.05f)); EnsureConsistency(); }
    }
    public float ShrinkSpeed { get => shrinkSpeed; set => shrinkSpeed = Mathf.Max(0f, value); }
    public float MaxSpeed { get => maxSpeed; set => maxSpeed = Mathf.Max(0f, value); }
    public float SeparationRadius { get => separationRadius; set => separationRadius = Mathf.Max(0f, value); }
    public float SlotArrivalThreshold { get => slotArrivalThreshold; set => slotArrivalThreshold = Mathf.Max(0f, value); }

    public float SeparationWeight { get => separationWeight; set => separationWeight = Mathf.Max(0f, value); }
    public float CohesionWeight { get => cohesionWeight; set => cohesionWeight = Mathf.Max(0f, value); }
    public float AlignWeight { get => alignWeight; set => alignWeight = Mathf.Max(0f, value); }
    public float SlotWeight { get => slotWeight; set => slotWeight = Mathf.Max(0f, value); }

    public float AttackDamage { get => attackDamage; set => attackDamage = Mathf.Max(0f, value); }
    public float AttackCooldown { get => attackCooldown; set => attackCooldown = Mathf.Max(0.01f, value); }
    public float AttackRangeBonus { get => attackRangeBonus; set => attackRangeBonus = Mathf.Max(0f, value); }
    #endregion

    #region Debug (read-only)
    public Vector2 DebugSlotTarget => debugSlotTarget;
    public Vector2 DebugSteering => steering;
    public float CurrentRingRadius => currentRingRadius;
    public float AttackRange => minRingRadius + attackRangeBonus;
    #endregion

    #region State Machine Hooks
    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        currentRingRadius = desiredRingRadius;
        EnsureConsistency();
        atkTimer = 0f;

        lastPlayerForMembership = null;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        LeaveCurrentSquad();
        steering = Vector2.zero;
        atkTimer = 0f;
    }

    public override void ResetValues()
    {
        base.ResetValues();
        steering = Vector2.zero;
        atkTimer = 0f;
        currentRingRadius = Mathf.Clamp(desiredRingRadius, minRingRadius, desiredRingRadius);
    }
    #endregion

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        playerTransform = enemy.GetPlayer();
        if (playerTransform == null)
        {
            enemy.moveEnemy(Vector2.zero);
            enemy.animator.SetBool("isAttacking", false);
            LeaveCurrentSquad();
            return;
        }

        RefreshSquadMembership(playerTransform);

        UpdateRingRadius();

        var squad = GetSquad(playerTransform);
        int N = Mathf.Max(squad.Count, 1);
        int myIndex = GetMyIndex(playerTransform, squad);

        Vector2 pos = enemy.transform.position;
        float slotAngle = (360f / N) * myIndex * Mathf.Deg2Rad;
        Vector2 slotTarget = (Vector2)playerTransform.position +
                             new Vector2(Mathf.Cos(slotAngle), Mathf.Sin(slotAngle)) *
                             currentRingRadius;

        float distanceToSlot = Vector2.Distance(pos, slotTarget);
        debugSlotTarget = slotTarget;

        if (distanceToSlot > slotArrivalThreshold)
        {
            ComputeSteering(playerTransform, squad, pos, slotTarget);
            enemy.moveEnemy(steering);
            enemy.animator.SetBool("isAttacking", false);
        }
        else
        {
            enemy.moveEnemy(Vector2.zero);
            enemy.animator.SetBool("isAttacking", true);
        }

        TryDamagePlayer();
    }

    private void UpdateRingRadius()
    {
        float target = Mathf.Max(minRingRadius, desiredRingRadius);
        currentRingRadius = Mathf.MoveTowards(currentRingRadius, target, shrinkSpeed * Time.deltaTime);
        currentRingRadius = Mathf.Clamp(currentRingRadius, minRingRadius, desiredRingRadius);
    }

    public void RuntimeReinit(bool resetRing = true)
    {
        if (resetRing)
            currentRingRadius = Mathf.Clamp(desiredRingRadius, minRingRadius, desiredRingRadius);
        atkTimer = 0f;
    }

    public void ApplyDebugParamChange(bool resetRing = true)
    {
        EnsureConsistency();
        if (resetRing)
            currentRingRadius = Mathf.Clamp(desiredRingRadius, minRingRadius, desiredRingRadius);
        atkTimer = 0f;
    }

    private void EnsureConsistency()
    {
        if (desiredRingRadius < minRingRadius + 0.05f)
            desiredRingRadius = minRingRadius + 0.05f;

        currentRingRadius = Mathf.Clamp(currentRingRadius, minRingRadius, desiredRingRadius);
    }

    private void ComputeSteering(Transform player, List<Enemy> squad, Vector2 myPos, Vector2 slotTarget)
    {
        int removed = squad.RemoveAll(m => m == null);
        if (removed > 0) MarkDirty(player);

        Vector2 toSlot = (slotTarget - myPos).normalized;

        Vector2 separation = Vector2.zero, cohesion = Vector2.zero, alignment = Vector2.zero;
        int neighbors = 0;

        foreach (Enemy mate in squad)
        {
            if (mate == null || mate == enemy) continue;

            Vector2 diff = (Vector2)mate.transform.position - myPos;
            float dist = diff.magnitude;

            if (dist < separationRadius)
                separation -= diff / Mathf.Max(dist, 0.0001f);

            if (dist < currentRingRadius * 2f)
            {
                cohesion += (Vector2)mate.transform.position;
                alignment += mate.rb.linearVelocity;
                neighbors++;
            }
        }

        if (neighbors > 0)
        {
            cohesion = (cohesion / neighbors - myPos).normalized;
            alignment = (alignment / neighbors).normalized;
        }

        steering = separation * separationWeight
                 + cohesion * cohesionWeight
                 + alignment * alignWeight
                 + toSlot * slotWeight;

        steering = Vector2.ClampMagnitude(steering, maxSpeed);
    }

    private void TryDamagePlayer()
    {
        atkTimer += Time.deltaTime;
        if (atkTimer < attackCooldown) return;

        Vector2 attackOrigin = enemy.transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackOrigin,
            AttackRange,
            LayerMask.GetMask("Player"));

        foreach (var hit in hits)
        {
            IDamagable dmg = hit.GetComponent<IDamagable>();
            if (dmg != null)
            {
                dmg.Damage(attackDamage);
                atkTimer = 0f;
                return;
            }
        }
    }

    private static List<Enemy> GetSquad(Transform player)
    {
        if (player == null) return EmptyList;
        if (!squadsByPlayer.TryGetValue(player, out var list))
        {
            list = new List<Enemy>(8);
            squadsByPlayer[player] = list;
            dirtyPlayers.Add(player);
        }
        return list;
    }

    private void RefreshSquadMembership(Transform currentPlayer)
    {
        if (currentPlayer == lastPlayerForMembership)
        {
            EnsureInSquad(currentPlayer);
            return;
        }

        if (lastPlayerForMembership != null)
        {
            var oldSquad = GetSquad(lastPlayerForMembership);
            if (oldSquad.Remove(enemy)) MarkDirty(lastPlayerForMembership);
            slotIndexByEnemy.Remove(enemy);
        }

        lastPlayerForMembership = currentPlayer;
        EnsureInSquad(currentPlayer);
    }

    private void EnsureInSquad(Transform player)
    {
        var squad = GetSquad(player);
        if (!squad.Contains(enemy))
        {
            squad.Add(enemy);
            MarkDirty(player);
        }
    }

    private void LeaveCurrentSquad()
    {
        if (lastPlayerForMembership == null) return;
        var squad = GetSquad(lastPlayerForMembership);
        if (squad.Remove(enemy)) MarkDirty(lastPlayerForMembership);
        slotIndexByEnemy.Remove(enemy);
        lastPlayerForMembership = null;
    }

    private static void MarkDirty(Transform player)
    {
        if (player != null) dirtyPlayers.Add(player);
    }

    private static readonly List<Enemy> EmptyList = new(0);

    private static void ReindexIfDirty(Transform player, List<Enemy> squad)
    {
        if (player == null || !dirtyPlayers.Contains(player)) return;

        squad.RemoveAll(m => m == null);
        squad.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

        for (int i = 0; i < squad.Count; i++)
            slotIndexByEnemy[squad[i]] = i;

        dirtyPlayers.Remove(player);
    }

    private int GetMyIndex(Transform player, List<Enemy> squad)
    {
        ReindexIfDirty(player, squad);
        if (!slotIndexByEnemy.TryGetValue(enemy, out int idx))
        {
            MarkDirty(player);
            ReindexIfDirty(player, squad);
            if (!slotIndexByEnemy.TryGetValue(enemy, out idx))
            {
                if (!squad.Contains(enemy)) { squad.Add(enemy); MarkDirty(player); ReindexIfDirty(player, squad); }
                slotIndexByEnemy.TryGetValue(enemy, out idx);
            }
        }
        return Mathf.Max(0, idx);
    }

    private void OnEnable()
    {
        CaptureDefaultsIfNeeded();
    }

    private void CaptureDefaultsIfNeeded()
    {
        if (_defaultsCaptured) return;

        d_desiredRingRadius = desiredRingRadius;
        d_minRingRadius = minRingRadius;
        d_shrinkSpeed = shrinkSpeed;

        d_maxSpeed = maxSpeed;
        d_separationRadius = separationRadius;
        d_slotArrivalThreshold = slotArrivalThreshold;

        d_separationWeight = separationWeight;
        d_cohesionWeight = cohesionWeight;
        d_alignWeight = alignWeight;
        d_slotWeight = slotWeight;

        d_attackDamage = attackDamage;
        d_attackCooldown = attackCooldown;
        d_attackRangeBonus = attackRangeBonus;

        _defaultsCaptured = true;
    }

    public void ResetToDefaults(bool reinit = true)
    {
        CaptureDefaultsIfNeeded();

        DesiredRingRadius = d_desiredRingRadius;
        MinRingRadius = d_minRingRadius;
        ShrinkSpeed = d_shrinkSpeed;

        MaxSpeed = d_maxSpeed;
        SeparationRadius = d_separationRadius;
        SlotArrivalThreshold = d_slotArrivalThreshold;

        SeparationWeight = d_separationWeight;
        CohesionWeight = d_cohesionWeight;
        AlignWeight = d_alignWeight;
        SlotWeight = d_slotWeight;

        AttackDamage = d_attackDamage;
        AttackCooldown = d_attackCooldown;
        AttackRangeBonus = d_attackRangeBonus;

        if (reinit) RuntimeReinit(true);
    }
}
