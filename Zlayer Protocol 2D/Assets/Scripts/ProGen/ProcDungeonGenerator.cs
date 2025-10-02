// Assets/Scripts/ProcGen/ProcDungeonGenerator.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProcDungeonGenerator : MonoBehaviour
{
    [Header("Room Count")]
    public int minRooms = 8;    // incluye start (sin contar extras)
    public int maxRooms = 12;   // incluye start (sin contar extras)

    [Header("Extras")]
    [Tooltip("La sala del Jefe se agrega como hoja adicional con 1 entrada.")]
    public bool bossIsExtra = true;

    [Tooltip("La Armory se agrega como hoja adicional con 1 entrada.")]
    public bool armoryIsExtra = true;

    [Header("Layout")]
    public Vector2Int start = Vector2Int.zero;

    [Range(0f, 0.8f)]
    [Tooltip("Probabilidad de ramificar desde una sala ya creada.")]
    public float branchChance = 0.25f;

    [Header("Prefabs")]
    public RoomBuilder roomPrefab;
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public GameObject playerPrefab;
    public GameObject doorBarrierPrefab;

    [Header("Pickups / Mejoras")]
    [Tooltip("Prefab del objeto de mejora (pickup) que aparecerá en Armory y tras el jefe.")]
    public GameObject upgradePickupPrefab;

    [Header("Room Params")]
    public Vector2 roomWorldSpacing = new(20f, 14f);

    // Runtime
    Dictionary<Vector2Int, RoomBuilder> rooms = new();

    void OnValidate()
    {
        if (minRooms < 1) minRooms = 1;
        if (maxRooms < minRooms) maxRooms = minRooms;
    }

    void Start()
    {
        StartCoroutine(GenerateRoutine());
    }

    IEnumerator GenerateRoutine()
    {
        GenerateCore();
        yield return null;

        if (rooms.TryGetValue(start, out var startRoom))
            CameraRoomLock.Instance?.SnapToRoom(startRoom.RoomBounds);
    }

    void GenerateCore()
    {
        rooms.Clear();

        // 1) Construir set base [min,max] (sin contar extras)
        int targetRooms = Random.Range(minRooms, maxRooms + 1);
        HashSet<Vector2Int> cells = BuildRoomsSet(targetRooms);

        // 2) Elegir hojas estrictas (1 sola entrada) para Boss y Armory (diferentes)
        var bossLeaf = FindStrictLeaf(cells, start, forbid: null);                 // la más lejana posible
        var armoryLeaf = FindStrictLeaf(cells, start, forbid: new() { bossLeaf.bossCell }); // otra hoja distinta

        if (bossIsExtra && bossLeaf.bossCell != bossLeaf.attachFrom) cells.Add(bossLeaf.bossCell);
        if (armoryIsExtra && armoryLeaf.bossCell != armoryLeaf.attachFrom) cells.Add(armoryLeaf.bossCell);

        // 3) Instanciar salas
        foreach (var cell in cells)
        {
            var pos = new Vector3(cell.x * roomWorldSpacing.x, cell.y * roomWorldSpacing.y, 0);
            var room = Instantiate(roomPrefab, pos, Quaternion.identity, transform);
            rooms[cell] = room;
        }

        // 4) Conexiones + Build + runtime base
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
            runtime.doorBarrierPrefab = doorBarrierPrefab;
            runtime.upgradePickupPrefab = upgradePickupPrefab; // para Armory y drop del Boss
        }

        // 5) Start + Player
        if (rooms.TryGetValue(start, out var startRoom))
        {
            var runtime = startRoom.GetComponent<RoomRuntime>();
            runtime.MarkAsStartRoom();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null && playerPrefab != null)
            {
                player = Instantiate(playerPrefab);
                player.tag = "Player";
            }
            if (player != null)
            {
                player.transform.position = startRoom.RoomBounds.center;
                CameraRoomLock.Instance?.SnapToRoom(startRoom.RoomBounds);
            }
        }

        // 6) Configurar Boss y Armory
        Vector2Int fallbackFarthest = GetFarthestCell(new HashSet<Vector2Int>(rooms.Keys), start);

        // Boss
        Vector2Int chosenBossCell =
            (bossIsExtra && rooms.ContainsKey(bossLeaf.bossCell) && bossLeaf.bossCell != start)
            ? bossLeaf.bossCell : fallbackFarthest;

        if (chosenBossCell != start && rooms.TryGetValue(chosenBossCell, out var bossRoom))
        {
            var bossRt = bossRoom.GetComponent<RoomRuntime>();
            bossRt.ConfigureAsBossRoom(bossPrefab);
        }

        // Armory (evitar solapar con Boss/Start)
        Vector2Int chosenArmoryCell =
            (armoryIsExtra && rooms.ContainsKey(armoryLeaf.bossCell) &&
             armoryLeaf.bossCell != start && armoryLeaf.bossCell != chosenBossCell)
            ? armoryLeaf.bossCell : FindAnotherLeafOrFarthest(rooms, start, chosenBossCell);

        if (chosenArmoryCell != start && chosenArmoryCell != chosenBossCell &&
            rooms.TryGetValue(chosenArmoryCell, out var armRoom))
        {
            var armRt = armRoom.GetComponent<RoomRuntime>();
            armRt.ConfigureAsArmoryRoom(upgradePickupPrefab);
        }
    }

    // ---------- helpers de layout ----------
    HashSet<Vector2Int> BuildRoomsSet(int targetRooms)
    {
        HashSet<Vector2Int> cells = new() { start };
        Vector2Int cur = start;

        while (cells.Count < targetRooms)
        {
            if (Random.value < branchChance)
                cur = GetRandomCell(cells);

            Vector2Int next = cur + RandomDir();
            cells.Add(next);
            cur = next;
        }
        return cells;
    }

    Vector2Int GetRandomCell(HashSet<Vector2Int> cells)
    {
        int idx = Random.Range(0, cells.Count);
        foreach (var c in cells) { if (idx-- == 0) return c; }
        return start;
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

    (Vector2Int attachFrom, Vector2Int bossCell) FindStrictLeaf(HashSet<Vector2Int> cells, Vector2Int source, HashSet<Vector2Int> forbid)
    {
        var dist = BFS(cells, source);

        Vector2Int bestAttach = source;
        Vector2Int bestLeaf = source;
        int bestDist = -1;

        foreach (var c in cells)
        {
            int dc = dist.GetValueOrDefault(c, -1);
            if (dc < 0) continue;

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int n = c + dir;
                if (forbid != null && forbid.Contains(n)) continue;
                if (cells.Contains(n)) continue;

                int neighbors = CountNeighborsInSet(n, cells);
                if (neighbors == 1) // estrictamente 1 entrada
                {
                    if (dc > bestDist)
                    {
                        bestDist = dc;
                        bestAttach = c;
                        bestLeaf = n;
                    }
                }
            }
        }

        if (bestDist >= 0)
            return (bestAttach, bestLeaf);

        // fallback
        var far = GetFarthestCell(cells, source);
        return (far, far);
    }

    Vector2Int FindAnotherLeafOrFarthest(Dictionary<Vector2Int, RoomBuilder> map, Vector2Int source, Vector2Int avoid)
    {
        var cells = new HashSet<Vector2Int>(map.Keys);
        var leaf = FindStrictLeaf(cells, source, new() { avoid });
        if (leaf.bossCell != leaf.attachFrom && map.ContainsKey(leaf.bossCell))
            return leaf.bossCell;

        // fallback: otra celda lejana que no sea avoid
        Vector2Int far = GetFarthestCell(cells, source);
        if (far == avoid) // busca la segunda más lejana simple
        {
            var dist = BFS(cells, source);
            int best = -1; Vector2Int pick = source;
            foreach (var kv in dist)
            {
                if (kv.Key == avoid) continue;
                if (kv.Value != int.MaxValue && kv.Value > best)
                {
                    best = kv.Value; pick = kv.Key;
                }
            }
            return pick;
        }
        return far;
    }

    int CountNeighborsInSet(Vector2Int cell, HashSet<Vector2Int> set)
    {
        int cnt = 0;
        if (set.Contains(cell + Vector2Int.up)) cnt++;
        if (set.Contains(cell + Vector2Int.down)) cnt++;
        if (set.Contains(cell + Vector2Int.left)) cnt++;
        if (set.Contains(cell + Vector2Int.right)) cnt++;
        return cnt;
    }

    Dictionary<Vector2Int, int> BFS(HashSet<Vector2Int> cells, Vector2Int source)
    {
        var q = new Queue<Vector2Int>();
        var dist = new Dictionary<Vector2Int, int>();
        foreach (var c in cells) dist[c] = int.MaxValue;
        dist[source] = 0;
        q.Enqueue(source);

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            foreach (var v in Neigh(u))
            {
                if (!cells.Contains(v)) continue;
                if (dist[v] != int.MaxValue) continue;
                dist[v] = dist[u] + 1;
                q.Enqueue(v);
            }
        }
        return dist;
    }

    Vector2Int GetFarthestCell(HashSet<Vector2Int> cells, Vector2Int source)
    {
        var dist = BFS(cells, source);
        Vector2Int far = source; int best = -1;
        foreach (var kv in dist)
        {
            if (kv.Value != int.MaxValue && kv.Value > best)
            {
                best = kv.Value; far = kv.Key;
            }
        }
        return far;
    }

    IEnumerable<Vector2Int> Neigh(Vector2Int c)
    {
        yield return c + Vector2Int.up;
        yield return c + Vector2Int.down;
        yield return c + Vector2Int.left;
        yield return c + Vector2Int.right;
    }
}
