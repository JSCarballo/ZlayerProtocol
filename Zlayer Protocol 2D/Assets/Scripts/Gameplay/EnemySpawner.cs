// Scripts/Gameplay/EnemySpawner.cs
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;
    public Transform[] points;
    public int minEnemies = 3, maxEnemies = 6;

    bool spawned = false;

    public void Begin()
    {
        if (spawned) return;
        spawned = true;

        int count = Random.Range(minEnemies, maxEnemies + 1);
        for (int i = 0; i < count; i++)
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var pos = points.Length > 0 ? points[Random.Range(0, points.Length)].position
                                        : transform.position + (Vector3)Random.insideUnitCircle * 2f;
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }
}
