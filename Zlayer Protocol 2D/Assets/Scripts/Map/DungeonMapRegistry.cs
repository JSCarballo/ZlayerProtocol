// Assets/Scripts/Map/DungeonMapRegistry.cs
using UnityEngine;
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(-500)]
public class DungeonMapRegistry : MonoBehaviour
{
    public static DungeonMapRegistry Instance { get; private set; }

    public struct RoomInfo
    {
        public Vector2Int cell;
        public bool north, south, east, west;
        public bool isStart, isBoss, isArmory;
        public bool visited;     // ya entraste
        public bool discovered;  // visible en minimapa
    }

    public event Action<RoomInfo> OnRoomRegistered;
    public event Action<RoomInfo> OnRoomUpdated;
    public event Action<Vector2Int> OnPlayerEnteredRoom;

    readonly Dictionary<Vector2Int, RoomInfo> map = new();
    Vector2Int? startCell = null;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterRoom(Vector2Int cell, bool n, bool s, bool e, bool w, bool isStart, bool isBoss, bool isArmory)
    {
        RoomInfo info = new RoomInfo
        {
            cell = cell,
            north = n,
            south = s,
            east = e,
            west = w,
            isStart = isStart,
            isBoss = isBoss,
            isArmory = isArmory,
            visited = isStart,
            discovered = isStart
        };
        map[cell] = info;
        OnRoomRegistered?.Invoke(info);

        if (isStart)
        {
            startCell = cell;
            // No dependas solo de esta llamada temprana; haremos un “repaso” final después de registrar todo.
            RevealNeighbors(cell);
            OnPlayerEnteredRoom?.Invoke(cell);
        }
    }

    public void SetVisited(Vector2Int cell)
    {
        if (!map.TryGetValue(cell, out var info)) return;
        if (!info.visited)
        {
            info.visited = true;
            info.discovered = true;
            map[cell] = info;
            OnRoomUpdated?.Invoke(info);
        }
        // Al visitar, revelar vecinas (como TBoi)
        RevealNeighbors(cell);
    }

    public void NotifyPlayerEnteredRoom(Vector2Int cell)
    {
        OnPlayerEnteredRoom?.Invoke(cell);
    }

    /// <summary>
    /// Llamar tras haber registrado TODAS las salas del piso. Re-revela vecinas de
    /// la sala inicial y de cualquier sala ya marcada como visitada.
    /// </summary>
    public void FinalizeAfterAllRoomsRegistered()
    {
        if (startCell.HasValue) RevealNeighbors(startCell.Value);
        foreach (var kv in map)
            if (kv.Value.visited) RevealNeighbors(kv.Key);
    }

    /// <summary>
    /// Marca como discovered las salas vecinas ya registradas de 'cell' y emite OnRoomUpdated.
    /// </summary>
    public void RevealNeighbors(Vector2Int cell)
    {
        if (!map.TryGetValue(cell, out var c)) return;

        void Mark(Vector2Int ncell)
        {
            if (!map.TryGetValue(ncell, out var nfo)) return;
            if (!nfo.discovered)
            {
                nfo.discovered = true;
                map[ncell] = nfo;
                OnRoomUpdated?.Invoke(nfo);
            }
        }

        if (c.north) Mark(cell + Vector2Int.up);
        if (c.south) Mark(cell + Vector2Int.down);
        if (c.east) Mark(cell + Vector2Int.right);
        if (c.west) Mark(cell + Vector2Int.left);
    }

    public bool TryGet(Vector2Int cell, out RoomInfo info) => map.TryGetValue(cell, out info);
    public IEnumerable<RoomInfo> AllRooms() => map.Values;

    // No borra suscriptores por defecto para no romper el HUD/minimapa
    public void ClearAll(bool keepSubscribers = true)
    {
        map.Clear();
        startCell = null;
        if (!keepSubscribers)
        {
            OnRoomRegistered = null;
            OnRoomUpdated = null;
            OnPlayerEnteredRoom = null;
        }
    }
}
