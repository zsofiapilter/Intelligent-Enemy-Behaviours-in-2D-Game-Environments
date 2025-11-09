using UnityEngine;

public class EnemyRangesRuntimeGizmos : MonoBehaviour
{
    IEnemyRanges ranges;

    LineRenderer aggroLR, attackLR, viewLR;

    static Material sMat;
    const string SortingLayerName = "Default";
    const int SortingOrder = 5000;

    [Header("Fallbacks if IEnemyRanges not present (>0 overrides)")]
    public float aggroDistance = 0f;
    public float attackDistance = 0f;
    public float viewDistance = 0f;

    EnemyGizmoSettings S => (RuntimeGizmoManager.Instance
        ? RuntimeGizmoManager.Instance.settings
        : EnemyGizmoSettings.Instance);

    void Awake()
    {
        ranges = GetComponent<IEnemyRanges>();

        aggroLR = MakeLR("AggroLR");
        attackLR = MakeLR("AttackLR");
        viewLR = MakeLR("ViewLR");
    }

    void OnDisable()
    {
        Hide(aggroLR);
        Hide(attackLR);
        Hide(viewLR);
    }

    void LateUpdate()
    {
        var s = S;
        if (s == null || !s.runtimeEnabled || !s.drawAll)
        {
            Hide(aggroLR);
            Hide(attackLR);
            Hide(viewLR);
            return;
        }

        float agg = aggroDistance;
        float atk = attackDistance;
        float vew = viewDistance;

        if (ranges != null)
        {
            if (agg <= 0f) agg = ranges.AggroDistance;
            if (atk <= 0f) atk = ranges.AttackDistance;
            if (vew <= 0f) vew = ranges.ViewDistance;
        }

        Vector3 me = transform.position;
        int seg = Mathf.Max(12, s.circleSegments);
        float w = s.lineWidth;

        if (s.showAggro && agg > 0f) DrawCircle(aggroLR, me, agg, seg, w, s.aggroColor);
        else Hide(aggroLR);

        if (s.showAttack && atk > 0f) DrawCircle(attackLR, me, atk, seg, w, s.attackColor);
        else Hide(attackLR);

        if (s.showViewCircle && vew > 0f) DrawCircle(viewLR, me, vew, seg, w, s.viewCircleColor);
        else Hide(viewLR);
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
        lr.sortingLayerName = SortingLayerName;
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

    void DrawCircle(LineRenderer lr, Vector3 center, float radius, int segments, float width, Color col)
    {
        if (radius <= 0f || segments < 3) { Hide(lr); return; }

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

    void Hide(LineRenderer lr)
    {
        if (lr) lr.positionCount = 0;
    }
}