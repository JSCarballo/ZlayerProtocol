// Assets/Scripts/UI/MinimapUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[DefaultExecutionOrder(100)] // corre después de los managers base
public class MinimapUI : MonoBehaviour
{
    [Header("Refs (requeridos)")]
    public RectTransform container; // Panel vacío donde instanciamos iconos

    [Header("Prefabs de iconos (VISITADA)")]
    public Image roomIconPrefab;
    public Image bossIconPrefab;
    public Image armoryIconPrefab;
    public Image playerIconPrefab;

    [Header("Prefabs de iconos (NO VISITADA, opcional)")]
    [Tooltip("Si se deja vacío, se usará el prefab de visitada con un tinte.")]
    public Image roomIconUnvisitedPrefab;
    public Image bossIconUnvisitedPrefab;
    public Image armoryIconUnvisitedPrefab;

    [Header("Fallback de NO VISITADA (si no asignas prefab)")]
    public Color unvisitedTint = new Color(1f, 1f, 1f, 0.6f);

    [Header("Layout")]
    public float cellSize = 20f;          // tamaño en píxeles por celda
    public bool autoCenter = true;        // centrar contenido al reconstruir
    public Vector2 manualOffset = Vector2.zero; // offset extra si lo quieres mover

    // --- Estructuras internas ---
    class IconEntry
    {
        public RectTransform root;
        public Image visited;
        public Image unvisited;
        public Vector2Int cell;
    }

    readonly Dictionary<Vector2Int, IconEntry> icons = new();
    Image playerIcon;

    Coroutine waitRoutine;

    void OnEnable()
    {
        ProcDungeonGenerator.OnGenerated += HandleGenerated;
        waitRoutine = StartCoroutine(EnsureRegistryAndBindThenRebuild());
    }

    void OnDisable()
    {
        ProcDungeonGenerator.OnGenerated -= HandleGenerated;
        if (waitRoutine != null) StopCoroutine(waitRoutine);

        var reg = DungeonMapRegistry.Instance;
        if (reg != null)
        {
            reg.OnRoomRegistered -= HandleRegistered;
            reg.OnRoomUpdated -= HandleUpdated;
            reg.OnPlayerEnteredRoom -= HandlePlayerEntered;
        }
    }

    IEnumerator EnsureRegistryAndBindThenRebuild()
    {
        while (DungeonMapRegistry.Instance == null) yield return null;

        var reg = DungeonMapRegistry.Instance;
        reg.OnRoomRegistered += HandleRegistered;
        reg.OnRoomUpdated += HandleUpdated;
        reg.OnPlayerEnteredRoom += HandlePlayerEntered;

        RebuildFromRegistry();
    }

    void HandleGenerated()
    {
        RebuildFromRegistry();
    }

    void RebuildFromRegistry()
    {
        if (!container) { Debug.LogWarning("[MinimapUI] 'container' no asignado."); return; }
        if (!roomIconPrefab) { Debug.LogWarning("[MinimapUI] 'roomIconPrefab' no asignado."); return; }

        foreach (Transform t in container) Destroy(t.gameObject);
        icons.Clear();
        playerIcon = null;

        var reg = DungeonMapRegistry.Instance;
        if (reg == null) return;

        var all = new List<DungeonMapRegistry.RoomInfo>(reg.AllRooms());
        if (all.Count == 0) return;

        // Bounds para auto-centro
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var info in all)
        {
            minX = Mathf.Min(minX, info.cell.x);
            minY = Mathf.Min(minY, info.cell.y);
            maxX = Mathf.Max(maxX, info.cell.x);
            maxY = Mathf.Max(maxY, info.cell.y);
        }

        Vector2 offset = Vector2.zero;
        if (autoCenter)
        {
            Vector2 centerCell = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            offset = -centerCell * cellSize;
        }
        offset += manualOffset;

        // Crear íconos por sala
        foreach (var info in all)
        {
            InstantiateRoomIcon(info, offset);
            HandleUpdated(info); // aplica discovered/visited
        }

