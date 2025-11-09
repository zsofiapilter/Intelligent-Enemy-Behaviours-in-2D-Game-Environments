using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject enemyPrefab;

    public override void OnStartServer()
    {
        base.OnStartServer();

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");

        foreach (GameObject point in spawnPoints)
        {
            SpawnEnemy(point.transform.position);
        }
    }

    [Server]
    private void SpawnEnemy(Vector3 spawnPosition)
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkServer.Spawn(enemy);
    }
}
