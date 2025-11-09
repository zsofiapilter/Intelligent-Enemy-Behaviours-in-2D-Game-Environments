using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Enemy))]
public class EnemyGatherGizmos : MonoBehaviour
{
    public EnemyGizmoSettings settings;
    private Enemy enemy;

    private void Awake() { enemy = GetComponent<Enemy>(); }
    private EnemyGizmoSettings S => settings != null ? settings : EnemyGizmoSettings.Instance;

    void OnDrawGizmos()
    {
        var s = S; if (s == null || !s.drawAll || !s.drawUnselected) return;
        DrawGizmosImpl();
    }
    void OnDrawGizmosSelected()
    {
        var s = S; if (s == null || !s.drawAll) return;
        DrawGizmosImpl();
    }

    void DrawGizmosImpl()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
        if (enemy == null) return;

        var s = S;
        var gather = enemy.enemyAttackBaseInstance as EnemyAttackGather;
        if (gather == null) return;

        var player = enemy.GetPlayer();
        if (player && s.showGatherRing)
        {
            Gizmos.color = s.ringColor;
            Gizmos.DrawWireSphere(player.position, gather.CurrentRingRadius);

            Gizmos.color = new Color(s.ringColor.r, s.ringColor.g, s.ringColor.b, 0.25f);
            Gizmos.DrawWireSphere(player.position, gather.DesiredRingRadius);

            Gizmos.color = new Color(s.ringColor.r, s.ringColor.g, s.ringColor.b, 0.15f);
            Gizmos.DrawWireSphere(player.position, gather.MinRingRadius);
        }

        if (s.showSlotTarget)
        {
            Gizmos.color = s.slotColor;
            Vector3 slot = gather.DebugSlotTarget;
            Gizmos.DrawSphere(slot, 0.08f);
            Gizmos.DrawLine(transform.position, slot);
        }

        if (s.showSeparationRadius)
        {
            Gizmos.color = s.separationColor;
            Gizmos.DrawWireSphere(transform.position, gather.SeparationRadius);
        }

        if (s.showAttackRange)
        {
            Gizmos.color = s.attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, gather.AttackRange);
        }

#if UNITY_EDITOR
    if (s.showSteeringVector)
    {
        Gizmos.color = s.steeringColor;
        Vector3 from = transform.position;
        Vector3 v = (Vector3)gather.DebugSteering;
        Vector3 to = from + v * 0.5f;
        Gizmos.DrawLine(from, to);
        UnityEditor.Handles.ArrowHandleCap(0, to, Quaternion.LookRotation(Vector3.forward, v), 0.35f, EventType.Repaint);
    }
#endif
    }
}
