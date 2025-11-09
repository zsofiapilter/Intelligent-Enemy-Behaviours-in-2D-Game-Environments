using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "GOAP/Actions/AcquireLOS")]
public class AcquireLOSAction : GoapActionSO
{
    public float timeout = 3f;

    public override bool Preconditions(GoapAgent a, in WorldState ws) => !ws.HasLOS;
    public override void ApplyEffects(ref WorldState ws) { ws.HasLOS = true; }

    public override IEnumerator Execute(GoapAgent a)
    {
        float t = 0f;

        if (a.CurrentTarget) a.PathChaseTo(a.CurrentTarget);

        while (t < timeout)
        {
            if (!a.CurrentTarget) { a.PathStop(); yield break; }

            bool hasLOS = !Physics2D.Linecast(a.transform.position,
                                              a.CurrentTarget.position,
                                              a.obstacleMask);
            if (hasLOS)
            {
                a.PathStop();
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        a.PathStop();
    }

    bool IsLOS(GoapAgent a)
    {
        var tgt = a.CurrentTarget;
        if (!tgt) return false;
        var hit = Physics2D.Linecast(a.transform.position, tgt.position, a.obstacleMask);
        return !hit;
    }

    protected override void OnEnable() { if (string.IsNullOrWhiteSpace(ActionName)) ActionName = "AcquireLOS"; }
}
