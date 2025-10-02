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
    public Vector2Int roomSizeTiles = new(16, 10); // ancho x alto (tiles)

    // conexiones (las marca el generador)
    [HideInInspector] public bool north, south, east, west;

    public Bounds RoomBounds { get; private set; }

    // ==== NUEVO: info de anclas para puertas (centro + tamaño en unidades mundo)
    public struct DoorSpawn
    {
        public Vector3 center;
        public Vector2 size;
        public DoorSpawn(Vector3 c, Vector2 s) { center = c; size = s; }
    }

    public void Build()
    {
        floorMap.ClearAllTiles();
        wallsMap.ClearAllTiles();

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

        // Abrimos los huecos de puerta SOLO si hay vecino (3 tiles de ancho/alto)
        CarveDoorways();

        // Bounds fiables a partir de renderers
        floorMap.CompressBounds();
        wallsMap.CompressBounds();
        var fr = floorMap.GetComponent<TilemapRenderer>();
        var wr = wallsMap.GetComponent<TilemapRenderer>();
        Bounds b = fr ? fr.bounds : new Bounds(transform.position, Vector3.one);
        if (wr) b.Encapsulate(wr.bounds);
        RoomBounds = b;

        // Ajusta el trigger de la habitación al rectángulo de la sala
        var trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = transform.InverseTransformPoint(RoomBounds.center);
        trigger.size = RoomBounds.size;
    }

    void CarveDoorways()
    {
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

        if (north)
        {
            for (int dx = -1; dx <= 1; dx++)
                wallsMap.SetTile(new Vector3Int(midX + dx, roomSizeTiles.y - 1, 0), null);
        }
        if (south)
        {
            for (int dx = -1; dx <= 1; dx++)
                wallsMap.SetTile(new Vector3Int(midX + dx, 0, 0), null);
        }
        if (east)
        {
            for (int dy = -1; dy <= 1; dy++)
                wallsMap.SetTile(new Vector3Int(roomSizeTiles.x - 1, midY + dy, 0), null);
        }
        if (west)
        {
            for (int dy = -1; dy <= 1; dy++)
                wallsMap.SetTile(new Vector3Int(0, midY + dy, 0), null);
        }
    }

    Vector3 CellCenterWorld(int x, int y)
    {
        var wp = wallsMap.CellToWorld(new Vector3Int(x, y, 0));
        var cs = wallsMap.layoutGrid.cellSize;
        return wp + new Vector3(cs.x, cs.y, 0) * 0.5f;
    }

    public Vector2 GetCellSize() => wallsMap.layoutGrid.cellSize;

    // ==== NUEVO: devuelve anclas (centro y tamaño en mundo) para colocar barreras
    public IEnumerable<DoorSpawn> GetDoorSpawns()
    {
        var cs = GetCellSize();
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

        // Norte/SUR: hueco horizontal de 3 tiles x 1 tile
        if (north)
        {
            Vector3 c = CellCenterWorld(midX, roomSizeTiles.y - 1);
            yield return new DoorSpawn(c, new Vector2(cs.x * 3f, cs.y * 1f));
        }
        if (south)
        {
            Vector3 c = CellCenterWorld(midX, 0);
            yield return new DoorSpawn(c, new Vector2(cs.x * 3f, cs.y * 1f));
        }

        // Este/Oeste: hueco vertical de 1 tile x 3 tiles
        if (east)
        {
            Vector3 c = CellCenterWorld(roomSizeTiles.x - 1, midY);
            yield return new DoorSpawn(c, new Vector2(cs.x * 1f, cs.y * 3f));
        }
        if (west)
        {
            Vector3 c = CellCenterWorld(0, midY);
            yield return new DoorSpawn(c, new Vector2(cs.x * 1f, cs.y * 3f));
        }
    }
}
