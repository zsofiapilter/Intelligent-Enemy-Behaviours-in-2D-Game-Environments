using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyRuntimeGizmos : MonoBehaviour
{
    Enemy enemy;
    EnemyAttackGather gather;
    IEnemyRanges ranges;

    LineRenderer aggroLR, attackLR, viewCircleLR, attackBonusLR;
    LineRenderer ringCurrentLR, ringDesiredLR, ringMinLR;
    LineRenderer steerLR, slotCircleLR, slotLineLR, sepLR;

    static Material sMat;
    const string Debug = "Default";
    const int SortingOrder = 5000;

    [Header("If Enemy doesn't expose radii, it sets these here")]
    public float aggroDistance = 0f;
    public float attackDistance = 0f;
    public float viewCircle = 0f;

    EnemyGizmoSettings S
        => (RuntimeGizmoManager.Instance ? RuntimeGizmoManager.Instance.settings : EnemyGizmoSettings.Instance);

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        ranges = GetComponent<IEnemyRanges>();
        gather = enemy ? enemy.enemyAttackBaseInstance as EnemyAttackGather : null;

        aggroLR = MakeLR("AggroLR");
        attackLR = MakeLR("AttackLR");
        viewCircleLR = MakeLR("viewCircleLR");
        attackBonusLR = MakeLR("AttackBonusLR");

        ringCurrentLR = MakeLR("RingCurrentLR");
        ringDesiredLR = MakeLR("RingDesiredLR");
        ringMinLR = MakeLR("RingMinLR");

        steerLR = MakeLR("SteerLR");
        slotCircleLR = MakeLR("SlotCircleLR");
        slotLineLR = MakeLR("SlotLineLR");
        sepLR = MakeLR("SeparationLR");
    }

    LineRenderer MakeLR(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = GetMat();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 0;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;
        lr.alignment = LineAlignment.View;
        lr.sortingLayerName = Debug;
        lr.sortingOrder = SortingOrder;
        return lr;
    }

    Material GetMat()
    {
        if (sMat == null)
        {
            sMat = Resources.Load<Material>("RuntimeGizmoMat");
            if (sMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                sMat = new Material(sh);
            }
        }
        return sMat;
    }

    void OnDisable() { HideAll(); }

    void LateUpdate()
    {
        var S = (RuntimeGizmoManager.Instance ? RuntimeGizmoManager.Instance.settings : EnemyGizmoSettings.Instance);
        if (S == null || !S.drawAll || !S.runtimeEnabled) { HideAll(); return; }

        float viewFromChase = 0f;
        var losChase = enemy ? enemy.enemyChaseBaseInstance as EnemyChaseLineOfSight : null;
        if (losChase != null) viewFromChase = Mathf.Max(0f, losChase.ViewDistance);

        if (ranges == null) ranges = GetComponent<IEnemyRanges>();
        if (ranges != null)
        {
            if (aggroDistance <= 0f && ranges.AggroDistance > 0f) aggroDistance = ranges.AggroDistance;
            if (attackDistance <= 0f && ranges.AttackDistance > 0f) attackDistance = ranges.AttackDistance;

            if (viewFromChase <= 0f && viewCircle <= 0f && ranges.ViewDistance > 0f)
                viewCircle = ranges.ViewDistance;
        }

        if (enemy != null)
        {
            if (aggroDistance <= 0f && enemy.AttackDistance > 0f) aggroDistance = enemy.AggroDistance;
            if (attackDistance <= 0f && enemy.AttackDistance > 0f) attackDistance = enemy.AggroDistance;
            if (viewFromChase <= 0f && viewCircle <= 0f && enemy.ViewDistance > 0f)
                viewCircle = enemy.ViewDistance;
        }

        if (viewFromChase > 0f) viewCircle = viewFromChase;

        float w = S.lineWidth;
        int seg = Mathf.Max(8, S.circleSegments);
        Vector3 me = transform.position;

        if (S.showAggro && aggroDistance > 0f)
            DrawCircle(aggroLR, me, aggroDistance, seg, w, S.aggroColor, true);
        else aggroLR.positionCount = 0;

        if (S.showAttack && attackDistance > 0f)
            DrawCircle(attackLR, me, attackDistance, seg, w, S.attackColor, true);
        else attackLR.positionCount = 0;

        if (S.showViewCircle && viewCircle > 0f)
            DrawCircle(viewCircleLR, me, viewCircle, seg, w, S.viewCircleColor, true);
        else viewCircleLR.positionCount = 0;

        bool hasGather = (gather != null);
        if (!hasGather)
        {
            ringCurrentLR.positionCount = 0;
            ringDesiredLR.positionCount = 0;
            ringMinLR.positionCount = 0;
            slotCircleLR.positionCount = 0;
            slotLineLR.positionCount = 0;
            steerLR.positionCount = 0;
            sepLR.positionCount = 0;
            attackBonusLR.positionCount = 0;
            return;
        }

        Transform player = enemy.GetPlayer();
        Vector3 p = player ? (Vector3)player.position : me;

        if (S.showGatherRing)
        {
            DrawCircle(ringCurrentLR, p, gather.CurrentRingRadius, seg, w, S.ringColor, gather.CurrentRingRadius > 0f);

            var desiredCol = S.ringColor; desiredCol.a = 0.25f;
            DrawCircle(ringDesiredLR, p, gather.DesiredRingRadius, seg, w, desiredCol, gather.DesiredRingRadius > 0f);

            var minCol = S.ringColor; minCol.a = 0.15f;
            DrawCircle(ringMinLR, p, gather.MinRingRadius, seg, w, minCol, gather.MinRingRadius > 0f);
        }
        else
        {
            ringCurrentLR.positionCount = 0;
            ringDesiredLR.positionCount = 0;
            ringMinLR.positionCount = 0;
        }

        if (S.showSlotTarget)
        {
            Vector3 slot = GetSlotWorldOrSelf(gather, me);
            DrawCircle(slotCircleLR, slot, 0.12f, 16, w, S.slotColor, true);

            DrawLine(slotLineLR, me, slot, w, S.slotColor);
        }
        else
        {
            slotCircleLR.positionCount = 0;
            slotLineLR.positionCount = 0;
        }

        if (S.showSteeringVector)
        {
            Vector2 v = GetSteeringOrZero(gather);
            DrawLine(steerLR, me, me + (Vector3)(v * Mathf.Max(0.01f, S.vectorScale)), w, S.steeringColor);
        }
        else steerLR.positionCount = 0;

        if (S.showSeparationRadius && gather.SeparationRadius > 0f)
            DrawCircle(sepLR, me, gather.SeparationRadius, 24, w, S.separationColor, true);
        else sepLR.positionCount = 0;

        if (S.showAttackRange && gather.AttackRange > 0f)
            DrawCircle(attackBonusLR, me, gather.AttackRange, 32, w, S.attackRangeColor, true);
        else attackBonusLR.positionCount = 0;
    }

    void HideAll()
    {
        aggroLR.positionCount = 0;
        attackLR.positionCount = 0;
        attackBonusLR.positionCount = 0;
        ringCurrentLR.positionCount = 0;
        ringDesiredLR.positionCount = 0;
        ringMinLR.positionCount = 0;
        steerLR.positionCount = 0;
        slotCircleLR.positionCount = 0;
        slotLineLR.positionCount = 0;
        sepLR.positionCount = 0;
        viewCircleLR.positionCount = 0;
    }

    void DrawCircle(LineRenderer lr, Vector3 center, float radius, int segments, float width, Color col, bool show)
    {
        if (!show || radius <= 0f) { lr.positionCount = 0; return; }
        lr.loop = true;
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = col;
        if (lr.positionCount != segments) lr.positionCount = segments;

        float step = 2f * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float a = step * i;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
        }
    }

    void DrawLine(LineRenderer lr, Vector3 a, Vector3 b, float width, Color col)
    {
        lr.loop = false;
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = col;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
    }

    Vector2 GetSteeringOrZero(EnemyAttackGather g)
    {
        return g != null ? g.DebugSteering : Vector2.zero;
    }

    Vector3 GetSlotWorldOrSelf(EnemyAttackGather g, Vector3 fallback)
    {
        return g != null ? (Vector3)g.DebugSlotTarget : fallback;
    }
}
