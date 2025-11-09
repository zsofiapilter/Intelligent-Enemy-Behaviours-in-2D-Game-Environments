using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "GOAP/Actions/ApproachMidRange")]
public class ApproachMidRangeAction : GoapActionSO
{
    public override bool Preconditions(GoapAgent a, in WorldState ws)
        => ws.DistanceBand == DistanceBand.Far;

    public override void ApplyEffects(ref WorldState ws)
        => ws.DistanceBand = DistanceBand.Mid;

    public override IEnumerator Execute(GoapAgent a)
    {
        var tgt = a.CurrentTarget;
        if (!tgt) yield break;

        a.PathChaseTo(tgt);

        float targetMin = a.nearThresh + 0.1f;
        float targetMax = a.midThresh - 0.2f;

        while (true)
        {
            if (!a.CurrentTarget) { a.PathStop(); yield break; }

            bool hasLOS = !Physics2D.Linecast(a.transform.position,
                                              a.CurrentTarget.position,
                                              a.obstacleMask);
            if (!hasLOS)
            {
                a.PathStop();
                yield break;
            }

            float d = Vector2.Distance(a.transform.position, a.CurrentTarget.position);

            if (d >= targetMin && d <= targetMax)
            {
                a.PathStop();
                yield break;
            }

            yield return null;
        }
    }

    protected override void OnEnable() { if (string.IsNullOrWhiteSpace(ActionName)) ActionName = "ApproachMidRange"; }
}
