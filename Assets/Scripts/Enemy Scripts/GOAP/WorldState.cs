using UnityEngine;

public enum DistanceBand { Near, Mid, Far }

public struct WorldState
{
    public bool HasLOS;
    public bool WeaponReady;
    public bool LowHP;
    public DistanceBand DistanceBand;
    public bool DidShoot;

    public int DistanceToGoal()
    {
        int miss = 0;
        if (!HasLOS) miss++;
        if (!WeaponReady) miss++;
        if (DistanceBand == DistanceBand.Far) miss++;
        if (!DidShoot) miss++;
        return miss;
    }
}
