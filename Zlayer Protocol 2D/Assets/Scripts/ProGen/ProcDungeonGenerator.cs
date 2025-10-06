using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using URandom = UnityEngine.Random;

public class ProcDungeonGenerator : MonoBehaviour
{
    public static Action OnGenerated; // se lanza al finalizar la generación

    [Header("AutoGenerate (debug)")]
    public bool autoGenerateAtStart = false; // lo maneja FloorFlowController

    [Header("Player")]
    public GameObject playerPrefab;
    public bool setPlayerTagOnSpawn = true;

    [Header("Fallback global (si el piso no define prefab de sala)")]
    public RoomBuilder roomPrefab;

    FloorDefinitionSO active;
    Dictionary<Vector2Int, RoomBuilder> rooms = new();

    void Start()
    {
        if (autoGenerateAtStart && FloorFlowController.Instance == null)
        {
            Debug.LogWarning("[ProcGen] Autogenerando sin FloorFlow (solo debug).");
            var dummy = ScriptableObject.CreateInstance<FloorDefinitionSO>();
            dummy.floorId = "Debug"; dummy.displayName = "Debug Floor"; dummy.hasBoss = false;
            GenerateForFloor(dummy);
        }
    }

    public void GenerateForFloor(FloorDefinitionSO floor)
    {
        if (!roomPrefab && !floor.defaultRoomPrefab)
        {
            Debug.LogError("[ProcGen] Falta RoomBuilder (ni en generator ni en FloorDefinition).");
            return;
        }
        active = floor;

        foreach (var kv in rooms) if (kv.Value) Destroy(kv.Value.gameObject);
        rooms.Clear();

        StopAllCoroutines();
        StartCoroutine(GenerateRoutine());
    }

    IEnumerator GenerateRoutine()
    {
        var start = Vector2Int.zero;

        // 1) Layout base
        HashSet<Vector2Int> cells = BuildRoomsSet(start, active.minRooms, active.maxRooms, active.branchChance);

        // 2) Boss leaf: ancla = celda más lejana al start (leaf estricta -> 1 conexión)
        Vector2Int bossCell = start;
        if (active.hasBoss && active.bossIsExtra)
        {
            var far = GetFarthestCell(cells, start);
            TryAddStrictLeafFromAnchor(cells, far, exclude: null, out bossCell);
        }

        // 3) Armory leaf: lejos y NO adyacente al boss
        Vector2Int armCell = start;
        if (active.armoryIsExtra)
        {
            HashSet<Vector2Int> exclude = new() { bossCell };
            foreach (var d in dirs) exclude.Add(bossCell + d);
            var far2 = GetFarthestValidAnchor(cells, start, exclude);
            TryAddStrictLeafFromAnchor(cells, far2, exclude: exclude, out armCell);
        }

        // 4) Instanciar salas (usa prefabs del piso si existen)
        foreach (var cell in cells)
        {
            RoomBuilder prefabToUse = active.defaultRoomPrefab ? active.defaultRoomPrefab : roomPrefab;
            if (cell == start && active.overrideStartRoomPrefab != null)
                prefabToUse = active.overrideStartRoomPrefab;

            var pos = new Vector3(cell.x * active.roomWorldSpacing.x, cell.y * active.roomWorldSpacing.y, 0);
            var room = Instantiate(prefabToUse, pos, Quaternion.identity, transform);
            rooms[cell] = room;
        }

        // 5) Conexiones + build + runtime + registro
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

            runtime.gridCell = cell;
            runtime.enemyPrefab = active.enemyPrefab;
            runtime.doorBarrierPrefab = active.doorBarrierPrefab;
            runtime.upgradePickupPrefab = active.upgradePickupPrefab;
            runtime.elevatorExitPrefab = active.elevatorExitPrefab;
            runtime.minEnemies = active.enemyMin;
            runtime.maxEnemies = active.enemyMax;

            bool isStart = (cell == start);
            bool isBoss = active.hasBoss && (cell == bossCell);
            bool isArm = active.armoryIsExtra && (cell == armCell);

            if (isBoss) runtime.ConfigureAsBossRoom(active.bossPrefab);
            if (isArm) runtime.ConfigureAsArmoryRoom(active.upgradePickupPrefab);
            if (isStart) runtime.MarkAsStartRoom();

            runtime.overrideToArena = active.isArenaFloor && isStart;
            runtime.arenaWaves = active.arenaWaves;
            runtime.arenaWaveMin = active.arenaWaveMin;
            runtime.arenaWaveMax = active.arenaWaveMax;
            runtime.arenaInterval = active.arenaInterval;
            runtime.isFinalBossFloor = active.isFinalBoss;

            DungeonMapRegistry.Instance?.RegisterRoom(
                cell,
                room.north, room.south, room.east, room.west,
                isStart, isBoss, isArm
            );
        }

