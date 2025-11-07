using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomBuilder : MonoBehaviour
{
    [Header("Tilemap refs")]
    public Tilemap floorMap;
    public Tilemap wallsMap;

    [Header("Floor tiles")]
    public TileBase floorTile;                 // relleno del piso
    [Tooltip("Opcional: borde de piso cuando hay pared al Norte/Sur/Este/Oeste")]
    public TileBase floorEdgeN, floorEdgeS, floorEdgeE, floorEdgeW;

    [Header("Floor Edge Caps (terminaciones opcionales)")]
    public TileBase floorEdgeN_LeftCap;   // remate del tramo Norte, extremo izquierdo
    public TileBase floorEdgeN_RightCap;  // remate del tramo Norte, extremo derecho
    public TileBase floorEdgeS_LeftCap;   // remate del tramo Sur, extremo izquierdo
    public TileBase floorEdgeS_RightCap;  // remate del tramo Sur, extremo derecho
    public TileBase floorEdgeE_TopCap;    // remate del tramo Este, extremo superior
    public TileBase floorEdgeE_BottomCap; // remate del tramo Este, extremo inferior
    public TileBase floorEdgeW_TopCap;    // remate del tramo Oeste, extremo superior
    public TileBase floorEdgeW_BottomCap; // remate del tramo Oeste, extremo inferior

    [Header("Walls – Fallback genéricos")]
    [Tooltip("Se usan si en la secuencia no hay tile para esa posición.")]
    public TileBase wallH;   // tramo horizontal (arriba/abajo)
    public TileBase wallV;   // tramo vertical (izquierda/derecha)

    [Header("Walls – Esquinas de la SALA")]
    public TileBase wallCornerNW, wallCornerNE, wallCornerSE, wallCornerSW;

    [Header("Walls – Secuencias personalizables (excluyen esquinas de sala)")]
    [Tooltip("Fila superior, de izquierda a derecha, SIN incluir las esquinas.")]
    public List<TileBase> topEdge = new();
    [Tooltip("Fila inferior, de izquierda a derecha, SIN incluir las esquinas.")]
    public List<TileBase> bottomEdge = new();
    [Tooltip("Columna izquierda, de arriba a abajo, SIN incluir las esquinas.")]
    public List<TileBase> leftEdge = new();
    [Tooltip("Columna derecha, de arriba a abajo, SIN incluir las esquinas.")]
    public List<TileBase> rightEdge = new();

    public enum SeqFit { Repeat, Clamp, Stretch }
    [Header("Ajuste de secuencias")]
    public SeqFit sequenceFit = SeqFit.Repeat;

    [Header("Door threshold (suelo rojo)")]
    public TileBase thresholdH;                // umbral horizontal (N/S)
    public TileBase thresholdV;                // umbral vertical (E/W)

    [Header("Walls – Esquinas de PUERTA (JAMBAS)")]
    [Tooltip("Norte: esquina izquierda del hueco (x = midX-2, y = yMax)")]
    public TileBase doorNorthLeftCorner;   // N_L
    [Tooltip("Norte: esquina derecha del hueco (x = midX+2, y = yMax)")]
    public TileBase doorNorthRightCorner;  // N_R
    [Tooltip("Sur: esquina izquierda del hueco (x = midX-2, y = 0)")]
    public TileBase doorSouthLeftCorner;   // S_L
    [Tooltip("Sur: esquina derecha del hueco (x = midX+2, y = 0)")]
    public TileBase doorSouthRightCorner;  // S_R

    [Tooltip("Este: esquina superior del hueco (x = xMax, y = midY+2)")]
    public TileBase doorEastTopCorner;     // E_T
    [Tooltip("Este: esquina inferior del hueco (x = xMax, y = midY-2)")]
    public TileBase doorEastBottomCorner;  // E_B
    [Tooltip("Oeste: esquina superior del hueco (x = 0, y = midY+2)")]
    public TileBase doorWestTopCorner;     // W_T
    [Tooltip("Oeste: esquina inferior del hueco (x = 0, y = midY-2)")]
    public TileBase doorWestBottomCorner;  // W_B

    [Header("Geometry")]
    public Vector2Int roomSizeTiles = new(16, 10); // ancho x alto (tiles)

    // conexiones (marcadas por el generador)
    [HideInInspector] public bool north, south, east, west;

    public Bounds RoomBounds { get; private set; }

    // ==== Anclas para puertas (centro + tamaño en mundo)
    public struct DoorSpawn
    {
        public Vector3 center;
        public Vector2 size;
        public DoorSpawn(Vector3 c, Vector2 s) { center = c; size = s; }
    }

    // ---------------- Build ----------------
    public void Build()
    {
        floorMap.ClearAllTiles();
        wallsMap.ClearAllTiles();

        // 1) Piso base
        for (int y = 0; y < roomSizeTiles.y; y++)
            for (int x = 0; x < roomSizeTiles.x; x++)
                floorMap.SetTile(new Vector3Int(x, y, 0), floorTile);

        // 2) Pared perimetral (esquinas de sala + secuencias personalizadas)
        PlacePerimeterWallsCustom();

        // 3) Abrimos huecos de puerta (3 tiles de ancho/alto)
        CarveDoorways();

        // 4) Thresholds (suelo rojo en los huecos)
        PlaceThresholds();

        // 5) JAMBAS: esquinas de puerta en extremos del hueco
        PlaceDoorJambCorners();

        // 6) Borde de piso junto a paredes (con caps)
        DecorateFloorEdges();

        // 7) FIX específicos bajo jambas superiores West/East
        ApplyWestTopBelowSpecialCap(); // usa floorEdgeN_RightCap
        ApplyEastTopBelowSpecialCap(); // usa floorEdgeN_LeftCap

        // 8) Bounds fiables
        floorMap.CompressBounds();
        wallsMap.CompressBounds();
        var fr = floorMap.GetComponent<TilemapRenderer>();
        var wr = wallsMap.GetComponent<TilemapRenderer>();
        Bounds b = fr ? fr.bounds : new Bounds(transform.position, Vector3.one);
        if (wr) b.Encapsulate(wr.bounds);
        RoomBounds = b;

        // 9) Trigger sala al rectángulo
        var trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = transform.InverseTransformPoint(RoomBounds.center);
        trigger.size = RoomBounds.size;
    }

    // ----- Pared con secuencias personalizables -----
    void PlacePerimeterWallsCustom()
    {
        int xMax = roomSizeTiles.x - 1;
        int yMax = roomSizeTiles.y - 1;

        // Esquinas de la sala
        SetWallSafe(new Vector3Int(0, 0, 0), wallCornerSW, wallH);
        SetWallSafe(new Vector3Int(xMax, 0, 0), wallCornerSE, wallH);
        SetWallSafe(new Vector3Int(0, yMax, 0), wallCornerNW, wallH);
        SetWallSafe(new Vector3Int(xMax, yMax, 0), wallCornerNE, wallH);

        // Horizontales (arriba/abajo), sin esquinas
        int slotsTopBottom = Mathf.Max(0, roomSizeTiles.x - 2);
        for (int i = 0; i < slotsTopBottom; i++)
        {
            int x = 1 + i;
            var topTile = PickFromSeq(topEdge, i, slotsTopBottom);
            var bottomTile = PickFromSeq(bottomEdge, i, slotsTopBottom);
            SetWallSafe(new Vector3Int(x, 0, 0), bottomTile, wallH);
            SetWallSafe(new Vector3Int(x, yMax, 0), topTile, wallH);
        }

        // Verticales (izquierda/derecha), sin esquinas
        int slotsLeftRight = Mathf.Max(0, roomSizeTiles.y - 2);
        for (int i = 0; i < slotsLeftRight; i++)
        {
            int y = 1 + i;
            var leftTile = PickFromSeq(leftEdge, i, slotsLeftRight);
            var rightTile = PickFromSeq(rightEdge, i, slotsLeftRight);
            SetWallSafe(new Vector3Int(0, y, 0), leftTile, wallV);
            SetWallSafe(new Vector3Int(roomSizeTiles.x - 1, y, 0), rightTile, wallV);
        }
    }

    void SetWallSafe(Vector3Int cell, TileBase preferred, TileBase fallback)
    {
        wallsMap.SetTile(cell, preferred ? preferred : fallback);
    }

    TileBase PickFromSeq(List<TileBase> seq, int index, int slots)
    {
        if (seq == null || seq.Count == 0) return null;
        int len = seq.Count;
        if (len == 1) return seq[0];

        switch (sequenceFit)
        {
            case SeqFit.Repeat: return seq[index % len];
            case SeqFit.Clamp: return seq[Mathf.Clamp(index, 0, len - 1)];
            case SeqFit.Stretch:
                if (slots <= 1) return seq[0];
                float t = (float)index / (float)(slots - 1);
                int k = Mathf.RoundToInt(t * (len - 1));
                return seq[Mathf.Clamp(k, 0, len - 1)];
        }
        return seq[index % len];
    }

    // ----- Huecos de puerta (3 tiles) -----
    void CarveDoorways()
    {
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

        if (north)
            for (int dx = -1; dx <= 1; dx++)
                wallsMap.SetTile(new Vector3Int(midX + dx, roomSizeTiles.y - 1, 0), null);

        if (south)
            for (int dx = -1; dx <= 1; dx++)
                wallsMap.SetTile(new Vector3Int(midX + dx, 0, 0), null);

        if (east)
            for (int dy = -1; dy <= 1; dy++)
                wallsMap.SetTile(new Vector3Int(roomSizeTiles.x - 1, midY + dy, 0), null);

        if (west)
            for (int dy = -1; dy <= 1; dy++)
                wallsMap.SetTile(new Vector3Int(0, midY + dy, 0), null);
    }

    // ----- Thresholds (suelo rojo) en huecos -----
    void PlaceThresholds()
    {
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

        if (north && thresholdH)
            for (int dx = -1; dx <= 1; dx++)
                floorMap.SetTile(new Vector3Int(midX + dx, roomSizeTiles.y - 1, 0), thresholdH);

        if (south && thresholdH)
            for (int dx = -1; dx <= 1; dx++)
                floorMap.SetTile(new Vector3Int(midX + dx, 0, 0), thresholdH);

        if (east && thresholdV)
            for (int dy = -1; dy <= 1; dy++)
                floorMap.SetTile(new Vector3Int(roomSizeTiles.x - 1, midY + dy, 0), thresholdV);

        if (west && thresholdV)
            for (int dy = -1; dy <= 1; dy++)
                floorMap.SetTile(new Vector3Int(0, midY + dy, 0), thresholdV);
    }

    // ----- JAMBAS: esquinas de puerta en extremos del hueco -----
    void PlaceDoorJambCorners()
    {
        int xMax = roomSizeTiles.x - 1;
        int yMax = roomSizeTiles.y - 1;
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

        // Norte (fila yMax): extremos en x = midX-2 y midX+2
        if (north && yMax >= 0)
        {
            int y = yMax;
            int xl = Mathf.Clamp(midX - 2, 1, xMax - 1);
            int xr = Mathf.Clamp(midX + 2, 1, xMax - 1);
            if (doorNorthLeftCorner) wallsMap.SetTile(new Vector3Int(xl, y, 0), doorNorthLeftCorner);   // N_L
            if (doorNorthRightCorner) wallsMap.SetTile(new Vector3Int(xr, y, 0), doorNorthRightCorner);  // N_R
        }

        // Sur (fila 0): extremos en x = midX-2 y midX+2
        if (south)
        {
            int y = 0;
            int xl = Mathf.Clamp(midX - 2, 1, xMax - 1);
            int xr = Mathf.Clamp(midX + 2, 1, xMax - 1);
            if (doorSouthLeftCorner) wallsMap.SetTile(new Vector3Int(xl, y, 0), doorSouthLeftCorner);   // S_L
            if (doorSouthRightCorner) wallsMap.SetTile(new Vector3Int(xr, y, 0), doorSouthRightCorner);  // S_R
        }

        // Este (columna xMax): extremos en y = midY+2 (arriba) y midY-2 (abajo)
        if (east && xMax >= 0)
        {
            int x = xMax;
            int yt = Mathf.Clamp(midY + 2, 1, yMax - 1);
            int yb = Mathf.Clamp(midY - 2, 1, yMax - 1);
            if (doorEastTopCorner) wallsMap.SetTile(new Vector3Int(x, yt, 0), doorEastTopCorner);     // E_T
            if (doorEastBottomCorner) wallsMap.SetTile(new Vector3Int(x, yb, 0), doorEastBottomCorner);  // E_B
        }

        // Oeste (columna 0): extremos en y = midY+2 y midY-2
        if (west)
        {
            int x = 0;
            int yt = Mathf.Clamp(midY + 2, 1, yMax - 1);
            int yb = Mathf.Clamp(midY - 2, 1, yMax - 1);
            if (doorWestTopCorner) wallsMap.SetTile(new Vector3Int(x, yt, 0), doorWestTopCorner);     // W_T
            if (doorWestBottomCorner) wallsMap.SetTile(new Vector3Int(x, yb, 0), doorWestBottomCorner);  // W_B
        }
    }

    // ----- Borde de piso con caps (detección por línea) -----
    void DecorateFloorEdges()
    {
        bool any =
            floorEdgeN || floorEdgeS || floorEdgeE || floorEdgeW ||
            floorEdgeN_LeftCap || floorEdgeN_RightCap ||
            floorEdgeS_LeftCap || floorEdgeS_RightCap ||
            floorEdgeE_TopCap || floorEdgeE_BottomCap ||
            floorEdgeW_TopCap || floorEdgeW_BottomCap;

        if (!any) return;

        int xMax = roomSizeTiles.x - 1;
        int yMax = roomSizeTiles.y - 1;

        // Norte (fila justo debajo del muro superior)
        int yN = Mathf.Max(0, yMax - 1);
        for (int x = 1; x <= xMax - 1; x++)
        {
            var cell = new Vector3Int(x, yN, 0);
            if (IsThresholdCell(cell)) continue;

            bool above = wallsMap.HasTile(new Vector3Int(x, yN + 1, 0));
            if (!above) continue;

            bool aboveLeft = wallsMap.HasTile(new Vector3Int(x - 1, yN + 1, 0));
            bool aboveRight = wallsMap.HasTile(new Vector3Int(x + 1, yN + 1, 0));
            bool start = !aboveLeft;
            bool end = !aboveRight;

            TileBase edge = floorEdgeN;
            if (start && floorEdgeN_LeftCap) edge = floorEdgeN_LeftCap;
            else if (end && floorEdgeN_RightCap) edge = floorEdgeN_RightCap;

            if (edge) floorMap.SetTile(cell, edge);
        }

        // Sur (fila justo encima del muro inferior)
        int yS = 1;
        if (yMax >= 1)
        {
            for (int x = 1; x <= xMax - 1; x++)
            {
                var cell = new Vector3Int(x, yS, 0);
                if (IsThresholdCell(cell)) continue;

                bool below = wallsMap.HasTile(new Vector3Int(x, yS - 1, 0));
                if (!below) continue;

                bool belowLeft = wallsMap.HasTile(new Vector3Int(x - 1, yS - 1, 0));
                bool belowRight = wallsMap.HasTile(new Vector3Int(x + 1, yS - 1, 0));
                bool start = !belowLeft;
                bool end = !belowRight;

                TileBase edge = floorEdgeS;
                if (start && floorEdgeS_LeftCap) edge = floorEdgeS_LeftCap;
                else if (end && floorEdgeS_RightCap) edge = floorEdgeS_RightCap;

                if (edge) floorMap.SetTile(cell, edge);
            }
        }

        // Este (columna inmediatamente a la izquierda del muro derecho)
        int xE = Mathf.Max(0, xMax - 1);
        for (int y = 2; y <= yMax - 2; y++)
        {
            var cell = new Vector3Int(xE, y, 0);
            if (IsThresholdCell(cell)) continue;

            bool right = wallsMap.HasTile(new Vector3Int(xE + 1, y, 0));
            if (!right) continue;

            bool upRight = wallsMap.HasTile(new Vector3Int(xE + 1, y + 1, 0));
            bool downRight = wallsMap.HasTile(new Vector3Int(xE + 1, y - 1, 0));
            bool start = !upRight;
            bool end = !downRight;

            TileBase edge = floorEdgeE;
            if (start && floorEdgeE_TopCap) edge = floorEdgeE_TopCap;
            else if (end && floorEdgeE_BottomCap) edge = floorEdgeE_BottomCap;

            if (edge) floorMap.SetTile(cell, edge);
        }

        // Oeste (columna inmediatamente a la derecha del muro izquierdo)
        int xW = 1;
        if (xMax >= 1)
        {
            for (int y = 2; y <= yMax - 2; y++)
            {
                var cell = new Vector3Int(xW, y, 0);
                if (IsThresholdCell(cell)) continue;

                bool left = wallsMap.HasTile(new Vector3Int(xW - 1, y, 0));
                if (!left) continue;

                bool upLeft = wallsMap.HasTile(new Vector3Int(xW - 1, y + 1, 0));
                bool downLeft = wallsMap.HasTile(new Vector3Int(xW - 1, y - 1, 0));
                bool start = !upLeft;
                bool end = !downLeft;

                TileBase edge = floorEdgeW;
                if (start && floorEdgeW_TopCap) edge = floorEdgeW_TopCap;
                else if (end && floorEdgeW_BottomCap) edge = floorEdgeW_BottomCap;

                if (edge) floorMap.SetTile(cell, edge);
            }
        }
    }

    // --- FIX 1: bajo Door West Top Corner usar Floor Edge N Right Cap ---
    void ApplyWestTopBelowSpecialCap()
    {
        if (!west || floorEdgeN_RightCap == null) return;

        int yMax = roomSizeTiles.y - 1;
        int midY = roomSizeTiles.y / 2;

        // Door West Top Corner en (x=0, y=midY+2) → debajo: (0, midY+1)
        int x = 0;
        int y = Mathf.Clamp(midY + 1, 0, yMax);
        floorMap.SetTile(new Vector3Int(x, y, 0), floorEdgeN_RightCap);
    }

    // --- FIX 2: bajo Door East Top Corner usar Floor Edge N Left Cap ---
    void ApplyEastTopBelowSpecialCap()
    {
        if (!east || floorEdgeN_LeftCap == null) return;

        int xMax = roomSizeTiles.x - 1;
        int yMax = roomSizeTiles.y - 1;
        int midY = roomSizeTiles.y / 2;

        // Door East Top Corner en (x=xMax, y=midY+2) → debajo: (xMax, midY+1)
        int x = xMax;
        int y = Mathf.Clamp(midY + 1, 0, yMax);
        floorMap.SetTile(new Vector3Int(x, y, 0), floorEdgeN_LeftCap);
    }

    bool IsThresholdCell(Vector3Int cell)
    {
        var t = floorMap.GetTile(cell);
        return (t != null) && (t == thresholdH || t == thresholdV);
    }

    Vector3 CellCenterWorld(int x, int y)
    {
        var wp = wallsMap.CellToWorld(new Vector3Int(x, y, 0));
        var cs = wallsMap.layoutGrid.cellSize;
        return wp + new Vector3(cs.x, cs.y, 0) * 0.5f;
    }

    public Vector2 GetCellSize() => wallsMap.layoutGrid.cellSize;

    // ==== Anclas (centro y tamaño en mundo) para colocar barreras/puertas ====
    public IEnumerable<DoorSpawn> GetDoorSpawns()
    {
        var cs = GetCellSize();
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

        // Norte / Sur: hueco horizontal 3x1
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

        // Este / Oeste: hueco vertical 1x3
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
