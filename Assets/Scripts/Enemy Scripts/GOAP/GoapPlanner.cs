using System.Collections.Generic;
using UnityEngine;

public static class GoapPlanner
{
    public static bool Plan(GoapAgent agent, WorldState start, List<GoapActionSO> actions, out List<GoapActionSO> plan)
    {
        plan = new List<GoapActionSO>();
        var ws = start;
        int guard = 16;

        while (!GoalOk(ws) && guard-- > 0)
        {
            GoapActionSO best = null;
            int bestScore = int.MaxValue;

            foreach (var act in actions)
            {
                if (!act.Preconditions(agent, ws)) continue;
                var sim = ws;
                act.ApplyEffects(ref sim);
                int score = sim.DistanceToGoal();
                if (score < bestScore) { bestScore = score; best = act; }
            }

            if (best == null)
            {
                Debug.LogWarning($"GOAP: no applicable action. Actions count={actions?.Count ?? 0}. ws: LOS={ws.HasLOS}, Dist={ws.DistanceBand}, WeaponReady={ws.WeaponReady}");
                return false;
            }
            best.ApplyEffects(ref ws);
            plan.Add(best);
        }

        for (int i = plan.Count - 2; i >= 0; --i)
            if (plan[i].ActionName == plan[i + 1].ActionName)
                plan.RemoveAt(i + 1);

        return GoalOk(ws);
    }

    static bool GoalOk(in WorldState ws)
        => ws.HasLOS && ws.DistanceBand != DistanceBand.Far && ws.WeaponReady && ws.DidShoot;
}
