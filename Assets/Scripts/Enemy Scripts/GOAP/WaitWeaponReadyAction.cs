using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "GOAP/Actions/WaitWeaponReady")]
public class WaitWeaponReadyAction : GoapActionSO
{
    [Tooltip("Safety upper bound for waiting (seconds). 0 = no limit.")]
    public float hardTimeout = 3f;

    public override bool Preconditions(GoapAgent a, in WorldState ws)
        => ws.WeaponReady == false;

    public override void ApplyEffects(ref WorldState ws)
    {
        ws.WeaponReady = true;
    }

    public override IEnumerator Execute(GoapAgent a)
    {
        float t = 0f;
        if (!a.CurrentTarget) yield break;

        while (!a.IsWeaponReady)
        {
            if (!a.CurrentTarget) yield break;
            if (hardTimeout > 0f && (t += Time.deltaTime) >= hardTimeout) break;
            yield return null;
        }
    }

    protected override void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(ActionName))
            ActionName = "WaitWeaponReady";
    }
}
