using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyGoap), typeof(Rigidbody2D))]
public class GoapAgent : MonoBehaviour
{
    [Header("Targeting")]
    public Transform explicitTarget;
    public string playerTag = "Player";

    [Header("Scene/Physics")]
    public LayerMask obstacleMask;
    public float losRaycastSkin = 0.1f;

    [Header("Distance bands (m)")]
    public float nearThresh = 1.8f;
    public float midThresh = 4.0f;

    [Header("Weapon")]
    public Rigidbody2D bulletPrefab;
    public float bulletSpeed = 6f;
    public float fireCooldown = 0.8f;
    float fireCD;
    public bool IsWeaponReady => fireCD <= 0f;
    public float WeaponCooldownRemaining => Mathf.Max(0f, fireCD);

    [Header("Movement")]
    public float moveSpeed = 4f;
    EnemyMovementAStarGoap pathMover;
    Transform followTarget;

    [Header("Avoidance")]
    public float avoidProbe = 0.9f;
    public float sideProbe = 0.5f;
    public float avoidGain = 0.8f;
    public LayerMask solidMask;
    public float stopDistance = 0.05f;

    [Header("Pathfinding (A*)")]
    public EnemyMovementAStarGoap aStar;

    [Header("Orbit Tuning")]
    public float orbitTangential = 2.2f;
    public float orbitKp = 2f, orbitKd = 0.6f;

    [Header("Planning")]
    public List<GoapActionSO> actions;
    public float replanCooldown = 0.5f;

    EnemyGoap enemy;
    Rigidbody2D rb;
    Transform target;
    WorldState ws;
    List<GoapActionSO> plan;
    int planIndex;
    float nextReplan;
    bool isRunning;

    [Header("Shooting LOS")]
    public float shotSkin = 0.08f;
    public float bulletRadius = 0.0f;
    public Transform muzzle;

    public WorldState DebugWorldState => ws;
    public IReadOnlyList<GoapActionSO> DebugPlan => plan;
    public int DebugPlanIndex => planIndex;
    public Transform CurrentTarget => target;

    void Awake()
    {
        enemy = GetComponent<EnemyGoap>();
        rb = GetComponent<Rigidbody2D>();
        if (!aStar) aStar = GetComponent<EnemyMovementAStarGoap>();
        pathMover = GetComponent<EnemyMovementAStarGoap>();
    }

    void Update()
    {
        target = ResolveTarget();
        Sense();

        if (fireCD > 0f) fireCD -= Time.deltaTime;
        ws.WeaponReady = fireCD <= 0f;

        bool criticalSenseChange = (!ws.HasLOS && plan != null && planIndex > 0);
        bool needReplan = plan == null || planIndex >= plan.Count || criticalSenseChange;

        if (!isRunning && needReplan && Time.time >= nextReplan)
        {
            ws.DidShoot = false;
            if (GoapPlanner.Plan(this, ws, actions, out plan)) planIndex = 0;
            nextReplan = Time.time + replanCooldown;
        }

        if (plan != null && planIndex < plan.Count && !isRunning)
            StartCoroutine(Run(plan[planIndex]));
    }

    void LateUpdate()
    {
        if (followTarget && pathMover)
            pathMover.SetGoalPosition(followTarget.position);
    }

    Transform ResolveTarget()
    {
        if (explicitTarget) return explicitTarget;
        var p = GameObject.FindGameObjectWithTag(playerTag);
        return p ? p.transform : null;
    }

    public void PathChaseTo(Transform t)
    {
        if (!pathMover || !t) return;
        followTarget = t;
        pathMover.SetGoalPosition(t.position);
    }

    public void PathChaseTo(Vector2 worldPos)
    {
        if (!pathMover) return;
        followTarget = null;
        pathMover.SetGoalPosition(worldPos);
    }

    public void PathStop()
    {
        if (!pathMover) return;
        followTarget = null;
        pathMover.ClearGoal();
        MoveStop();
    }

    void Sense()
    {
        if (!target) return;

        float d = Vector2.Distance(transform.position, target.position);
        ws.DistanceBand = d < nearThresh ? DistanceBand.Near
                          : (d < midThresh ? DistanceBand.Mid : DistanceBand.Far);

        Vector2 a = transform.position;
        Vector2 b = target.position;
        var hit = Physics2D.Linecast(a, b, obstacleMask);
        ws.HasLOS = !hit;

        ws.LowHP = enemy.currentHealth <= enemy.maxHealth * 0.3f;
    }

