// Assets/Scripts/ProcGen/ProcDungeonGenerator.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProcDungeonGenerator : MonoBehaviour
{
    [Header("Room Count (sin extras)")]
    [Tooltip("Mínimo de habitaciones base (incluye Start). Extras (Boss/Armory) no cuentan aquí.")]
    public int minRooms = 8;
    public int maxRooms = 12;

    [Header("Extras (hojas de 1 acceso)")]
    public bool bossIsExtra = true;     // Boss como hoja adicional
    public bool armoryIsExtra = true;   // Armory como hoja adicional

    [Header("Layout")]
    public Vector2Int start = Vector2Int.zero;

    [Range(0f, 0.8f)]
    [Tooltip("Probabilidad de ramificar desde una celda existente al generar el layout.")]
    public float branchChance = 0.25f;

    [Header("Prefabs")]
    public RoomBuilder roomPrefab;
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public GameObject playerPrefab;
    public GameObject doorBarrierPrefab;
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
        yield return null; // espera 1 frame por tilemap/colliders

        if (rooms.TryGetValue(start, out var startRoom))
        {
            CameraRoomLock.Instance?.SnapToRoom(startRoom.RoomBounds);
        }
    }

    void GenerateCore()
    {
        rooms.Clear();

        // ===== 1) Mapa base =====
        int targetRooms = Random.Range(minRooms, maxRooms + 1);
        HashSet<Vector2Int> cells = BuildRoomsSet(targetRooms);

        // ===== 2) Elegir hojas EXTRA (siempre intentamos agregarlas físicamente) =====
        //     - Boss: celda nueva con 1 vecino (si es posible)
        //     - Armory: otra celda nueva distinta de Boss
        Vector2Int bossAttach, bossCell;
        if (!TryFindStrictLeaf(cells, start, null, out bossAttach, out bossCell))
        {
            // fuerza hoja extra desde la celda más lejana con algún vecino libre
            TryForceExtraLeaf(cells, start, null, out bossAttach, out bossCell);
        }

        Vector2Int armAttach, armCell;
        var forbidArm = new HashSet<Vector2Int> { bossCell };
        if (!TryFindStrictLeaf(cells, start, forbidArm, out armAttach, out armCell))
        {
            TryForceExtraLeaf(cells, start, forbidArm, out armAttach, out armCell);
        }

        // Añadir físicamente las hojas extra si se piden
        if (bossIsExtra && bossCell != bossAttach) cells.Add(bossCell);
        if (armoryIsExtra && armCell != armAttach && armCell != bossCell) cells.Add(armCell);

        // Determinar definitivos (si extras están apagados, podemos usar farthest como fallback visual)
        Vector2Int chosenBossCell = bossIsExtra && cells.Contains(bossCell) ? bossCell : GetFarthestCell(cells, start);
        Vector2Int chosenArmCell = armoryIsExtra && cells.Contains(armCell) ? armCell : FindAnotherFarthest(cells, start, chosenBossCell);

        // ===== 3) Instanciar salas =====
        foreach (var cell in cells)
        {
            var pos = new Vector3(cell.x * roomWorldSpacing.x, cell.y * roomWorldSpacing.y, 0);
            var room = Instantiate(roomPrefab, pos, Quaternion.identity, transform);
            rooms[cell] = room;
        }

        // ===== 4) Conexiones + Build + runtime base + registro mapa =====
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
            runtime.upgradePickupPrefab = upgradePickupPrefab;
            runtime.gridCell = cell;

            // Registrar sala en el minimapa con flags correctos ya decididos
            bool isStart = (cell == start);
            bool isBoss = (cell == chosenBossCell);
            bool isArm = (cell == chosenArmCell);

            DungeonMapRegistry.Instance?.RegisterRoom(
                cell,
                room.north, room.south, room.east, room.west,
                isStart, isBoss, isArm
            );
        }

        // ===== 5) Start + Player =====
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

        // ===== 6) Configurar Boss y Armory =====
        if (rooms.TryGetValue(chosenBossCell, out var bossRoom))
        {
            var bossRt = bossRoom.GetComponent<RoomRuntime>();
            bossRt.ConfigureAsBossRoom(bossPrefab);
        }

        if (rooms.TryGetValue(chosenArmCell, out var armRoom))
        {
            var armRt = armRoom.GetComponent<RoomRuntime>();
            armRt.ConfigureAsArmoryRoom(upgradePickupPrefab);
        }
    }

    // =======================
    //   Construcción del set
    // =======================
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

    // =======================
    //     Hojas estrictas
    // =======================
    bool TryFindStrictLeaf(HashSet<Vector2Int> cells, Vector2Int source, HashSet<Vector2Int> forbid,
                           out Vector2Int attachFrom, out Vector2Int extraCell)
    {
        attachFrom = source; extraCell = source;

        var dist = BFS(cells, source);
        var ordered = new List<Vector2Int>(cells);
        ordered.Sort((a, b) => dist.GetValueOrDefault(b, -1).CompareTo(dist.GetValueOrDefault(a, -1)));

        foreach (var c in ordered)
        {
            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var n = c + dir;
                if (cells.Contains(n)) continue;
                if (forbid != null && forbid.Contains(n)) continue;

                if (CountNeighborsInSet(n, cells) == 1)
                {
                    attachFrom = c;
                    extraCell = n;
                    return true;
                }
            }
        }
        return false;
    }

    bool TryForceExtraLeaf(HashSet<Vector2Int> cells, Vector2Int source, HashSet<Vector2Int> forbid,
                           out Vector2Int attachFrom, out Vector2Int extraCell)
    {
        attachFrom = source; extraCell = source;

        var dist = BFS(cells, source);
        var ordered = new List<Vector2Int>(cells);
        ordered.Sort((a, b) => dist.GetValueOrDefault(b, -1).CompareTo(dist.GetValueOrDefault(a, -1)));

        foreach (var c in ordered)
        {
            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var n = c + dir;
                if (cells.Contains(n)) continue;
                if (forbid != null && forbid.Contains(n)) continue;

                attachFrom = c;
                extraCell = n;
                return true;
            }
        }
        // no hay huecos libres (muy raro) → caída
        attachFrom = GetFarthestCell(cells, source);
        extraCell = attachFrom; // no-extra posible
        return false;
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

    Vector2Int FindAnotherFarthest(HashSet<Vector2Int> cells, Vector2Int source, Vector2Int avoid)
    {
        var dist = BFS(cells, source);
        Vector2Int pick = source; int best = -1;
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

    IEnumerable<Vector2Int> Neigh(Vector2Int c)
    {
        yield return c + Vector2Int.up;
        yield return c + Vector2Int.down;
        yield return c + Vector2Int.left;
        yield return c + Vector2Int.right;
    }
}
