using UnityEngine;

public class LineOfSightDebugDrawer : MonoBehaviour
{
    public Enemy enemy;
    public EnemyGizmoSettings settings;
    public EnemyChaseLineOfSight chaseSO;
    public bool showViewCircle = true;

    void OnDrawGizmos()
    {
        if (!showViewCircle || !settings || !settings.showViewCircle) return;

        if (!enemy) enemy = GetComponent<Enemy>();
        var active = enemy ? enemy.enemyChaseBaseInstance as EnemyChaseLineOfSight : null;
        var src = active != null ? active : chaseSO;

        if (!enemy || src == null) return;

        Gizmos.color = settings.viewCircleColor;
        Gizmos.DrawWireSphere(enemy.transform.position, src.ViewDistance);
    }
}