    public void MoveTowards(Vector2 worldPos)
    {
        if (!enemy.CanMove) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 to = worldPos - (Vector2)transform.position;
        float dist = to.magnitude;
        if (dist <= stopDistance) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 dir = to / (dist + 1e-6f);
        dir = SteerWithAvoidance(dir);

        rb.linearVelocity = dir * moveSpeed;
        enemy.checkForLeftOrRightFacing(rb.linearVelocity);
    }

    public void MoveStop()
    {
        rb.linearVelocity = Vector2.zero;
    }

    public void OrbitStep(float desiredRadius, bool clockwise)
    {
        if (!target) return;

        Vector2 p = transform.position;
        Vector2 q = target.position;
        Vector2 r = p - q;
        float d = r.magnitude + 1e-5f;
        Vector2 n = r / d;
        Vector2 t = new Vector2(-n.y, n.x);
        if (clockwise) t = -t;

        float e = d - desiredRadius;
        float radial = Mathf.Clamp(orbitKp * e - orbitKd * Vector2.Dot(rb.linearVelocity, n), -moveSpeed, moveSpeed);

        Vector2 vDesired = t * orbitTangential + n * radial;

        Vector2 vDir = SteerWithAvoidance(vDesired.normalized);
        rb.linearVelocity = vDir * moveSpeed;

        enemy.checkForLeftOrRightFacing(rb.linearVelocity);
    }

    Vector2 SteerWithAvoidance(Vector2 desiredDir)
    {
        Vector2 pos = transform.position;

        bool hitFwd = Physics2D.Raycast(pos, desiredDir, avoidProbe, solidMask);
        if (!hitFwd) return desiredDir;

        Vector2 left = new Vector2(-desiredDir.y, desiredDir.x);
        Vector2 right = -left;

        bool hitL = Physics2D.Raycast(pos, desiredDir + left * sideProbe, avoidProbe, solidMask);
        bool hitR = Physics2D.Raycast(pos, desiredDir + right * sideProbe, avoidProbe, solidMask);

        Vector2 steer = desiredDir;
        if (hitL && !hitR) steer += right * avoidGain;
        else if (!hitL && hitR) steer += left * avoidGain;
        else steer += (Random.value < 0.5f ? left : right) * avoidGain;

        return steer.normalized;
    }

    public void ShootOnce()
    {
        if (!CurrentTarget || fireCD > 0f || bulletPrefab == null) return;
        if (!HasClearShot()) return;

        Vector2 dir = (CurrentTarget.position - transform.position).normalized;
        var b = Instantiate(bulletPrefab, muzzle ? muzzle.position : transform.position, Quaternion.identity);
        b.velocity = dir * bulletSpeed;

        if (enemy.animator)
        {
            enemy.animator.ResetTrigger("isHit");
            enemy.animator.SetBool("isAttacking", true);
            StartCoroutine(ResetAttackFlag());
        }

        fireCD = fireCooldown;
    }

    IEnumerator ResetAttackFlag()
    {
        yield return new WaitForSeconds(0.1f);
        if (enemy && enemy.animator) enemy.animator.SetBool("isAttacking", false);
    }

    IEnumerator Run(GoapActionSO act)
    {
        isRunning = true;
        yield return StartCoroutine(act.Execute(this));
        planIndex++;
        isRunning = false;
    }

    public bool HasClearShot()
    {
        if (!CurrentTarget) return false;

        Vector2 a = muzzle ? muzzle.position : transform.position;
        Vector2 b = CurrentTarget.position;

        Vector2 dir = (b - a).normalized;
        float dist = Mathf.Max(0f, Vector2.Distance(a, b) - 2f * shotSkin);
        if (dist <= 0f) return true;

        Vector2 from = a + dir * shotSkin;

        if (bulletRadius > 0f)
        {
            var hit = Physics2D.CircleCast(from, bulletRadius, dir, dist, obstacleMask);
            return !hit;
        }
        else
        {
            var hit = Physics2D.Raycast(from, dir, dist, obstacleMask);
            return !hit;
        }
    }

    string SafeActionName(GoapActionSO a)
        => string.IsNullOrWhiteSpace(a.ActionName) ? a.GetType().Name : a.ActionName;
}
