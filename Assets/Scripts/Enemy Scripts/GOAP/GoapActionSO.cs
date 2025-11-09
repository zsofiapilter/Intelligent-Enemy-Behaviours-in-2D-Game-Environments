using System.Collections;
using UnityEngine;

public abstract class GoapActionSO : ScriptableObject
{
    [Header("Meta")]
    public string ActionName;
    public float Cost = 1f;

    public abstract bool Preconditions(GoapAgent a, in WorldState ws);
    public abstract void ApplyEffects(ref WorldState ws);

    public abstract IEnumerator Execute(GoapAgent a);
    protected virtual void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(ActionName))
            ActionName = GetType().Name;
    }
}
