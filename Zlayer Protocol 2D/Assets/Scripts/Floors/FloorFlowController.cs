using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorFlowController : MonoBehaviour
{
    public static FloorFlowController Instance { get; private set; }

    [Header("Secuencia de pisos")]
    public FloorSequenceSO sequence;

    [Header("Refs en escena")]
    public ProcDungeonGenerator generator;

    public int currentIndex { get; private set; } = -1;
    public FloorDefinitionSO Current =>
        (sequence && currentIndex >= 0 && currentIndex < sequence.floors.Count) ? sequence.floors[currentIndex] : null;

    [Header("Spawn en Start Room")]
    [Tooltip("Margen para clamping interno; solo seguridad al colocar en el centro.")]
    public float startClampMargin = 0.5f;

    [Header("Robustez de warp")]
    [Tooltip("Espera máxima para que la Start Room tenga bounds válidos.")]
    public float maxWaitSeconds = 6f;
    [Tooltip("Frames consecutivos en los que los bounds deben verse estables antes de warpear.")]
    public int stableFramesRequired = 2;

    // Evitar dobles warps por piso
    bool warpDoneThisFloor = false;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        ProcDungeonGenerator.OnGenerated += OnDungeonGenerated;

        // Señal temprana desde la StartRoom
        RoomRuntime.OnStartRoomMarked += HandleStartRoomMarked;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        ProcDungeonGenerator.OnGenerated -= OnDungeonGenerated;
        RoomRuntime.OnStartRoomMarked -= HandleStartRoomMarked;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;

        generator = FindObjectOfType<ProcDungeonGenerator>();
        if (!sequence || sequence.floors.Count == 0)
        {
            Debug.LogError("[FloorFlow] No hay FloorSequence asignada.");
            return;
        }

        if (SaveManager.HasSave())
        {
            if (currentIndex < 0) currentIndex = 0;
            BuildCurrentFloor();
        }
        else
        {
            ResetRunState();
            NextFloor();
        }
    }

    /// <summary>Resetea la corrida (antes de una nueva partida).</summary>
    public void ResetRunState()
    {
        currentIndex = -1;
        DungeonMapRegistry.Instance?.ClearAll();
        CleanupSceneLeftovers();

        RoomRuntime.ResetStartPointerForNewFloor();
        warpDoneThisFloor = false;
    }

    /// <summary>Avanza al siguiente piso y lo genera.</summary>
    public void NextFloor()
    {
        if (!sequence || sequence.floors.Count == 0)
        {
            Debug.LogError("[FloorFlow] No hay FloorSequence asignada.");
            return;
        }

        currentIndex++;
        if (currentIndex >= sequence.floors.Count)
        {
            WinGame();
            return;
        }

        BuildCurrentFloor();
    }

    /// <summary>Reconstruye el piso del índice actual sin modificar el índice.</summary>
    void BuildCurrentFloor()
    {
        DungeonMapRegistry.Instance?.ClearAll();
        CleanupSceneLeftovers();

        if (!generator) generator = FindObjectOfType<ProcDungeonGenerator>();
        if (!generator)
        {
            Debug.LogError("[FloorFlow] No encuentro ProcDungeonGenerator en la escena.");
            return;
        }

        var cfg = Current;
        if (cfg == null)
        {
            Debug.LogError($"[FloorFlow] currentIndex {currentIndex} sin FloorDefinition válido.");
            return;
        }

        RoomRuntime.ResetStartPointerForNewFloor();
        warpDoneThisFloor = false;

        // Genera (warp se hará por evento de StartRoom y/o fallback OnGenerated)
        generator.GenerateForFloor(cfg);
        Debug.Log($"[FloorFlow] Entrando a piso {cfg.floorId} – {cfg.displayName} (index={currentIndex})");
    }

    // === Señales ===

    void HandleStartRoomMarked(RoomRuntime startRoom)
    {
        if (warpDoneThisFloor || startRoom == null) return;
        StartCoroutine(WarpToStartCenter_Co(startRoom));
    }

    void OnDungeonGenerated()
    {
        if (warpDoneThisFloor) return;
        StartCoroutine(FallbackWarpAfterGenerated_Co());
    }

    IEnumerator FallbackWarpAfterGenerated_Co()
    {
        float timeout = maxWaitSeconds;
        float t = 0f;
        RoomRuntime startRoom = null;

        while (t < timeout && startRoom == null)
        {
            startRoom = RoomRuntime.LastStartRoom ?? FindStartFromRegistry();
            if (startRoom == null) { t += Time.unscaledDeltaTime; yield return null; }
        }
        if (startRoom != null)
            yield return WarpToStartCenter_Co(startRoom);
    }

    // === Warp robusto al CENTRO de la Start Room ===
    IEnumerator WarpToStartCenter_Co(RoomRuntime startRoom)
    {
        if (warpDoneThisFloor) yield break;

        var builder = startRoom ? startRoom.GetComponent<RoomBuilder>() : null;
        yield return EnsureBoundsReady_Co(builder, maxWaitSeconds, stableFramesRequired);

        // Holguras extra para física/render/tilemaps
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        yield return null;

        // Player
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (!playerGO) yield break;

        var locker = playerGO.GetComponent<PlayerControlLocker>();
        if (locker) locker.HardLock();

        Vector3 spawn = (builder != null) ? builder.RoomBounds.center : playerGO.transform.position;
        if (builder != null) spawn = ClampInside(builder.RoomBounds, spawn, startClampMargin);

        var rb = playerGO.GetComponent<Rigidbody2D>();
        if (rb)
        {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.position = (Vector2)spawn;
        }
        else
        {
            playerGO.transform.position = spawn;
        }

        // Cam a la Start Room
        if (builder && CameraRoomLock.Instance)
            CameraRoomLock.Instance.SnapToRoom(builder.RoomBounds);

        if (locker) locker.HardUnlock();

        warpDoneThisFloor = true;
    }

    IEnumerator EnsureBoundsReady_Co(RoomBuilder builder, float maxWait, int stableFrames)
    {
        float t = 0f;
        // Espera a que exista builder
        while (builder == null && t < maxWait)
        {
            builder = FindObjectOfType<RoomBuilder>();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (builder == null) yield break;

        // Espera a que los bounds tengan área
        t = 0f;
        while (t < maxWait && (builder.RoomBounds.size.x * builder.RoomBounds.size.y) < 0.001f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Requiere estabilidad N frames
        int stable = 0;
        Vector3 lastSize = builder.RoomBounds.size;
        while (stable < Mathf.Max(1, stableFrames) && t < maxWait)
        {
            yield return null;
            t += Time.unscaledDeltaTime;

            var curSize = builder.RoomBounds.size;
            if (Approximately(curSize, lastSize))
                stable++;
            else
            {
                stable = 0;
                lastSize = curSize;
            }
        }
    }

    bool Approximately(Vector3 a, Vector3 b)
    {
        const float eps = 0.0005f;
        return Mathf.Abs(a.x - b.x) < eps && Mathf.Abs(a.y - b.y) < eps;
    }

    Vector3 ClampInside(Bounds b, Vector3 p, float margin)
    {
        float x = Mathf.Clamp(p.x, b.min.x + margin, b.max.x - margin);
        float y = Mathf.Clamp(p.y, b.min.y + margin, b.max.y - margin);
        return new Vector3(x, y, p.z);
    }

    // === Helpers ===
    RoomRuntime FindStartFromRegistry()
    {
        var reg = DungeonMapRegistry.Instance;
        if (reg == null) return null;

        foreach (var info in reg.AllRooms())
        {
            if (!info.isStart) continue;
            var rooms = Object.FindObjectsByType<RoomRuntime>(FindObjectsSortMode.None);
            for (int i = 0; i < rooms.Length; i++)
                if (rooms[i].gridCell == info.cell)
                    return rooms[i];
        }
        return null;
    }

    void CleanupSceneLeftovers()
    {
        var elevators = Object.FindObjectsByType<ElevatorExit>(FindObjectsSortMode.None);
        foreach (var e in elevators) if (e) Destroy(e.gameObject);

        var upgrades = Object.FindObjectsByType<WeaponUpgradePickup>(FindObjectsSortMode.None);
        foreach (var u in upgrades) if (u) Destroy(u.gameObject);
    }

    public void WinGame()
    {
        Debug.Log("[FloorFlow] ¡Has vencido a la Reina! GAME CLEAR.");
        GameFlowController.Instance?.OnVictory();
        // SaveManager.ClearSave(); // opcional
    }
}
