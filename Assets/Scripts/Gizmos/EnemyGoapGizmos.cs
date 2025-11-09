using UnityEngine;

[DefaultExecutionOrder(10000)]
public class EnemyGoapGizmos : MonoBehaviour
{
    public EnemyGoap enemyGoap;
    public EnemyGizmoSettings settings;

    void Reset()
    {
        if (!enemyGoap) enemyGoap = GetComponent<EnemyGoap>();
        if (!settings) settings = EnemyGizmoSettings.Instance;
    }

    void OnDrawGizmos()
    {
        if (!enemyGoap) enemyGoap = GetComponent<EnemyGoap>();
        if (!enemyGoap || !settings) return;
        if (!settings.runtimeEnabled) return;

        var p = enemyGoap.transform.position;

        if (settings.showAttack)
        {
            Gizmos.color = settings.attackColor;
            DrawCircle(p, enemyGoap.AttackDistance);
        }
        if (settings.showAggro)
        {
            Gizmos.color = settings.aggroColor;
            DrawCircle(p, enemyGoap.AggroDistance);
        }
        if (settings.showViewCircle)
        {
            Gizmos.color = settings.viewCircleColor;
            DrawCircle(p, enemyGoap.ViewDistance);
        }
    }

    static void DrawCircle(Vector3 center, float r, int seg = 64)
    {
        if (r <= 0f) return;
        Vector3 prev = center + new Vector3(r, 0f, 0f);
        float dt = Mathf.PI * 2f / seg;
        for (int i = 1; i <= seg; i++)
        {
            float a = i * dt;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
