using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(RoomBuilder))]
[RequireComponent(typeof(BoxCollider2D))]
public class RoomRuntime : MonoBehaviour
{
    // ---------- Estado estático por piso ----------
    static bool s_InitialFocusDone = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ResetInitialFocusFlag() => s_InitialFocusDone = false;

    // Puntero a la StartRoom más reciente
    public static RoomRuntime LastStartRoom { get; private set; } = null;

    // Evento: avisamos cuando se marca la Start Room durante la generación
    public static event System.Action<RoomRuntime> OnStartRoomMarked;

    /// <summary>Resetea punteros/flags para un nuevo piso en la MISMA escena.</summary>
    public static void ResetStartPointerForNewFloor()
    {
        LastStartRoom = null;
        s_InitialFocusDone = false;
    }

    [Header("Spawning (enemigos)")]
    public GameObject enemyPrefab;
    public int minEnemies = 3, maxEnemies = 6;

    [Header("Puertas (barreras)")]
    public GameObject doorBarrierPrefab;
    public float doorThickness = 0.5f;
    public float doorPadding = 0.1f;
    public float doorInset = 0.05f;

    [Header("Puertas – Sprites visuales")]
    [Tooltip("Sprite para puertas horizontales (N/S) – visual más ALTO para dar perspectiva.")]
    public Sprite doorSpriteHorizontal;
    [Tooltip("Sprite para puertas verticales (E/W). Opcional.")]
    public Sprite doorSpriteVertical;
    [Tooltip("Multiplicador de altura visual SOLO para horizontales (no afecta al collider).")]
    public float doorH_VisualHeightMul = 2.0f;
    [Tooltip("Offset de sorting order respecto al floor para que la puerta quede por encima.")]
    public int doorSortingOrderOffset = +5;

    [Header("Transición de cámara")]
    public bool enableCamTransition = true;
    public float camTransitionDuration = 0.25f;
    public AnimationCurve camCurve = null;
    public bool camResizeDuringTransition = true;

    [Header("Presentación de spawn")]
    public bool syncFadeWithCamera = true;
    public float actorFadeDuration = 0.25f;
    public AnimationCurve actorFadeCurve = null;

    [Header("Pull-in del jugador (siempre al entrar)")]
    public float pullDistance = 1.3f;
    public float pullDuration = 0.16f;

    [Header("Boss")]
    public bool isBossRoom = false;
    public GameObject bossPrefab;
    public bool isFinalBossFloor = false;

    [Header("Recompensas Boss (offsets)")]
    public Vector2 elevatorOffset = new Vector2(0f, +1.2f);
    public Vector2 bossUpgradeOffset = new Vector2(0f, -1.2f);

    [Header("Armory")]
    public bool isArmoryRoom = false;
    public GameObject upgradePickupPrefab;

    [Header("Ascensor")]
    public GameObject elevatorExitPrefab;

    [Header("Armory (opciones)")]
    public bool armoryChoicesUsePool = true;
    public int armoryChoices = 3;
    public float armoryChoiceSpacing = 2.2f;
    public float armoryChoiceYOffset = 0f;
    public float armoryChoiceLifetime = 9999f;

    [Header("Spawn Safety")]
    public bool useEntrySafeZone = true;
    public float entrySafeDepth = 3.0f;
    public float entrySafeExtraWidth = 0.5f;
    public float minSpawnDistFromPlayer = 1.5f;

    [Header("Arena (Piso 1B)")]
    public bool overrideToArena = false;
    public int arenaWaves = 3;
    public int arenaWaveMin = 6;
    public int arenaWaveMax = 10;
    public float arenaInterval = 1.0f;

