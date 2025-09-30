using UnityEngine;

/// <summary>
/// Populates a room with a random set of enemies and decorations when the
/// player enters. Attach this to each Room prefab and assign enemy
/// prefabs and other objects via inspector.
/// </summary>
public class RoomPopulator : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] enemyPrefabs;
    public int minEnemies = 1;
    public int maxEnemies = 3;
    public Transform[] spawnPoints;

    private bool populated = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!populated && collision.CompareTag("Player"))
        {
            Populate();
        }
    }

    private void Populate()
    {
        populated = true;
        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);
        for (int i = 0; i < enemyCount; i++)
        {
            if (spawnPoints.Length == 0 || enemyPrefabs.Length == 0) return;
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Instantiate(prefab, point.position, Quaternion.identity, transform);
        }
    }
}