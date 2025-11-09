using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "GOAP/Actions/Shoot")]
public class ShootAction : GoapActionSO
{
    public override bool Preconditions(GoapAgent a, in WorldState ws)
        => ws.HasLOS && ws.WeaponReady && ws.DistanceBand != DistanceBand.Far;

    public override void ApplyEffects(ref WorldState ws)
    {
        ws.WeaponReady = false;
        ws.DidShoot = true;
    }

    public override IEnumerator Execute(GoapAgent a)
    {
        if (!a.HasClearShot())
            yield break;

        a.ShootOnce();
        yield return new WaitForSeconds(0.05f);
    }

    protected override void OnEnable() { if (string.IsNullOrWhiteSpace(ActionName)) ActionName = "Shoot"; }
}
