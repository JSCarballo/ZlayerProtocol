// Assets/Scripts/UI/MinimapUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapUI : MonoBehaviour
{
    [Header("Layout")]
    public RectTransform content; // contenedor de iconos
    public Vector2 cellSize = new(12f, 12f);
    public float cellSpacing = 2f;

    [Header("Colors")]
    public Color colorUnknown = new(0.2f, 0.2f, 0.2f, 0.6f);
    public Color colorDiscovered = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Color colorVisited = Color.white;
    public Color colorCurrent = new(0.2f, 0.8f, 1f, 1f);
    public Color colorBoss = new(1f, 0.3f, 0.3f, 1f);
    public Color colorArmory = new(1f, 0.85f, 0.3f, 1f);

    [Header("Sprites (opcionales)")]
    public Sprite roomSprite;   // cuadrado
    public Sprite bossSprite;   // icono boss
    public Sprite armorySprite; // icono armory

    Dictionary<Vector2Int, Image> icons = new();
    Vector2Int currentCell;

    void Awake()
    {
        if (!content) content = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (!DungeonMapRegistry.Instance) return;
        DungeonMapRegistry.Instance.OnRoomRegistered += HandleReg;
        DungeonMapRegistry.Instance.OnRoomUpdated += HandleUpd;
        DungeonMapRegistry.Instance.OnPlayerEnteredRoom += HandleEnter;

        // Por si se habilita tarde:
        foreach (var r in DungeonMapRegistry.Instance.AllRooms()) HandleReg(r);
    }

    void OnDisable()
    {
        if (!DungeonMapRegistry.Instance) return;
        DungeonMapRegistry.Instance.OnRoomRegistered -= HandleReg;
        DungeonMapRegistry.Instance.OnRoomUpdated -= HandleUpd;
        DungeonMapRegistry.Instance.OnPlayerEnteredRoom -= HandleEnter;
    }

    void HandleReg(DungeonMapRegistry.RoomInfo info) => CreateOrUpdate(info);
    void HandleUpd(DungeonMapRegistry.RoomInfo info) => CreateOrUpdate(info);

    void HandleEnter(Vector2Int cell)
    {
        currentCell = cell;
        // repintar current
        foreach (var kv in icons) RefreshColor(kv.Key);
    }

    void CreateOrUpdate(DungeonMapRegistry.RoomInfo info)
    {
        if (!icons.TryGetValue(info.cell, out var img))
        {
            var go = new GameObject($"Mini_{info.cell.x}_{info.cell.y}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(content, false);
            img = go.GetComponent<Image>();
            img.raycastTarget = false;
            icons[info.cell] = img;

            var rt = img.rectTransform;
            rt.sizeDelta = cellSize;
            rt.anchoredPosition = GridToLocal(info.cell);
        }

        // sprite según tipo
        if (info.isBoss && bossSprite) img.sprite = bossSprite;
        else if (info.isArmory && armorySprite) img.sprite = armorySprite;
        else img.sprite = roomSprite;

        RefreshColor(info.cell, info);
    }

    void RefreshColor(Vector2Int cell)
    {
        if (!DungeonMapRegistry.Instance) return;
        if (!icons.TryGetValue(cell, out var img)) return;
        if (!DungeonMapRegistry.Instance.TryGet(cell, out var info)) return;
        RefreshColor(cell, info);
    }

    void RefreshColor(Vector2Int cell, DungeonMapRegistry.RoomInfo info)
    {
        if (!icons.TryGetValue(cell, out var img)) return;

        Color c;
        if (cell == currentCell) c = colorCurrent;
        else if (info.visited) c = info.isBoss ? colorBoss : (info.isArmory ? colorArmory : colorVisited);
        else if (info.discovered) c = colorDiscovered;
        else c = colorUnknown;

        img.color = c;
    }

    Vector2 GridToLocal(Vector2Int cell)
    {
        // coloca (0,0) en el centro visual del contenedor
        float stepX = cellSize.x + cellSpacing;
        float stepY = cellSize.y + cellSpacing;
        return new Vector2(cell.x * stepX, cell.y * stepY);
    }
}
