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
    [Tooltip("Margen para clamping interno; seguridad al colocar en el centro.")]
    public float startClampMargin = 0.5f;

    [Header("Robustez / espera")]
    [Tooltip("Espera máxima para que la Start Room tenga bounds válidos.")]
    public float maxWaitSeconds = 8f;
    [Tooltip("Frames consecutivos con bounds estables antes de warpear.")]
    public int stableFramesRequired = 2;

    [Header("Pantalla de carga")]
    public LoadingScreenController loadingScreen;
    public Sprite[] loadingArtCandidates;
    [Tooltip("Tiempo mínimo visible de la pantalla de carga.")]
    public float minLoadingSeconds = 1.0f;

    // Estado interno
    bool warpDoneThisFloor = false;
    bool manualLoading = false; // usado para ignorar warps por eventos mientras usamos loading

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        ProcDungeonGenerator.OnGenerated += OnDungeonGenerated;
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

    public void ResetRunState()
    {
        currentIndex = -1;
        DungeonMapRegistry.Instance?.ClearAll();
        CleanupSceneLeftovers();

        RoomRuntime.ResetStartPointerForNewFloor();
        warpDoneThisFloor = false;
        manualLoading = false;
    }

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

        generator.GenerateForFloor(cfg);
        Debug.Log($"[FloorFlow] Entrando a piso {cfg.floorId} – {cfg.displayName} (index={currentIndex})");
    }

    // =========================
    //   Integración con Elevador
    // =========================
    /// <summary>Llamar desde ElevatorExit cuando el jugador entra.</summary>
    public void UI_EnterElevator_AndLoadNextFloor()
    {
        if (!isActiveAndEnabled) return;
        StartCoroutine(EnterElevatorAndLoadNextFloor_Co());
    }

    IEnumerator EnterElevatorAndLoadNextFloor_Co()
    {
        if (!sequence || sequence.floors.Count == 0) yield break;

        // Lock jugador
        var player = GameObject.FindGameObjectWithTag("Player");
        var locker = player ? player.GetComponent<PlayerControlLocker>() : null;
        if (locker) locker.HardLock();

        // Mostrar pantalla de carga
        manualLoading = true;
        float shownAt = Time.unscaledTime;
        if (loadingScreen)
        {
            var art = PickRandomArt();
            loadingScreen.Show(art);
        }

        // Avanzar índice y generar
        currentIndex++;
        if (currentIndex >= sequence.floors.Count)
        {
            WinGame();
            yield break;
        }

        // Generación "manual" (no confiar en warp por eventos mientras manualLoading)
        DungeonMapRegistry.Instance?.ClearAll();
        CleanupSceneLeftovers();

        if (!generator) generator = FindObjectOfType<ProcDungeonGenerator>();
        if (!generator)
        {
            Debug.LogError("[FloorFlow] No encuentro ProcDungeonGenerator en la escena.");
            yield break;
        }

        var cfg = Current;
        if (cfg == null)
        {
            Debug.LogError($"[FloorFlow] Índice {currentIndex} sin FloorDefinition.");
            yield break;
        }

        RoomRuntime.ResetStartPointerForNewFloor();
        warpDoneThisFloor = false;

        bool floorGenerated = false;
        System.Action genCb = () => floorGenerated = true;
        ProcDungeonGenerator.OnGenerated += genCb;

        generator.GenerateForFloor(cfg);

        // Esperar a que se haya generado y exista StartRoom
        float t = 0f;
        while (t < maxWaitSeconds && (!floorGenerated || RoomRuntime.LastStartRoom == null))
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Asegurar bounds estables
        if (RoomRuntime.LastStartRoom != null)
        {
            var rb = RoomRuntime.LastStartRoom.GetComponent<RoomBuilder>();
            yield return EnsureBoundsReady_Co(rb, maxWaitSeconds, stableFramesRequired);

            // Holguras para física/tilemaps
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();
            yield return null;

            // Warp al CENTRO exacto de la StartRoom
            Vector3 spawn = rb ? ClampInside(rb.RoomBounds, rb.RoomBounds.center, startClampMargin) : Vector3.zero;

            if (player)
            {
                var prb = player.GetComponent<Rigidbody2D>();
                if (prb)
                {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
                    prb.linearVelocity = Vector2.zero;
#else
                    prb.velocity = Vector2.zero;
#endif
                    prb.position = (Vector2)spawn;
                }
                else player.transform.position = spawn;
            }

            if (rb && CameraRoomLock.Instance)
                CameraRoomLock.Instance.SnapToRoom(rb.RoomBounds);
        }

        ProcDungeonGenerator.OnGenerated -= genCb;

        // Mantener pantalla el mínimo tiempo
        float minHold = Mathf.Max(0f, minLoadingSeconds - (Time.unscaledTime - shownAt));
        if (minHold > 0f) yield return new WaitForSecondsRealtime(minHold);

        if (loadingScreen) loadingScreen.Hide();

        if (locker) locker.HardUnlock();

        warpDoneThisFloor = true;
        manualLoading = false;
    }

    Sprite PickRandomArt()
    {
        if (loadingArtCandidates != null && loadingArtCandidates.Length > 0)
        {
            int i = Random.Range(0, loadingArtCandidates.Length);
            return loadingArtCandidates[i];
        }
        return null;
    }

    // =========================
    //   Señales normales (fallback)
    // =========================
    void HandleStartRoomMarked(RoomRuntime startRoom)
    {
        if (manualLoading) return;           // estamos en flujo manual
        if (warpDoneThisFloor || startRoom == null) return;
        StartCoroutine(WarpToStartCenter_Co(startRoom));
    }

    void OnDungeonGenerated()
    {
        if (manualLoading) return;           // estamos en flujo manual
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

    IEnumerator WarpToStartCenter_Co(RoomRuntime startRoom)
    {
        if (warpDoneThisFloor) yield break;

        var builder = startRoom ? startRoom.GetComponent<RoomBuilder>() : null;
        yield return EnsureBoundsReady_Co(builder, maxWaitSeconds, stableFramesRequired);

        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        yield return null;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (!playerGO) yield break;

        var locker = playerGO.GetComponent<PlayerControlLocker>();
        if (locker) locker.HardLock();

        Vector3 spawn = (builder != null) ? ClampInside(builder.RoomBounds, builder.RoomBounds.center, startClampMargin) : playerGO.transform.position;

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

        if (builder && CameraRoomLock.Instance)
            CameraRoomLock.Instance.SnapToRoom(builder.RoomBounds);

        if (locker) locker.HardUnlock();

        warpDoneThisFloor = true;
    }

    IEnumerator EnsureBoundsReady_Co(RoomBuilder builder, float maxWait, int stableFrames)
    {
        float t = 0f;
        while (builder == null && t < maxWait)
        {
            builder = FindObjectOfType<RoomBuilder>();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (builder == null) yield break;

        t = 0f;
        while (t < maxWait && (builder.RoomBounds.size.x * builder.RoomBounds.size.y) < 0.001f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        int stable = 0;
        Vector3 last = builder.RoomBounds.size;
        while (stable < Mathf.Max(1, stableFrames) && t < maxWait)
        {
            yield return null;
            t += Time.unscaledDeltaTime;

            var cur = builder.RoomBounds.size;
            if (Approximately(cur, last)) stable++;
            else { stable = 0; last = cur; }
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
    }
}
