using UnityEngine;

public class BattleManager : MonoBehaviour
{
    void Start()
    {
        string enemyToSpawn = GameData.selectedEnemy;
        if (!string.IsNullOrEmpty(enemyToSpawn))
        {
            GameObject enemyPrefab = Resources.Load<GameObject>("Enemies/" + enemyToSpawn);
            if (enemyPrefab != null)
            {
                Instantiate(enemyPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            }
            else
            {
                Debug.LogError("Enemy prefab not found: " + enemyToSpawn);
            }
        }
    }
}

