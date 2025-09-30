using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProcDungeonGenerator : MonoBehaviour
{
    [Header("Layout")]
    public int steps = 12;
    public Vector2Int start = Vector2Int.zero;

    [Header("Prefabs")]
    public RoomBuilder roomPrefab;
    public GameObject enemyPrefab;
    public GameObject playerPrefab; // NUEVO (así garantizamos player)

    [Header("Room Params")]
    public Vector2 roomWorldSpacing = new(20f, 14f);

    Dictionary<Vector2Int, RoomBuilder> rooms = new();

    void Start()
    {
        StartCoroutine(GenerateRoutine());
    }

    IEnumerator GenerateRoutine()
    {
        GenerateCore();

        // Snap al final de frame por si Tilemap/Colliders actualizan tarde
        yield return null;

        if (rooms.TryGetValue(start, out var startRoom))
        {
            CameraRoomLock.Instance?.SnapToRoom(startRoom.RoomBounds);
        }
    }

    void GenerateCore()
    {
        rooms.Clear();

        // 1) camino aleatorio conectado
        HashSet<Vector2Int> cells = new() { start };
        Vector2Int cur = start;
        for (int i = 0; i < steps; i++)
        {
            cur += RandomDir();
            cells.Add(cur);
        }

        // 2) crear salas
        foreach (var cell in cells)
        {
            var pos = new Vector3(cell.x * roomWorldSpacing.x, cell.y * roomWorldSpacing.y, 0);
            var room = Instantiate(roomPrefab, pos, Quaternion.identity, transform);
            rooms[cell] = room;
        }

        // 3) conexiones + build + runtime
        foreach (var kv in rooms)
        {
            var cell = kv.Key;
            var room = kv.Value;

            room.north = rooms.ContainsKey(cell + Vector2Int.up);
            room.south = rooms.ContainsKey(cell + Vector2Int.down);
            room.east = rooms.ContainsKey(cell + Vector2Int.right);
            room.west = rooms.ContainsKey(cell + Vector2Int.left);

            room.Build();

            var runtime = room.GetComponent<RoomRuntime>();
            if (!runtime) runtime = room.gameObject.AddComponent<RoomRuntime>();
            runtime.enemyPrefab = enemyPrefab;
        }

        // 4) sala inicial segura, Player y cámara
        if (rooms.TryGetValue(start, out var startRoom))
        {
            var runtime = startRoom.GetComponent<RoomRuntime>();
            runtime.MarkAsStartRoom();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                if (playerPrefab == null)
                {
                    Debug.LogError("[ProcDungeonGenerator] No hay Player en escena ni playerPrefab asignado.");
                    return;
                }
                player = Instantiate(playerPrefab);
                player.tag = "Player";
            }

            player.transform.position = startRoom.RoomBounds.center;

            // Snap inmediato
            CameraRoomLock.Instance?.SnapToRoom(startRoom.RoomBounds);
        }
        else
        {
            Debug.LogError("[ProcDungeonGenerator] No se encontró la start room en 'rooms'.");
        }
    }

    Vector2Int RandomDir()
    {
        int r = Random.Range(0, 4);
        return r switch
        {
            0 => Vector2Int.up,
            1 => Vector2Int.down,
            2 => Vector2Int.right,
            _ => Vector2Int.left,
        };
    }
}
