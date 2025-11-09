using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "GOAP/Actions/StrafeOrbit")]
public class StrafeOrbitAction : GoapActionSO
{
    public bool clockwise = false;

    public override bool Preconditions(GoapAgent a, in WorldState ws)
        => ws.HasLOS && (ws.DistanceBand == DistanceBand.Near || ws.DistanceBand == DistanceBand.Mid);

    public override void ApplyEffects(ref WorldState ws) { }

    public override IEnumerator Execute(GoapAgent a)
    {
        var tgt = a.CurrentTarget;
        if (!tgt) yield break;

        float desired = Mathf.Clamp((a.nearThresh + a.midThresh) * 0.5f, a.nearThresh + 0.1f, a.midThresh - 0.1f);
        float t = 0f, dur = 0.8f;

        while (t < dur)
        {
            a.OrbitStep(desired, clockwise);
            t += Time.deltaTime;
            yield return null;
        }
        a.MoveStop();
    }

    protected override void OnEnable() { if (string.IsNullOrWhiteSpace(ActionName)) ActionName = "StrafeOrbit"; }
}
