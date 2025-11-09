using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Debug/Rino Spawner")]
public class RinoSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject rinoPrefab;

    [Header("Hierarchy")]
    public Transform defaultParent;

    [Header("Defaults")]
    public int defaultCount = 1;
    public float defaultScatterRadius = 1.5f;

    private readonly List<GameObject> _spawned = new();

    public event System.Action<Enemy> OnSpawned;

    public void SpawnAtCursor() => Spawn(defaultCount, GetMouseWorldPosition(), defaultScatterRadius);
    public void SpawnAtCursor(int count, float scatterRadius) => Spawn(count, GetMouseWorldPosition(), scatterRadius);

    public void SpawnAroundPlayer()
    {
        var p = PlayerRegistry.GetClosestPlayer(Vector2.zero);
        var center = p ? p.position : Vector3.zero;
        Spawn(defaultCount, center, defaultScatterRadius);
    }

    public void SpawnAroundPlayer(int count, float scatterRadius)
    {
        var p = PlayerRegistry.GetClosestPlayer(Vector2.zero);
        var center = p ? p.position : Vector3.zero;
        Spawn(count, center, scatterRadius);
    }

    public void Spawn(int count, Vector3 center, float scatterRadius)
    {
        if (!rinoPrefab) return;

        for (int i = 0; i < count; i++)
        {
            var pos = center + (Vector3)(Random.insideUnitCircle * scatterRadius);
            pos.z = 0f;
            var go = Instantiate(rinoPrefab, pos, Quaternion.identity, defaultParent);
            go.name = $"Rino_{System.DateTime.Now:HHmmss}_{i}";
            _spawned.Add(go);

            var enemy = go.GetComponent<Enemy>();
            if (enemy != null) OnSpawned?.Invoke(enemy);
        }
    }
    public void DespawnAllSpawned()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
            if (_spawned[i] != null) Destroy(_spawned[i]);
        _spawned.Clear();
    }

    Vector3 GetMouseWorldPosition()
    {
        var cam = Camera.main;
        if (!cam) return Vector3.zero;
        var mp = Input.mousePosition;
        mp.z = Mathf.Abs(cam.transform.position.z);
        var w = cam.ScreenToWorldPoint(mp);
        w.z = 0f;
        return w;
    }
}
