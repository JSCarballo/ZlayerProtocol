using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RoomBuilder))]
[RequireComponent(typeof(BoxCollider2D))]
public class RoomRuntime : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int minEnemies = 3, maxEnemies = 6;

    RoomBuilder builder;
    bool visited = false;
    bool isStartRoom = false;
    int aliveCount = 0;
    readonly List<Health> spawned = new();

    void Awake() => builder = GetComponent<RoomBuilder>();

    void Start()
    {
        builder.SetAllDoors(true); // por defecto abiertas hasta entrar
    }

    public void MarkAsStartRoom()
    {
        isStartRoom = true;
        visited = true;            // no spawnear aqu�
        builder.SetAllDoors(true); // siempre abierta

        // Limpieza por si acaso
        foreach (var e in FindObjectsOfType<EnemyChaseAI>())
        {
            if (builder.RoomBounds.Contains(e.transform.position))
            {
                var hp = e.GetComponent<Health>();
                if (hp) hp.Damage(9999);
                else Destroy(e.gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // SIEMPRE encuadrar c�mara a esta sala al entrar
        CameraRoomLock.Instance?.GoToRoom(builder.RoomBounds, 0.6f);

        if (isStartRoom || visited) return;

        visited = true;
        builder.SetAllDoors(false);
        SpawnWave();
    }

    void SpawnWave()
    {
        int count = Random.Range(minEnemies, maxEnemies + 1);
        aliveCount = count;
        spawned.Clear();

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = RandomPointInside(builder.RoomBounds, 1.0f);
            var e = Instantiate(enemyPrefab, pos, Quaternion.identity);
            var hp = e.GetComponent<Health>();
            if (hp != null)
            {
                spawned.Add(hp);
                hp.OnDeath += OnEnemyDeath;
            }
        }
    }

    void OnEnemyDeath()
    {
        aliveCount--;
        if (aliveCount <= 0)
        {
            builder.SetAllDoors(true);
            foreach (var hp in spawned) if (hp != null) hp.OnDeath -= OnEnemyDeath;
            spawned.Clear();
        }
    }

    Vector3 RandomPointInside(Bounds b, float margin)
    {
        float x = Random.Range(b.min.x + margin, b.max.x - margin);
        float y = Random.Range(b.min.y + margin, b.max.y - margin);
        return new Vector3(x, y, 0);
    }
}
