// Assets/Scripts/Map/DungeonMapRegistry.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public class DungeonMapRegistry : MonoBehaviour
{
    public static DungeonMapRegistry Instance { get; private set; }

    public struct RoomInfo
    {
        public Vector2Int cell;
        public bool north, south, east, west;
        public bool isStart, isBoss, isArmory;
        public bool visited;     // ya entraste
        public bool discovered;  // aparece en minimapa (vecina de una visitada)
    }

    public event Action<RoomInfo> OnRoomRegistered;
    public event Action<RoomInfo> OnRoomUpdated;
    public event Action<Vector2Int> OnPlayerEnteredRoom;

    readonly Dictionary<Vector2Int, RoomInfo> map = new();

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
            RevealNeighbors(cell);
            OnPlayerEnteredRoom?.Invoke(cell); // invocado DENTRO de la clase -> válido
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
            RevealNeighbors(cell);
        }
    }

    public void NotifyPlayerEnteredRoom(Vector2Int cell)
    {
        OnPlayerEnteredRoom?.Invoke(cell); // método público para notificar desde afuera
    }

    void RevealNeighbors(Vector2Int cell)
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
}