        // Icono del jugador en la sala Start
        var startCell = FindStartCell(all);
        if (playerIconPrefab)
        {
            playerIcon = Instantiate(playerIconPrefab, container);
            playerIcon.rectTransform.anchoredPosition = GridToUI(startCell, offset);
        }
    }

    // --- Helpers de prefabs ---
    Image PickVisitedPrefab(DungeonMapRegistry.RoomInfo info)
    {
        if (info.isBoss && bossIconPrefab) return bossIconPrefab;
        if (info.isArmory && armoryIconPrefab) return armoryIconPrefab;
        return roomIconPrefab;
    }

    Image PickUnvisitedPrefab(DungeonMapRegistry.RoomInfo info)
    {
        if (info.isBoss && bossIconUnvisitedPrefab) return bossIconUnvisitedPrefab;
        if (info.isArmory && armoryIconUnvisitedPrefab) return armoryIconUnvisitedPrefab;
        return roomIconUnvisitedPrefab;
    }

    void InstantiateRoomIcon(DungeonMapRegistry.RoomInfo info, Vector2 offset)
    {
        // Crear root para la celda
        var rootGO = new GameObject($"Cell_{info.cell.x}_{info.cell.y}", typeof(RectTransform));
        var root = rootGO.GetComponent<RectTransform>();
        root.SetParent(container, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = GridToUI(info.cell, offset);
        root.localScale = Vector3.one;
        root.sizeDelta = Vector2.zero;

        // Prefabs
        var visitedPrefab = PickVisitedPrefab(info);
        var unvisitedPrefab = PickUnvisitedPrefab(info);

        // Imagen visitada
        var visited = Instantiate(visitedPrefab, root);
        visited.name = "Visited";
        visited.rectTransform.anchoredPosition = Vector2.zero;

        // Imagen no-visitada
        Image unvisited;
        if (unvisitedPrefab != null)
        {
            unvisited = Instantiate(unvisitedPrefab, root);
        }
        else
        {
            // Fallback: clonar el visitado y tintar
            unvisited = Instantiate(visitedPrefab, root);
            var c = unvisited.color; c = unvisitedTint; unvisited.color = c;
        }
        unvisited.name = "Unvisited";
        unvisited.rectTransform.anchoredPosition = Vector2.zero;

        var entry = new IconEntry
        {
            root = root,
            visited = visited,
            unvisited = unvisited,
            cell = info.cell
        };

        icons[info.cell] = entry;

        // Estado inicial (se ajusta nuevamente en HandleUpdated)
        root.gameObject.SetActive(info.discovered);
        visited.enabled = info.discovered && info.visited;
        unvisited.enabled = info.discovered && !info.visited;
    }

    void HandleRegistered(DungeonMapRegistry.RoomInfo info)
    {
        var offset = ComputeCurrentOffset();
        if (!icons.ContainsKey(info.cell))
            InstantiateRoomIcon(info, offset);
    }

    void HandleUpdated(DungeonMapRegistry.RoomInfo info)
    {
        if (icons.TryGetValue(info.cell, out var entry) && entry != null)
        {
            // Mostrar solo si está descubierta
            entry.root.gameObject.SetActive(info.discovered);

            if (info.discovered)
            {
                // Alternar entre visitada / no visitada
                if (entry.visited) entry.visited.enabled = info.visited;
                if (entry.unvisited) entry.unvisited.enabled = !info.visited;
            }
        }
    }

    void HandlePlayerEntered(Vector2Int cell)
    {
        var offset = ComputeCurrentOffset();

        if (!playerIcon && playerIconPrefab)
            playerIcon = Instantiate(playerIconPrefab, container);

        if (playerIcon)
            playerIcon.rectTransform.anchoredPosition = GridToUI(cell, offset);

        // Al entrar, la sala queda al menos descubierta/visible
        if (icons.TryGetValue(cell, out var entry) && entry != null)
        {
            entry.root.gameObject.SetActive(true);
            if (entry.unvisited) entry.unvisited.enabled = false;
            if (entry.visited) entry.visited.enabled = true;
        }
    }

    Vector2 GridToUI(Vector2Int cell, Vector2 offset)
    {
        return new Vector2(cell.x * cellSize, cell.y * cellSize) + offset;
    }

    Vector2Int FindStartCell(List<DungeonMapRegistry.RoomInfo> all)
    {
        foreach (var r in all) if (r.isStart) return r.cell;
        return Vector2Int.zero;
    }

    Vector2 ComputeCurrentOffset()
    {
        // Usa la primera entrada para recuperar el offset actual
        foreach (var kv in icons)
        {
            var cell = kv.Key;
            var pos = kv.Value.root.anchoredPosition;
            return pos - new Vector2(cell.x * cellSize, cell.y * cellSize);
        }
        return manualOffset;
    }
}