    // ---------- Overlay de color al cerrar puertas ----------
    [Header("Overlay de color al cerrar puertas")]
    public bool useRoomOverlay = true;
    [Tooltip("Sprite plano/gradiente. Si se deja vacío, se genera uno 1x1 blanco.")]
    public Sprite overlaySprite;
    [Tooltip("Material del overlay (recomendado multiplicativo/aditivo para efecto de filtro).")]
    public Material overlayMaterial;
    [Tooltip("Color cuando la sala está ABIERTA (normalmente transparente).")]
    public Color overlayOpenColor = new Color(1f, 1f, 1f, 0f);
    [Tooltip("Color cuando la sala está CERRADA (tinte rojo por defecto).")]
    public Color overlayClosedColor = new Color(1f, 0f, 0f, 0.22f);
    [Tooltip("Escala extra relativa a RoomBounds para cubrir con holgura todo el cuarto.")]
    [Range(1f, 1.8f)] public float overlayExtraScale = 1.1f;
    [Tooltip("Offset de sorting order respecto al piso (grande para estar encima de TODO).")]
    public int overlaySortingOrderOffset = 1000;

    RoomBuilder builder;
    BoxCollider2D triggerCol;

    bool visited = false;
    bool isStartRoom = false;

    int aliveCount = 0;
    bool upgradeSpawned = false;
    readonly List<GameObject> spawnedActors = new();
    readonly List<GameObject> spawnedDoors = new();

    // Armory runtime
    readonly List<WeaponUpgradePickup> currentArmoryChoices = new();
    float armoryChoicesTimer = 0f;
    bool armoryChoiceResolved = false;

    // Arena runtime
    bool arenaActive = false;
    bool arenaTriggered = false;
    int wavesLeft = 0;
    Vector3 lastEntryPos;

    // Overlay runtime
    SpriteRenderer overlaySR;
    static readonly int _ColorProp = Shader.PropertyToID("_Color");

    void Awake()
    {
        builder = GetComponent<RoomBuilder>();
        triggerCol = GetComponent<BoxCollider2D>();
        if (camCurve == null) camCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        EnsureTriggerCollider();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            builder = GetComponent<RoomBuilder>();
            triggerCol = GetComponent<BoxCollider2D>();
        }
        if (builder && triggerCol) EnsureTriggerCollider();