        // 6) Minimapa: revela vecinas de visitadas (incluye Start)
        DungeonMapRegistry.Instance?.FinalizeAfterAllRoomsRegistered();

        yield return null; // deja inicializar tilemaps/colliders

        // 7) Player a Start + cámara + follow solo en 1B + desbloqueo seguro
        if (rooms.TryGetValue(start, out var startRoom))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (!player) { var byName = GameObject.Find("Player"); if (byName) player = byName; }

            if (!player && playerPrefab)
            {
                player = Instantiate(playerPrefab);
                if (setPlayerTagOnSpawn) player.tag = "Player";
            }

            if (!player)
            {
                Debug.LogError("[ProcGen] No hay Player en escena y no se pudo instanciar.");
            }
            else
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb) rb.linearVelocity = Vector2.zero;

                player.transform.position = startRoom.RoomBounds.center;
                CameraRoomLock.Instance?.SnapToRoom(startRoom.RoomBounds);

                var locker = player.GetComponent<PlayerControlLocker>();
                if (locker) locker.HardUnlock(); // si venía lockeado por el ascensor

                // Follow SOLO si el piso es tipo arena (1B). En otros pisos, lock por salas.
                if (active.isArenaFloor)
                    CameraRoomLock.Instance?.EnterFollowMode(player.transform, startRoom.RoomBounds, 6f);
                else
                    CameraRoomLock.Instance?.ExitFollowMode();
            }
        }

        // 8) Señal para HUD/Minimapa
        OnGenerated?.Invoke();
    }

    // === Helpers ===
    HashSet<Vector2Int> BuildRoomsSet(Vector2Int start, int minRooms, int maxRooms, float branchChance)
    {
        int target = URandom.Range(minRooms, maxRooms + 1);
        HashSet<Vector2Int> cells = new() { start };
        Vector2Int cur = start;

        while (cells.Count < target)
        {
            if (URandom.value < branchChance)
            {
                int idx = URandom.Range(0, cells.Count);
                foreach (var c in cells) { if (idx-- == 0) { cur = c; break; } }
            }
            Vector2Int next = cur + RandomDir();
            cells.Add(next);
            cur = next;
        }
        return cells;
    }

    Vector2Int GetFarthestCell(HashSet<Vector2Int> cells, Vector2Int source)
    {
        var dist = BFS(cells, source);
        Vector2Int best = source; int maxd = -1;
        foreach (var kv in dist)
            if (kv.Value != int.MaxValue && kv.Value > maxd) { maxd = kv.Value; best = kv.Key; }
        return best;
    }

    Vector2Int GetFarthestValidAnchor(HashSet<Vector2Int> cells, Vector2Int source, HashSet<Vector2Int> exclude)
    {
        var dist = BFS(cells, source);
        Vector2Int best = source; int maxd = -1;
        foreach (var kv in dist)
        {
            if (kv.Value == int.MaxValue) continue;
            if (exclude != null && exclude.Contains(kv.Key)) continue;
            if (kv.Value > maxd) { maxd = kv.Value; best = kv.Key; }
        }
        return best;
    }

    // Añade hoja estricta desde un anchor (1 sola conexión). Evita celdas en 'exclude'.
    bool TryAddStrictLeafFromAnchor(HashSet<Vector2Int> cells, Vector2Int anchor, HashSet<Vector2Int> exclude, out Vector2Int newCell)
    {
        newCell = anchor;
        foreach (var dir in dirs)
        {
            var n = anchor + dir;
            if (cells.Contains(n)) continue;
            if (exclude != null && exclude.Contains(n)) continue;

            int neigh = 0; foreach (var d in dirs) if (cells.Contains(n + d)) neigh++;
            if (neigh == 1) { cells.Add(n); newCell = n; return true; }
        }
        // Fallback: probar otro anchor lejano
        var alt = GetFarthestValidAnchor(cells, Vector2Int.zero, exclude);
        if (alt != anchor) return TryAddStrictLeafFromAnchor(cells, alt, exclude, out newCell);
        return false;
    }

    Dictionary<Vector2Int, int> BFS(HashSet<Vector2Int> cells, Vector2Int source)
    {
        var q = new Queue<Vector2Int>();
        var dist = new Dictionary<Vector2Int, int>();
        foreach (var c in cells) dist[c] = int.MaxValue;
        dist[source] = 0; q.Enqueue(source);
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

    static readonly Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    IEnumerable<Vector2Int> Neigh(Vector2Int c) { foreach (var d in dirs) yield return c + d; }
    Vector2Int RandomDir() => dirs[URandom.Range(0, 4)];
}
