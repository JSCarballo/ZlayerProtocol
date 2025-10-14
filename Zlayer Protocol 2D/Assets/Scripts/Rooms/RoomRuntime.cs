using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RoomBuilder))]
[RequireComponent(typeof(BoxCollider2D))]
public class RoomRuntime : MonoBehaviour
{
    [Header("Spawning (enemigos)")]
    public GameObject enemyPrefab;
    public int minEnemies = 3, maxEnemies = 6;

    [Header("Puertas (barreras)")]
    public GameObject doorBarrierPrefab;
    public float doorThickness = 0.5f;
    public float doorPadding = 0.1f;
    public float doorInset = 0.05f;

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

    [HideInInspector] public Vector2Int gridCell;

    RoomBuilder builder;
    BoxCollider2D triggerCol;

    bool visited = false;
    bool isStartRoom = false;

    int aliveCount = 0;
    bool upgradeSpawned = false;
    readonly List<GameObject> spawnedActors = new();
    readonly List<GameObject> spawnedDoors = new();

    // Armory runtime
    readonly List<WeaponUpgradePickup> currentArmoryPickups = new();
    float armoryChoicesTimer = 0f;
    bool armoryChoiceResolved = false;
    bool armorySubscribed = false;

    // Arena runtime
    bool arenaActive = false;
    bool arenaTriggered = false;
    int wavesLeft = 0;
    Vector3 lastEntryPos;

    void Awake()
    {
        builder = GetComponent<RoomBuilder>();
        triggerCol = GetComponent<BoxCollider2D>();
        if (camCurve == null) camCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        EnsureTriggerCollider();
    }