        // Refrescar overlay en Play al cambiar colores en el inspector
        if (Application.isPlaying && overlaySR != null)
            ApplyOverlayColor(false); // no forzar re-escala
    }

    void EnsureTriggerCollider()
    {
        if (!triggerCol) triggerCol = gameObject.AddComponent<BoxCollider2D>();
        var b = builder.RoomBounds;
        triggerCol.isTrigger = true;
        triggerCol.usedByComposite = false;
        triggerCol.offset = (Vector2)(b.center - transform.position);
        triggerCol.size = b.size;
    }

    void Start()
    {
        // --- Foco inicial de cámara tras cargar piso ---
        StartCoroutine(InitialAutoFocusIfPlayerInside());
    }

    System.Collections.IEnumerator InitialAutoFocusIfPlayerInside()
    {
        if (s_InitialFocusDone) yield break;
        yield return null; // 1 frame para asegurar spawn de Player y generación

        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) yield break;

        Vector3 p = player.transform.position;
        if (!builder.RoomBounds.Contains(p)) yield break;

        if (CameraRoomLock.Instance)
            CameraRoomLock.Instance.SnapToRoom(builder.RoomBounds);

        visited = true;
        DungeonMapRegistry.Instance?.NotifyPlayerEnteredRoom(gridCell);
        DungeonMapRegistry.Instance?.SetVisited(gridCell);

        s_InitialFocusDone = true;
    }

    public void MarkAsStartRoom()
    {
        isStartRoom = true;
        visited = true;

        // Matar enemigos que hayan quedado dentro por error
        var enemies = Object.FindObjectsByType<EnemyChaseAI>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (builder.RoomBounds.Contains(e.transform.position))
            {
                var hp = e.GetComponent<Health>();
                if (hp) hp.Damage(99999);
                else Destroy(e.gameObject);
            }
        }

        // Puntero global + evento (para warp robusto en FloorFlowController)
        LastStartRoom = this;
        OnStartRoomMarked?.Invoke(this);
    }

    public void ConfigureAsBossRoom(GameObject bossRef)
    {
        isBossRoom = true;
        bossPrefab = bossRef;
        minEnemies = 0; maxEnemies = 0;
    }

    public void ConfigureAsArmoryRoom(GameObject upgradePrefab)
    {
        isArmoryRoom = true;
        upgradePickupPrefab = upgradePrefab;
        minEnemies = 0; maxEnemies = 0;
    }

    void Update()
    {
        if (isArmoryRoom && currentArmoryChoices.Count > 0 && !armoryChoiceResolved && armoryChoiceLifetime < 9999f)
        {
            armoryChoicesTimer += Time.deltaTime;
            if (armoryChoicesTimer >= armoryChoiceLifetime)
                ClearArmoryChoices(null, false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        DungeonMapRegistry.Instance?.NotifyPlayerEnteredRoom(gridCell);

        // 1B (follow activo): solo Snap para clamp correcto
        if (CameraRoomLock.Instance && CameraRoomLock.Instance.IsFollowActive)
        {
            CameraRoomLock.Instance.SnapToRoom(builder.RoomBounds);
        }
        else
        {
            if (enableCamTransition && CameraRoomLock.Instance)
                StartCoroutine(CameraRoomLock.Instance.PanToRoom(builder.RoomBounds, camTransitionDuration, camCurve, camResizeDuringTransition));
            else
                CameraRoomLock.Instance?.SnapToRoom(builder.RoomBounds);
        }

        StartCoroutine(EnterAnyRoomSequence(other.transform));
    }

    System.Collections.IEnumerator EnterAnyRoomSequence(Transform player)
    {
        if (player == null) yield break;

        var locker = player.GetComponent<PlayerControlLocker>();
        if (locker) locker.HardLock();

        lastEntryPos = player.position;
        yield return StartCoroutine(PullPlayerIn(player, pullDistance, pullDuration));

        bool firstVisit = !visited;

        // Armory (primera vez)
        if (firstVisit && isArmoryRoom && upgradePickupPrefab)
        {
            if (armoryChoicesUsePool) SpawnArmoryChoices();
            else { SpawnUpgradePickup(builder.RoomBounds.center + (Vector3)bossUpgradeOffset); upgradeSpawned = true; }
        }

        // Arena 1B
        if (overrideToArena && !arenaTriggered)
        {
            arenaTriggered = true;
            arenaActive = true;
            wavesLeft = Mathf.Max(1, arenaWaves);
            SpawnDoorBarriers();
            yield return StartCoroutine(SpawnNextArenaWave());
        }
        else
        {
            // Encuentro normal/boss (solo primera visita y si no es Armory)
            int count = (firstVisit && !isArmoryRoom) ? (isBossRoom ? 1 : UnityEngine.Random.Range(minEnemies, maxEnemies + 1)) : 0;

            List<Bounds> exclusionZones = new();
            if (firstVisit && count > 0 && useEntrySafeZone)
                if (TryBuildEntrySafeZone(lastEntryPos, out Bounds safe)) exclusionZones.Add(safe);

            if (firstVisit && count > 0)
            {
                SpawnDoorBarriers();
                SpawnActors(count, exclusionZones, player.position);

                float fadeDur = syncFadeWithCamera ? camTransitionDuration : actorFadeDuration;
                if (fadeDur <= 0f) RevealActorsInstant();
                else
                {
                    var fade = StartCoroutine(FadeInActors(fadeDur, actorFadeCurve));
                    if (enableCamTransition && CameraRoomLock.Instance)
                        yield return CameraRoomLock.Instance.PanToRoom(builder.RoomBounds, 0f, camCurve, camResizeDuringTransition);
                    yield return fade;
                }
                ReleaseActors();
            }
            else
            {
                if (enableCamTransition && CameraRoomLock.Instance)
                    yield return CameraRoomLock.Instance.PanToRoom(builder.RoomBounds, 0f, camCurve, camResizeDuringTransition);
            }
        }

        if (firstVisit)
        {
            visited = true;
            DungeonMapRegistry.Instance?.SetVisited(gridCell);
        }

        if (locker) locker.HardUnlock();
    }

    // ---------- Arena ----------
    System.Collections.IEnumerator SpawnNextArenaWave()
    {
        yield return null;

        if (wavesLeft <= 0)
        {
            foreach (var d in spawnedDoors) if (d) Destroy(d);
            spawnedDoors.Clear();

            if (elevatorExitPrefab)
                Instantiate(elevatorExitPrefab, builder.RoomBounds.center + (Vector3)elevatorOffset, Quaternion.identity);

            // Overlay vuelve a abierto (sin rojo)
            SetOverlayClosed(false);

            arenaActive = false;
            yield break;
        }

        int count = UnityEngine.Random.Range(Mathf.Max(1, arenaWaveMin), Mathf.Max(arenaWaveMin, arenaWaveMax) + 1);

        List<Bounds> exclusion = new();
        if (useEntrySafeZone && TryBuildEntrySafeZone(lastEntryPos, out Bounds safe))
            exclusion.Add(safe);

        SpawnActors(count, exclusion, lastEntryPos);

        float fadeDur = syncFadeWithCamera ? camTransitionDuration : actorFadeDuration;
        if (fadeDur <= 0f) RevealActorsInstant();
        else
        {
            var fade = StartCoroutine(FadeInActors(fadeDur, actorFadeCurve));
            if (enableCamTransition && CameraRoomLock.Instance)
                yield return CameraRoomLock.Instance.PanToRoom(builder.RoomBounds, 0f, camCurve, camResizeDuringTransition);
            yield return fade;
        }
        ReleaseActors();
    }

    // ---------- Armory ----------
    void SpawnArmoryChoices()
    {
        ClearArmoryChoices(null, false);
        currentArmoryChoices.Clear();
        armoryChoiceResolved = false;
        armoryChoicesTimer = 0f;

        if (!UpgradePoolManager.Instance)
        {
            SpawnUpgradePickup(builder.RoomBounds.center + (Vector3)bossUpgradeOffset);
            return;
        }

        if (!UpgradePoolManager.Instance.SampleCandidates(armoryChoices, out var candidates))
        {
            SpawnUpgradePickup(builder.RoomBounds.center + (Vector3)bossUpgradeOffset);
            return;
        }

        Vector3 center = builder.RoomBounds.center + new Vector3(0f, armoryChoiceYOffset, 0f);
        int n = candidates.Count;
        float totalSpan = (n - 1) * armoryChoiceSpacing;

        for (int i = 0; i < n; i++)
        {
            float x = -totalSpan * 0.5f + i * armoryChoiceSpacing;
            Vector3 pos = center + new Vector3(x, 0f, 0f);

            var go = Instantiate(upgradePickupPrefab, pos, Quaternion.identity);
            var p = go.GetComponent<WeaponUpgradePickup>();
            if (!p) continue;

            p.useScriptable = true;
            p.Assign(candidates[i]);

            p.onPicked += (so) =>
            {
                if (armoryChoiceResolved) return;
                armoryChoiceResolved = true;

                if (UpgradePoolManager.Instance && so != null)
                {
                    var wso = so as WeaponUpgradeSO;
                    if (wso != null)
                        UpgradePoolManager.Instance.MarkUsed(wso);
                }

                ClearArmoryChoices(p, true);
            };

            currentArmoryChoices.Add(p);
        }
    }

    void ClearArmoryChoices(WeaponUpgradePickup chosen, bool consumeSelected)
    {
        foreach (var p in currentArmoryChoices)
        {
            if (!p) continue;
            if (chosen != null && p == chosen) continue;
            Destroy(p.gameObject);
        }
        currentArmoryChoices.Clear();
    }

    // ---------- Puertas ----------
    void SpawnDoorBarriers()
    {
        if (!doorBarrierPrefab) { Debug.LogWarning("[RoomRuntime] doorBarrierPrefab no asignado."); return; }

        spawnedDoors.Clear();

        // Asegurar overlay (lo creamos aquí para que RoomBounds ya esté disponible)
        EnsureOverlayObject();

        foreach (var ds in builder.GetDoorSpawns())
        {
            GetDoorOrientation(builder.RoomBounds, ds.center, ds.size, out bool horizontal, out Vector2 inwardNormal);
            Vector2 finalSize = ComputeBarrierSize(ds.size, horizontal, doorThickness, doorPadding);
            Vector3 finalPos = ds.center + (Vector3)(inwardNormal * doorInset);

            var go = Instantiate(doorBarrierPrefab, finalPos, Quaternion.identity, transform);

            // Collider al tamaño real de bloqueo
            var box = go.GetComponent<BoxCollider2D>();
            if (box) box.size = finalSize;

            // Sprite visual y escala (ancho encaja; alto aumentado en horizontales)
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr)
            {
                // Sorting por encima del piso
                var fr = builder.floorMap ? builder.floorMap.GetComponent<TilemapRenderer>() : null;
                if (fr)
                {
                    sr.sortingLayerID = fr.sortingLayerID;
                    sr.sortingOrder = fr.sortingOrder + doorSortingOrderOffset;
                }

                if (horizontal && doorSpriteHorizontal != null)
                    sr.sprite = doorSpriteHorizontal;
                else if (!horizontal && doorSpriteVertical != null)
                    sr.sprite = doorSpriteVertical;

                if (sr.sprite != null)
                {
                    Vector2 spSize = sr.sprite.bounds.size;
                    float width = finalSize.x;
                    float height = finalSize.y * (horizontal ? Mathf.Max(1f, doorH_VisualHeightMul) : 1f);

                    go.transform.localScale = new Vector3(
                        width / Mathf.Max(0.0001f, spSize.x),
                        height / Mathf.Max(0.0001f, spSize.y),
                        1f
                    );
                }
            }

            go.gameObject.layer = LayerMask.NameToLayer("Walls");
            spawnedDoors.Add(go);
        }

        // Activar overlay rojo (cerrado)
        SetOverlayClosed(true);
    }

    public static Vector2 ComputeBarrierSize(Vector2 holeSize, bool horizontal, float thickness, float padding)
    {
        if (horizontal)
        {
            float length = Mathf.Max(0.01f, holeSize.x - 2f * padding);
            float thick = Mathf.Max(0.01f, thickness);
            return new Vector2(length, thick);
        }
        else
        {
            float length = Mathf.Max(0.01f, holeSize.y - 2f * padding);
            float thick = Mathf.Max(0.01f, thickness);
            return new Vector2(thick, length);
        }
    }

    public static void GetDoorOrientation(Bounds room, Vector3 center, Vector2 holeSize, out bool horizontal, out Vector2 inwardNormal)
    {
        horizontal = holeSize.x >= holeSize.y;
        float toTop = Mathf.Abs(room.max.y - center.y);
        float toBottom = Mathf.Abs(center.y - room.min.y);
        float toRight = Mathf.Abs(room.max.x - center.x);
        float toLeft = Mathf.Abs(center.x - room.min.x);

        inwardNormal = horizontal
            ? (toTop < toBottom ? Vector2.down : Vector2.up)
            : (toRight < toLeft ? Vector2.left : Vector2.right);
    }

    // ---------- Safe zone ----------
    bool TryBuildEntrySafeZone(Vector3 entryPos, out Bounds safe)
    {
        safe = new Bounds();

        RoomBuilder.DoorSpawn? closest = null;
        float best = float.MaxValue;
        foreach (var ds in builder.GetDoorSpawns())
        {
            float d = Vector2.SqrMagnitude((Vector2)entryPos - (Vector2)ds.center);
            if (d < best) { best = d; closest = ds; }
        }
        if (closest == null) return false;

        var dsC = closest.Value.center;
        var dsS = closest.Value.size;

        GetDoorOrientation(builder.RoomBounds, dsC, dsS, out bool horizontal, out Vector2 inward);

        Vector2 size;
        if (horizontal)
        {
            float width = dsS.x + 2f * entrySafeExtraWidth;
            size = new Vector2(width, Mathf.Max(0.05f, entrySafeDepth));
        }
        else
        {
            float height = dsS.y + 2f * entrySafeExtraWidth;
            size = new Vector2(Mathf.Max(0.05f, entrySafeDepth), height);
        }

        Vector3 center = dsC + (Vector3)(inward * (doorInset + (horizontal ? size.y : size.x) * 0.5f));
        safe = new Bounds(center, new Vector3(size.x, size.y, 1f));
        return true;
    }

    // ---------- Spawns ----------
    void SpawnActors(int count, List<Bounds> exclusionZones, Vector3 playerPos)
    {
        spawnedActors.Clear();
        if (isBossRoom)
        {
            Vector3 pos = FindFreePoint(builder.RoomBounds, 1.0f, exclusionZones, playerPos, minSpawnDistFromPlayer, 30);
            var b = Instantiate(bossPrefab ? bossPrefab : enemyPrefab, pos, Quaternion.identity);
            PrepareIntroState(b);
            HookDeath(b);
            aliveCount = 1;
            spawnedActors.Add(b);
            return;
        }

        aliveCount = count;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = FindFreePoint(builder.RoomBounds, 1.0f, exclusionZones, playerPos, minSpawnDistFromPlayer, 30);
            var e = Instantiate(enemyPrefab, pos, Quaternion.identity);
            PrepareIntroState(e);
            HookDeath(e);
            spawnedActors.Add(e);
        }
    }

    Vector3 FindFreePoint(Bounds area, float margin, List<Bounds> forbidden, Vector3 playerPos, float minDistFromPlayer, int tries = 20)
    {
        for (int i = 0; i < tries; i++)
        {
            float x = UnityEngine.Random.Range(area.min.x + margin, area.max.x - margin);
            float y = UnityEngine.Random.Range(area.min.y + margin, area.max.y - margin);
            Vector3 p = new Vector3(x, y, 0);

            bool insideForbidden = false;
            if (forbidden != null)
            {
                for (int f = 0; f < forbidden.Count; f++)
                    if (forbidden[f].Contains(p)) { insideForbidden = true; break; }
            }
            if (insideForbidden) continue;
            if (Vector2.Distance(playerPos, p) < minDistFromPlayer) continue;
            return p;
        }
        return area.center;
    }

    void PrepareIntroState(GameObject go)
    {
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
        { var c = sr.color; c.a = 0f; sr.color = c; }
        var ai = go.GetComponent<EnemyChaseAI>(); if (ai) ai.enabled = false;
        var touch = go.GetComponent<DamagePlayerOnTouch>(); if (touch) touch.enabled = false;
        foreach (var col in go.GetComponents<Collider2D>()) col.enabled = false;
    }

    void RevealActorsInstant()
    {
        foreach (var a in spawnedActors)
        {
            if (!a) continue;
            foreach (var sr in a.GetComponentsInChildren<SpriteRenderer>())
            { var c = sr.color; c.a = 1f; sr.color = c; }
        }
    }

    System.Collections.IEnumerator FadeInActors(float duration, AnimationCurve curve)
    {
        if (duration <= 0f) { RevealActorsInstant(); yield break; }

        float t = 0f;
        var bundles = new List<(SpriteRenderer sr, Color start, Color end)>();
        foreach (var a in spawnedActors)
            foreach (var sr in a.GetComponentsInChildren<SpriteRenderer>())
            { var c0 = sr.color; var c1 = c0; c1.a = 1f; bundles.Add((sr, c0, c1)); }

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = (curve != null) ? curve.Evaluate(k) : k;
            foreach (var b in bundles) if (b.sr) b.sr.color = Color.Lerp(b.start, b.end, e);
            yield return null;
        }
        foreach (var b in bundles) if (b.sr) b.sr.color = b.end;
    }

    void ReleaseActors()
    {
        foreach (var a in spawnedActors)
        {
            if (!a) continue;
            var ai = a.GetComponent<EnemyChaseAI>(); if (ai) ai.enabled = true;
            var touch = a.GetComponent<DamagePlayerOnTouch>(); if (touch) touch.enabled = true;
            foreach (var col in a.GetComponents<Collider2D>()) col.enabled = true;
        }
    }

    void HookDeath(GameObject go)
    {
        var hp = go.GetComponent<Health>();
        if (hp != null) hp.OnDeath += OnActorDeath;
    }

    void OnActorDeath()
    {
        aliveCount--;
        if (aliveCount > 0) return;

        if (arenaActive)
        {
            wavesLeft--;
            if (wavesLeft > 0) { StartCoroutine(ArenaNextWaveDelay()); return; }
            StartCoroutine(SpawnNextArenaWave()); // última: abre y spawnea ascensor
            return;
        }

        foreach (var d in spawnedDoors) if (d) Destroy(d);
        spawnedDoors.Clear();

        // Overlay vuelve a abierto (sin rojo)
        SetOverlayClosed(false);

        foreach (var a in spawnedActors)
        {
            if (!a) continue;
            var hp = a.GetComponent<Health>();
            if (hp != null) hp.OnDeath -= OnActorDeath;
        }
        spawnedActors.Clear();

        if (isBossRoom)
        {
            if (upgradePickupPrefab && !upgradeSpawned)
            {
                SpawnUpgradePickup(builder.RoomBounds.center + (Vector3)bossUpgradeOffset);
                upgradeSpawned = true;
            }

            if (isFinalBossFloor)
            {
                FloorFlowController.Instance?.WinGame();
            }
            else
            {
                if (elevatorExitPrefab)
                    Instantiate(elevatorExitPrefab, builder.RoomBounds.center + (Vector3)elevatorOffset, Quaternion.identity);
            }
        }
    }

    System.Collections.IEnumerator ArenaNextWaveDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, arenaInterval));
        yield return StartCoroutine(SpawnNextArenaWave());
    }

    void SpawnUpgradePickup(Vector3 pos)
    {
        if (!upgradePickupPrefab) return;

        var go = Instantiate(upgradePickupPrefab, pos, Quaternion.identity);
        var pickup = go.GetComponent<WeaponUpgradePickup>();
        if (!pickup) return;

        if (UpgradePoolManager.Instance)
        {
            if (UpgradePoolManager.Instance.DrawRandomUnique(out var so))
            {
                pickup.useScriptable = true;
                pickup.Assign(so);
            }
            else pickup.useScriptable = true;
        }
        else pickup.useScriptable = true;
    }

    System.Collections.IEnumerator PullPlayerIn(Transform player, float distance, float duration)
    {
        if (player == null || duration <= 0f || distance <= 0f) yield break;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        Vector3 center = builder.RoomBounds.center;
        Vector2 dir = ((Vector2)(center - player.position)).normalized;
        if (dir.sqrMagnitude < 0.0001f) yield break;

        Vector3 start = player.position;
        Vector3 target = start + (Vector3)(dir * distance);
        target = ClampInside(builder.RoomBounds, target, 0.5f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            Vector3 pos = Vector3.Lerp(start, target, k);
            if (rb) rb.MovePosition(pos); else player.position = pos;
            yield return null;
        }
        if (rb) rb.MovePosition(target); else player.position = target;
    }

    static Vector3 ClampInside(Bounds b, Vector3 p, float margin)
    {
        float x = Mathf.Clamp(p.x, b.min.x + margin, b.max.x - margin);
        float y = Mathf.Clamp(p.y, b.min.y, b.max.y - margin);
        return new Vector3(x, y, p.z);
    }

    // ---------- Overlay helpers ----------
    void EnsureOverlayObject()
    {
        if (!useRoomOverlay) return;

        if (overlaySR == null)
        {
            var go = new GameObject("RoomOverlay");
            go.transform.SetParent(transform, false);
            overlaySR = go.AddComponent<SpriteRenderer>();

            // Material instanciado para poder tocar _Color sin afectar otros overlays
            if (overlayMaterial != null)
                overlaySR.material = new Material(overlayMaterial);

            // Sprite por defecto (1x1 blanco) si no asignaste uno
            if (overlaySprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply(false, true);
                overlaySprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            overlaySR.sprite = overlaySprite;
        }

        // Sorting por encima del piso (y de todo lo demás)
        var fr = builder.floorMap ? builder.floorMap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>() : null;
        if (fr)
        {
            overlaySR.sortingLayerID = fr.sortingLayerID;
            overlaySR.sortingOrder = fr.sortingOrder + overlaySortingOrderOffset;
        }
        else
        {
            overlaySR.sortingOrder = 10000;
        }

        // Posición/escala para cubrir el cuarto con holgura
        var rb = builder.RoomBounds;
        overlaySR.transform.position = rb.center;
        var spSize = overlaySR.sprite.bounds.size;
        float sx = (rb.size.x * overlayExtraScale) / Mathf.Max(0.0001f, spSize.x);
        float sy = (rb.size.y * overlayExtraScale) / Mathf.Max(0.0001f, spSize.y);
        overlaySR.transform.localScale = new Vector3(sx, sy, 1f);

        // Al crear, arranca en estado "abierto"
        ApplyOverlayColor(false);
    }

    void SetOverlayClosed(bool closed)
    {
        if (!useRoomOverlay) return;
        EnsureOverlayObject();  // por si aún no existe
        var c = closed ? overlayClosedColor : overlayOpenColor;
        ApplyOverlayColor(true, c);
    }

    void ApplyOverlayColor(bool forceRescale, Color? custom = null)
    {
        if (!overlaySR) return;

        // Recalcular escala (por si el bounds cambió durante la generación)
        if (forceRescale)
        {
            var rb = builder.RoomBounds;
            overlaySR.transform.position = rb.center;
            var spSize = overlaySR.sprite.bounds.size;
            float sx = (rb.size.x * overlayExtraScale) / Mathf.Max(0.0001f, spSize.x);
            float sy = (rb.size.y * overlayExtraScale) / Mathf.Max(0.0001f, spSize.y);
            overlaySR.transform.localScale = new Vector3(sx, sy, 1f);
        }

        var c = custom ?? overlayOpenColor;
        overlaySR.color = c;

        var mat = overlaySR.material;
        if (mat != null && mat.HasProperty(_ColorProp))
            mat.SetColor(_ColorProp, c);
    }

    // ---------- Grid / mapa ----------
    [HideInInspector] public Vector2Int gridCell;

    // ---------- API de spawn recomendado para FloorFlowController ----------
    /// <summary>
    /// Punto recomendado para aparecer en esta sala al entrar por elevador/cambio de piso.
    /// Usa el centro; si hay puertas, empuja ligeramente hacia adentro según la normal.
    /// </summary>
    public Vector3 GetRecommendedSpawnPoint(float inset)
    {
        var rb = builder.RoomBounds;
        Vector3 center = rb.center;

        // Si tenemos anclas de puerta, elige la que tiene la normal "hacia adentro" y desplaza un poco
        RoomBuilder.DoorSpawn? any = null;
        foreach (var ds in builder.GetDoorSpawns()) { any = ds; break; }

        if (any.HasValue)
        {
            GetDoorOrientation(rb, any.Value.center, any.Value.size, out bool horizontal, out Vector2 inward);
            return any.Value.center + (Vector3)(inward * Mathf.Max(0f, inset));
        }

        return center;
    }
}
