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

    [Header("Prefabs de iconos (Image)")]
    public Image roomIconPrefab;
    public Image bossIconPrefab;
    public Image armoryIconPrefab;
    public Image playerIconPrefab;

    [Header("Layout")]
    public float cellSize = 20f;          // tamaño en píxeles por celda
    public bool autoCenter = true;        // centrar contenido al reconstruir
    public Vector2 manualOffset = Vector2.zero; // offset extra si lo quieres mover

    readonly Dictionary<Vector2Int, Image> icons = new();
    Image playerIcon;

    Coroutine waitRoutine;

    void OnEnable()
    {
        // Siempre quedamos escuchando la señal del generador (esté o no el registry)
        ProcDungeonGenerator.OnGenerated += HandleGenerated;

        // Arrancamos una rutina que espera a que aparezca el Registry y entonces se suscribe
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
        // Espera hasta que exista el Registry
        while (DungeonMapRegistry.Instance == null) yield return null;

        var reg = DungeonMapRegistry.Instance;
        reg.OnRoomRegistered += HandleRegistered;
        reg.OnRoomUpdated += HandleUpdated;
        reg.OnPlayerEnteredRoom += HandlePlayerEntered;

        RebuildFromRegistry(); // ahora sí, hay datos para leer
    }

    void HandleGenerated()
    {
        // Al finalizar la generación del piso, rehacemos el mapa
        RebuildFromRegistry();
    }

    void RebuildFromRegistry()
    {
        if (!container) { Debug.LogWarning("[MinimapUI] 'container' no asignado."); return; }
        if (!roomIconPrefab) { Debug.LogWarning("[MinimapUI] 'roomIconPrefab' no asignado."); return; }

        // Limpia elementos previos
        foreach (Transform t in container) Destroy(t.gameObject);
        icons.Clear();
        playerIcon = null;

        var reg = DungeonMapRegistry.Instance;
        if (reg == null) return;

        var all = new List<DungeonMapRegistry.RoomInfo>(reg.AllRooms());
        if (all.Count == 0) return; // el generador aún no registró nada (HandleGenerated volverá a llamar)

        // Calcular bounds en celdas para auto-centrar
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

        // Instanciar todos los íconos
        foreach (var info in all)
        {
            InstantiateRoomIcon(info, offset);
            HandleUpdated(info); // aplica discovered/visited
        }

        // Player icon en la sala Start (si ya existe, OnPlayerEntered lo actualizará)
        var startCell = FindStartCell(all);
        if (playerIconPrefab)
        {
            playerIcon = Instantiate(playerIconPrefab, container);
            playerIcon.rectTransform.anchoredPosition = GridToUI(startCell, offset);
        }
    }

    void InstantiateRoomIcon(DungeonMapRegistry.RoomInfo info, Vector2 offset)
    {
        Image prefab = roomIconPrefab;
        if (info.isBoss && bossIconPrefab) prefab = bossIconPrefab;
        else if (info.isArmory && armoryIconPrefab) prefab = armoryIconPrefab;

        var img = Instantiate(prefab, container);
        img.gameObject.name = $"Room_{info.cell.x}_{info.cell.y}";
        img.rectTransform.anchoredPosition = GridToUI(info.cell, offset);
        img.enabled = info.discovered;
        icons[info.cell] = img;
    }

    void HandleRegistered(DungeonMapRegistry.RoomInfo info)
    {
        var offset = ComputeCurrentOffset();
        if (!icons.ContainsKey(info.cell))
            InstantiateRoomIcon(info, offset);
    }

    void HandleUpdated(DungeonMapRegistry.RoomInfo info)
    {
        if (icons.TryGetValue(info.cell, out var img) && img)
        {
            img.enabled = info.discovered;
            img.color = info.visited ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }
    }

    void HandlePlayerEntered(Vector2Int cell)
    {
        var offset = ComputeCurrentOffset();
        if (!playerIcon && playerIconPrefab) playerIcon = Instantiate(playerIconPrefab, container);
        if (playerIcon) playerIcon.rectTransform.anchoredPosition = GridToUI(cell, offset);
        if (icons.TryGetValue(cell, out var img) && img) img.enabled = true;
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
        foreach (var kv in icons)
        {
            var cell = kv.Key;
            var pos = kv.Value.rectTransform.anchoredPosition;
            return pos - new Vector2(cell.x * cellSize, cell.y * cellSize);
        }
        return manualOffset;
    }
}