    void OnDestroy() => UnsubscribeArmory();

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            builder = GetComponent<RoomBuilder>();
            triggerCol = GetComponent<BoxCollider2D>();
        }
        if (builder && triggerCol) EnsureTriggerCollider();
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

    public void MarkAsStartRoom()
    {
        isStartRoom = true;
        visited = true;

        var enemies = UnityEngine.Object.FindObjectsByType<EnemyChaseAI>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (builder.RoomBounds.Contains(e.transform.position))
            {
                var hp = e.GetComponent<Health>();
                if (hp) hp.Damage(99999); else Destroy(e.gameObject);
            }
        }
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
        if (isArmoryRoom && currentArmoryPickups.Count > 0 && !armoryChoiceResolved && armoryChoiceLifetime < 9999f)
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

        if (CameraRoomLock.Instance && CameraRoomLock.Instance.IsFollowActive)
            CameraRoomLock.Instance.SnapToRoom(builder.RoomBounds);
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

        // Armory
        if (firstVisit && isArmoryRoom && upgradePickupPrefab)
        {
            if (armoryChoicesUsePool) SpawnArmoryChoices();
            else
            {
                SpawnUpgradePickup(builder.RoomBounds.center + (Vector3)bossUpgradeOffset, fromBoss: false);
                upgradeSpawned = true;
            }
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
            // Encuentro normal/boss (primera visita y no Armory)
            int count = (firstVisit && !isArmoryRoom)
                ? (isBossRoom ? 1 : UnityEngine.Random.Range(minEnemies, maxEnemies + 1))
                : 0;

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
        armoryChoiceResolved = false;
        armoryChoicesTimer = 0f;
        currentArmoryPickups.Clear();

        List<WeaponUpgradeSO> candidates;
        if (!UpgradePoolManager.Instance || !UpgradePoolManager.Instance.SampleCandidates(armoryChoices, out candidates) || candidates.Count == 0)
        {
            SpawnUpgradePickup(builder.RoomBounds.center + (Vector3)bossUpgradeOffset, fromBoss: false);
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
            var pick = go.GetComponent<WeaponUpgradePickup>() ?? go.AddComponent<WeaponUpgradePickup>();
            pick.useScriptable = true;
            pick.Assign(candidates[i]);

            currentArmoryPickups.Add(pick);
        }

        SubscribeArmory();
    }

    void ClearArmoryChoices(WeaponUpgradePickup chosen, bool consumeSelected)
    {
        foreach (var p in currentArmoryPickups)
        {
            if (!p) continue;
            if (chosen != null && p == chosen) continue;
            if (p.gameObject) Destroy(p.gameObject);
        }
        currentArmoryPickups.Clear();
        UnsubscribeArmory();
    }

    void SubscribeArmory()
    {
        if (armorySubscribed) return;
        WeaponUpgradePickup.AnyPicked += HandleAnyPickup;
        armorySubscribed = true;
    }
    void UnsubscribeArmory()
    {
        if (!armorySubscribed) return;
        WeaponUpgradePickup.AnyPicked -= HandleAnyPickup;
        armorySubscribed = false;
    }

    void HandleAnyPickup(WeaponUpgradePickup picked)
    {
        if (armoryChoiceResolved || picked == null) return;
        if (!currentArmoryPickups.Contains(picked)) return;

        armoryChoiceResolved = true;

        if (UpgradePoolManager.Instance && picked.upgradeSO)
            UpgradePoolManager.Instance.MarkUsed(picked.upgradeSO);

        ClearArmoryChoices(picked, true);
    }

    // ---------- Puertas ----------
    void SpawnDoorBarriers()
    {
        if (!doorBarrierPrefab) { Debug.LogWarning("[RoomRuntime] doorBarrierPrefab no asignado."); return; }

        spawnedDoors.Clear();
        foreach (var ds in builder.GetDoorSpawns())
        {
            GetDoorOrientation(builder.RoomBounds, ds.center, ds.size, out bool horizontal, out Vector2 inwardNormal);
            Vector2 finalSize = ComputeBarrierSize(ds.size, horizontal, doorThickness, doorPadding);
            Vector3 finalPos = ds.center + (Vector3)(inwardNormal * doorInset);

            var go = Instantiate(doorBarrierPrefab, finalPos, Quaternion.identity, transform);
            var box = go.GetComponent<BoxCollider2D>();
            if (box) box.size = finalSize;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr && sr.sprite != null)
            {
                Vector2 spriteSize = sr.sprite.bounds.size;
                go.transform.localScale = new Vector3(
                    finalSize.x / Mathf.Max(0.0001f, spriteSize.x),
                    finalSize.y / Mathf.Max(0.0001f, spriteSize.y),
                    1f
                );
            }
            go.gameObject.layer = LayerMask.NameToLayer("Walls");
            spawnedDoors.Add(go);
        }
    }

    static Vector2 ComputeBarrierSize(Vector2 holeSize, bool horizontal, float thickness, float padding)
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

    static void GetDoorOrientation(Bounds room, Vector3 center, Vector2 holeSize, out bool horizontal, out Vector2 inwardNormal)
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
            if (Vector2.Distance(playerPos, p) < minSpawnDistFromPlayer) continue;
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
            StartCoroutine(SpawnNextArenaWave());
            return;
        }

        foreach (var d in spawnedDoors) if (d) Destroy(d);
        spawnedDoors.Clear();

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
                SpawnUpgradePickup(builder.RoomBounds.center + (Vector3)bossUpgradeOffset, fromBoss: true);
                upgradeSpawned = true;
            }

            if (isFinalBossFloor)
                FloorFlowController.Instance?.WinGame();
            else if (elevatorExitPrefab)
                Instantiate(elevatorExitPrefab, builder.RoomBounds.center + (Vector3)elevatorOffset, Quaternion.identity);
        }
    }

    System.Collections.IEnumerator ArenaNextWaveDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, arenaInterval));
        yield return StartCoroutine(SpawnNextArenaWave());
    }

    // ---------- Mejoras ----------
    void SpawnUpgradePickup(Vector3 pos, bool fromBoss)
    {
        if (!upgradePickupPrefab) return;

        var go = Instantiate(upgradePickupPrefab, pos, Quaternion.identity);
        var pickup = go.GetComponent<WeaponUpgradePickup>() ?? go.AddComponent<WeaponUpgradePickup>();

        WeaponUpgradeSO so = null;
        if (UpgradePoolManager.Instance && UpgradePoolManager.Instance.DrawRandomUnique(out var drawn))
            so = drawn;

        pickup.useScriptable = true;
        pickup.Assign(so);

        if (so && UpgradePoolManager.Instance && fromBoss)
            UpgradePoolManager.Instance.MarkUsed(so);
    }

    // ---------- Utilidad Spawn ----------
    static Vector3 ClampInside(Bounds b, Vector3 p, float margin)
    {
        float x = Mathf.Clamp(p.x, b.min.x + margin, b.max.x - margin);
        float y = Mathf.Clamp(p.y, b.min.y, b.max.y - margin);
        return new Vector3(x, y, p.z);
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
}
