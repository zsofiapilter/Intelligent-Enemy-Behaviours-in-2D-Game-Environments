using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyCommonGizmos : MonoBehaviour
{
    public EnemyGizmoSettings settings;
    private Enemy enemy;

    private void Awake() { enemy = GetComponent<Enemy>(); }

    private EnemyGizmoSettings S => settings != null ? settings : EnemyGizmoSettings.Instance;

    private void OnDrawGizmosSelected()
    {
        var s = S;
        if (s == null || !s.drawAll) return;
        if (enemy == null) enemy = GetComponent<Enemy>();
        if (enemy == null) return;

        if (s.showAggro)
        {
            Gizmos.color = s.aggroColor;
            Gizmos.DrawWireSphere(transform.position, GetAggroDistance());
        }
        if (s.showAttack)
        {
            Gizmos.color = s.attackColor;
            Gizmos.DrawWireSphere(transform.position, GetAttackDistance());
        }

        if (s.showViewCircle)
        {
            Gizmos.color = s.viewCircleColor;
            Gizmos.DrawWireSphere(transform.position, GetViewDistance());
        }
    }

    private float GetAggroDistance()
    {
        var fi = typeof(Enemy).GetField("aggroDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return fi != null ? (float)fi.GetValue(enemy) : 5f;
    }

    private float GetAttackDistance()
    {
        var fi = typeof(Enemy).GetField("attackDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return fi != null ? (float)fi.GetValue(enemy) : 1.5f;
    }

    private float GetViewDistance()
    {
        var fi = typeof(Enemy).GetField("viewCircle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return fi != null ? (float)fi.GetValue(enemy) : 8f;
    }
}
