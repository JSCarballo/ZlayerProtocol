using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomBuilder : MonoBehaviour
{
    [Header("Tilemap refs")]
    public Tilemap floorMap;
    public Tilemap wallsMap;

    // =======================
    //  FLOOR: Mezcla de 3 tiles
    // =======================
    [Header("Floor mix (3 tiles con pesos)")]
    public TileBase floorA;                 // tile 1
    public TileBase floorB;                 // tile 2
    public TileBase floorC;                 // tile 3
    [Range(0f, 1f)] public float weightA = 0.65f;
    [Range(0f, 1f)] public float weightB = 0.25f;
    [Range(0f, 1f)] public float weightC = 0.10f;

    [Tooltip("Usa Perlin/Value noise para generar parches (no ruido pixel a pixel).")]
    public bool usePerlin = true;
    [Tooltip("Frecuencia del ruido (menor → parches más grandes).")]
    [Range(0.02f, 2f)] public float noiseScale = 0.18f;
    [Tooltip("Semilla para mezcla determinística por habitación.")]
    public int noiseSeed = 1337;

    [Header("Floor fallback (opcional si falta alguno de A/B/C)")]
    public TileBase floorFallback;

    // =======================
    //  FLOOR EDGES / CAPS
    // =======================
    [Header("Floor edges (opcional)")]
    public TileBase floorEdgeN, floorEdgeS, floorEdgeE, floorEdgeW;

    [Header("Floor Edge Caps (terminaciones opcionales)")]
    public TileBase floorEdgeN_LeftCap;
    public TileBase floorEdgeN_RightCap;
    public TileBase floorEdgeS_LeftCap;
    public TileBase floorEdgeS_RightCap;
    public TileBase floorEdgeE_TopCap;
    public TileBase floorEdgeE_BottomCap;
    public TileBase floorEdgeW_TopCap;
    public TileBase floorEdgeW_BottomCap;

    // =======================
    //  WALLS / PUERTAS
    // =======================
    [Header("Walls – Fallback genéricos")]
    public TileBase wallH;   // tramo horizontal (arriba/abajo)
    public TileBase wallV;   // tramo vertical (izquierda/derecha)

    [Header("Walls – Esquinas de la SALA")]
    public TileBase wallCornerNW, wallCornerNE, wallCornerSE, wallCornerSW;

    [Header("Walls – Secuencias personalizables (excluyen esquinas de sala)")]
    public List<TileBase> topEdge = new();
    public List<TileBase> bottomEdge = new();
    public List<TileBase> leftEdge = new();
    public List<TileBase> rightEdge = new();

    public enum SeqFit { Repeat, Clamp, Stretch }
    [Header("Ajuste de secuencias")]
    public SeqFit sequenceFit = SeqFit.Repeat;

    [Header("Door threshold (suelo rojo)")]
    public TileBase thresholdH;                // umbral horizontal (N/S)
    public TileBase thresholdV;                // umbral vertical (E/W)

    [Header("Walls – Esquinas de PUERTA (JAMBAS)")]
    public TileBase doorNorthLeftCorner;   // N_L
    public TileBase doorNorthRightCorner;  // N_R
    public TileBase doorSouthLeftCorner;   // S_L
    public TileBase doorSouthRightCorner;  // S_R
    public TileBase doorEastTopCorner;     // E_T
    public TileBase doorEastBottomCorner;  // E_B
    public TileBase doorWestTopCorner;     // W_T
    public TileBase doorWestBottomCorner;  // W_B

    // =======================
    //  LUZ CENTRAL
    // =======================
    [Header("Luz central (SpriteRenderer)")]
    public bool enableCenterLight = true;
    [Tooltip("Sprite con gradiente circular suave (un 'soft circle').")]
    public Sprite centerLightSprite;
    [Tooltip("Material para la luz (recomendado ADITIVO). Se instancia en runtime.")]
    public Material centerLightMaterial;
    [Tooltip("Tamaño de la luz en unidades mundo.")]
    public Vector2 centerLightWorldSize = new Vector2(6f, 6f);
    [Tooltip("Color cuando la sala está abierta.")]
    public Color lightOpenColor = Color.white;
    [Tooltip("Color cuando la sala está cerrada.")]
    public Color lightClosedColor = new Color(1f, 0.2f, 0.2f, 1f);
    [Tooltip("Offset vertical opcional (para alejarla del HUD).")]
    public float centerLightYOffset = 0f;
    [Tooltip("Orden de render (sobre el piso, bajo personajes).")]
    public int centerLightSortingOrderOffset = +1;

    // =======================
    //  GEOMETRÍA / CONEXIONES
    // =======================
    [Header("Geometry")]
    public Vector2Int roomSizeTiles = new(16, 10); // ancho x alto (tiles)

    [HideInInspector] public bool north, south, east, west;

    public Bounds RoomBounds { get; private set; }

    public struct DoorSpawn
    {
        public Vector3 center;
        public Vector2 size;
        public DoorSpawn(Vector3 c, Vector2 s) { center = c; size = s; }
    }

    // Runtime: luz + estado
    SpriteRenderer _centerLightSR;
    bool _lightClosed = false;
    static readonly int _ColorProp = Shader.PropertyToID("_Color");

    // ---------------- Build ----------------
    public void Build()
    {
        floorMap.ClearAllTiles();
        wallsMap.ClearAllTiles();

        // 1) Piso base (mezcla de 3 tiles)
        FillFloorMixed();

        // 2) Pared perimetral
        PlacePerimeterWallsCustom();

        // 3) Huecos de puerta
        CarveDoorways();

        // 4) Thresholds
        PlaceThresholds();

        // 5) Jambas (esquinas de puerta)
        PlaceDoorJambCorners();

        // 6) Borde de piso + caps
        DecorateFloorEdges();

        // 7) FIX específicos bajo jambas superiores West/East
        ApplyWestTopBelowSpecialCap(); // usa floorEdgeN_RightCap
        ApplyEastTopBelowSpecialCap(); // usa floorEdgeN_LeftCap

        // 8) Bounds
        floorMap.CompressBounds();
        wallsMap.CompressBounds();
        var fr = floorMap.GetComponent<TilemapRenderer>();
        var wr = wallsMap.GetComponent<TilemapRenderer>();
        Bounds b = fr ? fr.bounds : new Bounds(transform.position, Vector3.one);
        if (wr) b.Encapsulate(wr.bounds);
        RoomBounds = b;

        // 9) Trigger sala
        var trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = transform.InverseTransformPoint(RoomBounds.center);
        trigger.size = RoomBounds.size;

        // 10) Luz central (inicia en color abierto)
        EnsureCenterLight();
        _lightClosed = false;
        UpdateCenterLightVisual();
    }

    void OnValidate()
    {
        // Refresca la luz cuando cambias colores en el inspector
        if (Application.isPlaying && _centerLightSR != null)
            UpdateCenterLightVisual();
    }

    // =======================
    //  Piso mixto (3 tiles)
    // =======================
    void FillFloorMixed()
    {
        // Si falta alguno, usa fallback único
        if (floorA == null || floorB == null || floorC == null)
        {
            var t = floorA ?? floorB ?? floorC ?? floorFallback;
            if (t == null) return;
            for (int y = 0; y < roomSizeTiles.y; y++)
                for (int x = 0; x < roomSizeTiles.x; x++)
                    floorMap.SetTile(new Vector3Int(x, y, 0), t);
            return;
        }

        // Normalizamos pesos
        float sum = Mathf.Max(0.0001f, weightA + weightB + weightC);
        float pA = weightA / sum;
        float pB = weightB / sum;

        float ox = (noiseSeed * 0.1234f) % 1000f;
        float oy = (noiseSeed * 0.5678f) % 1000f;

        for (int y = 0; y < roomSizeTiles.y; y++)
        {
            for (int x = 0; x < roomSizeTiles.x; x++)
            {
                float r;
                if (usePerlin)
                {
                    float nx = (x + ox) * noiseScale;
                    float ny = (y + oy) * noiseScale;
                    r = Mathf.PerlinNoise(nx, ny);
                }
                else
                {
                    uint h = (uint)(x * 374761393 + y * 668265263) ^ (uint)noiseSeed;
                    h = (h ^ (h >> 13)) * 1274126177;
                    r = ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
                }

                TileBase pick = (r < pA) ? floorA : (r < pA + pB ? floorB : floorC);
                floorMap.SetTile(new Vector3Int(x, y, 0), pick);
            }
        }
    }

    // =======================
    //  Pared con secuencias
    // =======================
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

    // =======================
    //  Huecos y thresholds
    // =======================
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

    // =======================
    //  Jambas (esquinas puerta)
    // =======================
    void PlaceDoorJambCorners()
    {
        int xMax = roomSizeTiles.x - 1;
        int yMax = roomSizeTiles.y - 1;
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

        if (north && yMax >= 0)
        {
            int y = yMax;
            int xl = Mathf.Clamp(midX - 2, 1, xMax - 1);
            int xr = Mathf.Clamp(midX + 2, 1, xMax - 1);
            if (doorNorthLeftCorner) wallsMap.SetTile(new Vector3Int(xl, y, 0), doorNorthLeftCorner);
            if (doorNorthRightCorner) wallsMap.SetTile(new Vector3Int(xr, y, 0), doorNorthRightCorner);
        }

        if (south)
        {
            int y = 0;
            int xl = Mathf.Clamp(midX - 2, 1, xMax - 1);
            int xr = Mathf.Clamp(midX + 2, 1, xMax - 1);
            if (doorSouthLeftCorner) wallsMap.SetTile(new Vector3Int(xl, y, 0), doorSouthLeftCorner);
            if (doorSouthRightCorner) wallsMap.SetTile(new Vector3Int(xr, y, 0), doorSouthRightCorner);
        }

        if (east && xMax >= 0)
        {
            int x = xMax;
            int yt = Mathf.Clamp(midY + 2, 1, yMax - 1);
            int yb = Mathf.Clamp(midY - 2, 1, yMax - 1);
            if (doorEastTopCorner) wallsMap.SetTile(new Vector3Int(x, yt, 0), doorEastTopCorner);
            if (doorEastBottomCorner) wallsMap.SetTile(new Vector3Int(x, yb, 0), doorEastBottomCorner);
        }

        if (west)
        {
            int x = 0;
            int yt = Mathf.Clamp(midY + 2, 1, yMax - 1);
            int yb = Mathf.Clamp(midY - 2, 1, yMax - 1);
            if (doorWestTopCorner) wallsMap.SetTile(new Vector3Int(x, yt, 0), doorWestTopCorner);
            if (doorWestBottomCorner) wallsMap.SetTile(new Vector3Int(x, yb, 0), doorWestBottomCorner);
        }
    }

    // =======================
    //  Floor edges con caps
    // =======================
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

        // Norte
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

        // Sur
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

        // Este
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

        // Oeste
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

    // =======================
    //  FIX jambas superiores
    // =======================
    void ApplyWestTopBelowSpecialCap()
    {
        if (!west || floorEdgeN_RightCap == null) return;
        int yMax = roomSizeTiles.y - 1;
        int midY = roomSizeTiles.y / 2;
        floorMap.SetTile(new Vector3Int(0, Mathf.Clamp(midY + 1, 0, yMax), 0), floorEdgeN_RightCap);
    }

    void ApplyEastTopBelowSpecialCap()
    {
        if (!east || floorEdgeN_LeftCap == null) return;
        int xMax = roomSizeTiles.x - 1;
        int yMax = roomSizeTiles.y - 1;
        int midY = roomSizeTiles.y / 2;
        floorMap.SetTile(new Vector3Int(xMax, Mathf.Clamp(midY + 1, 0, yMax), 0), floorEdgeN_LeftCap);
    }

    // =======================
    //  LUZ CENTRAL
    // =======================
    void EnsureCenterLight()
    {
        if (!enableCenterLight || centerLightSprite == null) return;

        if (_centerLightSR == null)
        {
            var go = new GameObject("CenterLight");
            go.transform.SetParent(transform, false);
            _centerLightSR = go.AddComponent<SpriteRenderer>();
            // NO asignamos un material nuevo aquí: usaremos el que ya traiga el renderer
            // (Unity asigna por defecto el material de Sprites si no hay uno específico).
        }

        _centerLightSR.sprite = centerLightSprite;

        // Sorting por encima del piso
        var fr = floorMap ? floorMap.GetComponent<TilemapRenderer>() : null;
        if (fr)
        {
            _centerLightSR.sortingLayerID = fr.sortingLayerID;
            _centerLightSR.sortingOrder = fr.sortingOrder + centerLightSortingOrderOffset;
        }

        // Posición y escala a RoomBounds
        Vector3 c = RoomBounds.center + new Vector3(0f, centerLightYOffset, 0f);
        _centerLightSR.transform.position = c;

        var spSize = _centerLightSR.sprite.bounds.size;
        float sx = centerLightWorldSize.x / Mathf.Max(0.0001f, spSize.x);
        float sy = centerLightWorldSize.y / Mathf.Max(0.0001f, spSize.y);
        _centerLightSR.transform.localScale = new Vector3(sx, sy, 1f);
    }

    void UpdateCenterLightVisual()
    {
        if (!enableCenterLight || _centerLightSR == null) return;

        Color c = _lightClosed ? lightClosedColor : lightOpenColor;

        // 1) Color del SpriteRenderer (sirve para Sprites/Default)
        _centerLightSR.color = c;

        // 2) También intentamos escribir en el material ya asignado al renderer
        //    (URP usa _BaseColor; Sprites/Default usa _Color; otros shaders pueden usar _TintColor / _EmissionColor)
        var mat = _centerLightSR.material; // material de instancia en este renderer
        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", c);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c);
        }
    }

    /// Llama esto desde RoomRuntime cuando las puertas se cierran/abren.
    /// 'true' = rojo (cerrado), 'false' = abierto (blanco u otro que definas).
    public void SetCenterLightClosed(bool closed)
    {
        _lightClosed = closed;
        UpdateCenterLightVisual();
    }

    // =======================
    //  Utilidades
    // =======================
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

    public IEnumerable<DoorSpawn> GetDoorSpawns()
    {
        var cs = GetCellSize();
        int midX = roomSizeTiles.x / 2;
        int midY = roomSizeTiles.y / 2;

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
