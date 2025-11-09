using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Debug/Metrics Spawner")]
public class MetricsSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject obstaclePrefab;

    [Header("Hierarchy (optional)")]
    public Transform enemiesParent;
    public Transform obstaclesParent;

    [Header("Enemy Defaults")]
    public int enemyDefaultCount = 3;
    public float enemyDefaultScatterRadius = 1.5f;

    [Header("Obstacle Defaults")]
    public int obstacleDefaultCount = 10;
    public float obstacleDefaultScatterRadius = 3f;
    public Vector2 obstacleScaleRange = new Vector2(0.8f, 1.4f);
    public int randomSeed = 1234;

    readonly List<GameObject> _enemies = new();
    readonly List<GameObject> _obstacles = new();

    public System.Action<Enemy> OnEnemySpawned;

    public void SpawnEnemiesAtCursor() =>
        SpawnEnemies(enemyDefaultCount, GetMouseWorldPosition(), enemyDefaultScatterRadius);

    public void SpawnEnemiesAtCursor(int count, float scatterRadius) =>
        SpawnEnemies(count, GetMouseWorldPosition(), scatterRadius);

    public void SpawnEnemiesAroundPlayer()
    {
        var p = PlayerRegistry.GetClosestPlayer(Vector2.zero);
        var center = p ? p.position : Vector3.zero;
        SpawnEnemies(enemyDefaultCount, center, enemyDefaultScatterRadius);
    }
    public void SpawnEnemiesAroundPlayer(int count, float scatterRadius)
    {
        var p = PlayerRegistry.GetClosestPlayer(Vector2.zero);
        var center = p ? p.position : Vector3.zero;
        SpawnEnemies(count, center, scatterRadius);
    }

    public void SpawnEnemies(int count, Vector3 center, float scatterRadius)
    {
        if (!enemyPrefab) return;
        for (int i = 0; i < count; i++)
        {
            var pos = center + (Vector3)(Random.insideUnitCircle * scatterRadius);
            pos.z = 0f;
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity, enemiesParent);
            go.name = $"Enemy_{System.DateTime.Now:HHmmss}_{i}";
            _enemies.Add(go);

            var enemy = go.GetComponent<Enemy>();
            if (enemy) OnEnemySpawned?.Invoke(enemy);
        }
    }

    public void DespawnAllEnemies()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
            if (_enemies[i]) Destroy(_enemies[i]);
        _enemies.Clear();
    }

    public void SpawnObstaclesAtCursor() =>
        SpawnObstacles(obstacleDefaultCount, GetMouseWorldPosition(), obstacleDefaultScatterRadius);

    public void SpawnObstaclesAtCursor(int count, float scatterRadius) =>
        SpawnObstacles(count, GetMouseWorldPosition(), scatterRadius);

    public void SpawnObstaclesAroundPlayer()
    {
        var p = PlayerRegistry.GetClosestPlayer(Vector2.zero);
        var center = p ? p.position : Vector3.zero;
        SpawnObstacles(obstacleDefaultCount, center, obstacleDefaultScatterRadius);
    }
    public void SpawnObstaclesAroundPlayer(int count, float scatterRadius)
    {
        var p = PlayerRegistry.GetClosestPlayer(Vector2.zero);
        var center = p ? p.position : Vector3.zero;
        SpawnObstacles(count, center, scatterRadius);
    }

    public void SpawnObstacles(int count, Vector3 center, float scatterRadius)
    {
        if (!obstaclePrefab) return;

        var rng = new System.Random(randomSeed);
        for (int i = 0; i < count; i++)
        {
            var r = (float)rng.NextDouble();
            var pos = center + (Vector3)(Random.insideUnitCircle * scatterRadius);
            pos.z = 0f;

            var go = Instantiate(obstaclePrefab, pos, Quaternion.identity, obstaclesParent);
            go.name = $"Obstacle_{System.DateTime.Now:HHmmss}_{i}";

            float s = Mathf.Lerp(obstacleScaleRange.x, obstacleScaleRange.y, r);
            go.transform.localScale = new Vector3(s, s, 1f);

            _obstacles.Add(go);
        }
    }

    public void DespawnAllObstacles()
    {
        for (int i = _obstacles.Count - 1; i >= 0; i--)
            if (_obstacles[i]) Destroy(_obstacles[i]);
        _obstacles.Clear();
    }

    public void DespawnAll()
    {
        DespawnAllEnemies();
        DespawnAllObstacles();
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
