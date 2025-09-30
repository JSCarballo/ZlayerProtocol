using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomBuilder : MonoBehaviour
{
    [Header("Tilemap refs")]
    public Tilemap floorMap;
    public Tilemap wallsMap;

    [Header("Tiles")]
    public TileBase floorTile;
    public TileBase wallTile;

    [Header("Geometry")]
    public Vector2Int roomSizeTiles = new(16, 10); // ancho x alto

    [Header("Doors")]
    public Door doorPrefab;

    // conexiones (las marca el generador)
    [HideInInspector] public bool north, south, east, west;

    public Bounds RoomBounds { get; private set; }
    private readonly List<Door> doors = new();

    public void Build()
    {
        floorMap.ClearAllTiles();
        wallsMap.ClearAllTiles();
        doors.Clear();

        // Piso
        for (int y = 0; y < roomSizeTiles.y; y++)
            for (int x = 0; x < roomSizeTiles.x; x++)
                floorMap.SetTile(new Vector3Int(x, y, 0), floorTile);

        // Perímetro
        for (int x = 0; x < roomSizeTiles.x; x++)
        {
            wallsMap.SetTile(new Vector3Int(x, 0, 0), wallTile);
            wallsMap.SetTile(new Vector3Int(x, roomSizeTiles.y - 1, 0), wallTile);
        }
        for (int y = 0; y < roomSizeTiles.y; y++)
        {
            wallsMap.SetTile(new Vector3Int(0, y, 0), wallTile);
            wallsMap.SetTile(new Vector3Int(roomSizeTiles.x - 1, y, 0), wallTile);
        }

        // Huecos y puertas SOLO si hay vecino
        TryMakeDoorNorth();
        TryMakeDoorSouth();
        TryMakeDoorEast();
        TryMakeDoorWest();

        // Asegura bounds consistentes
        floorMap.CompressBounds();
        wallsMap.CompressBounds();

        var fr = floorMap.GetComponent<TilemapRenderer>();
        var wr = wallsMap.GetComponent<TilemapRenderer>();
        Bounds b = fr ? fr.bounds : new Bounds(transform.position, Vector3.one);
        if (wr) b.Encapsulate(wr.bounds);
        RoomBounds = b;

        // Ajusta trigger de la sala al rectángulo de la sala (mundo → local)
        var trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = transform.InverseTransformPoint(RoomBounds.center);
        trigger.size = RoomBounds.size;
    }

    void TryMakeDoorNorth()
    {
        if (!north) return;
        int mid = roomSizeTiles.x / 2;
        for (int dx = -1; dx <= 1; dx++)
            wallsMap.SetTile(new Vector3Int(mid + dx, roomSizeTiles.y - 1, 0), null);
        SpawnDoorAt(CellCenterWorld(mid, roomSizeTiles.y - 1));
    }
    void TryMakeDoorSouth()
    {
        if (!south) return;
        int mid = roomSizeTiles.x / 2;
        for (int dx = -1; dx <= 1; dx++)
            wallsMap.SetTile(new Vector3Int(mid + dx, 0, 0), null);
        SpawnDoorAt(CellCenterWorld(mid, 0));
    }
    void TryMakeDoorEast()
    {
        if (!east) return;
        int mid = roomSizeTiles.y / 2;
        for (int dy = -1; dy <= 1; dy++)
            wallsMap.SetTile(new Vector3Int(roomSizeTiles.x - 1, mid + dy, 0), null);
        SpawnDoorAt(CellCenterWorld(roomSizeTiles.x - 1, mid));
    }
    void TryMakeDoorWest()
    {
        if (!west) return;
        int mid = roomSizeTiles.y / 2;
        for (int dy = -1; dy <= 1; dy++)
            wallsMap.SetTile(new Vector3Int(0, mid + dy, 0), null);
        SpawnDoorAt(CellCenterWorld(0, mid));
    }

    Vector3 CellCenterWorld(int x, int y)
    {
        var wp = wallsMap.CellToWorld(new Vector3Int(x, y, 0));
        var cellSize = wallsMap.layoutGrid.cellSize;
        return wp + new Vector3(cellSize.x, cellSize.y, 0) * 0.5f; // centro de celda
    }

    void SpawnDoorAt(Vector3 worldPos)
    {
        if (!doorPrefab) return;
        var door = Instantiate(doorPrefab, worldPos, Quaternion.identity, transform);
        doors.Add(door);
    }

    public void SetAllDoors(bool open)
    {
        foreach (var d in doors) if (d) d.SetOpen(open);
    }
}
