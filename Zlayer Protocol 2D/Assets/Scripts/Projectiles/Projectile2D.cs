using UnityEngine;
using System;

/// Proyectil 2D que recibe stats del arma y aplica daño al impactar.
/// Además, expone un snapshot estático para HUD (daño, speed, flags, bounces, rof).
[RequireComponent(typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    // ---------- HUD Snapshot ----------
    public struct HUDSnapshot
    {
        public float damage;
        public float speed;
        public float rof;       // disparos/seg. Reportado por el shooter o por pickup.
        public bool piercing;
        public bool bouncing;
        public int maxBounces;
    }

    public static event System.Action<HUDSnapshot> OnHUDSnapshotChanged;

    private static HUDSnapshot lastHUD;
    private static bool hasHUD = false;

    public static bool HasHUD => hasHUD;
    public static HUDSnapshot GetLastHUD() => lastHUD;

    /// Llamado por el shooter para reportar el ROF actual (si cambia).
    public static void ReportFireRate(float rof)
    {
        lastHUD.rof = Mathf.Max(0.0001f, rof);
        hasHUD = true;
        OnHUDSnapshotChanged?.Invoke(lastHUD);
    }

    /// Llamado por pickups o bridges para actualizar el HUD en caliente (sin disparar).
    public static void ReportInstant(float damage, float bulletSpeed, bool piercing, bool bouncing, int maxBounces, float rof = -1f)
    {
        lastHUD.damage = damage;
        lastHUD.speed = bulletSpeed;
        lastHUD.piercing = piercing;
        lastHUD.bouncing = bouncing;
        lastHUD.maxBounces = Mathf.Max(0, maxBounces);
        if (rof > 0f) lastHUD.rof = rof;

        hasHUD = true;
        OnHUDSnapshotChanged?.Invoke(lastHUD);
    }

    /// Azúcar: actualizar snapshot desde PlayerWeaponStats (si lo usas internamente).
    public static void ReportFromPWS(PlayerWeaponStats ws)
    {
        if (!ws) return;
        ReportInstant(ws.damage, ws.bulletSpeed, ws.piercing, ws.bouncing, ws.maxBounces, ws.fireRate);
    }

    // ---------- Config propia del proyectil ----------
    [Header("Movimiento")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private bool faceVelocity = false;

    [Header("Vida (opcional)")]
    [SerializeField] private bool useLifetime = false;
    [SerializeField] private float lifetimeSeconds = 3f;

    [Header("Paredes")]
    [SerializeField] private string wallsLayerName = "Walls";

    [Header("Daño / Comportamiento")]
    public float damage = 1f;
    public bool piercing = false;
    public bool bouncing = false;
    public int maxBounces = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Rigidbody2D rb;
    private Vector2 vel;
    private float tLife;
    private int bouncesLeft;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        bouncesLeft = maxBounces;

        var sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            sr.enabled = true;
            var c = sr.color; if (c.a <= 0f) { c.a = 1f; sr.color = c; }
            var t0 = sr.transform; t0.position = new Vector3(t0.position.x, t0.position.y, 0f);
        }
    }

    // ====== API de stats que usa el Shooter ======
    public void SetStats(float dmg, bool prc, bool bnc, int maxB)
    {
        damage = dmg;
        piercing = prc;
        bouncing = bnc;
        maxBounces = Mathf.Max(0, maxB);
        bouncesLeft = maxBounces;

        // Actualiza snapshot parcial
        lastHUD.damage = damage;
        lastHUD.piercing = piercing;
        lastHUD.bouncing = bouncing;
        lastHUD.maxBounces = maxBounces;
        hasHUD = true;
        OnHUDSnapshotChanged?.Invoke(lastHUD);

        if (debugLog)
            Debug.Log($"[Projectile] SetStats: DMG={damage}, PRC={piercing}, BNC={bouncing}, MB={maxBounces}");
    }

    public void ConfigureFromStats(PlayerWeaponStats stats)
    {
        if (!stats) return;
        SetStats(stats.damage, stats.piercing, stats.bouncing, stats.maxBounces);
    }

    // ====== Movimiento ======
    public void Launch(Vector2 dir) => Launch(dir, -1f);

    public void Launch(Vector2 dir, float speedOverride)
    {
        if (speedOverride > 0f) speed = speedOverride;
        vel = dir.normalized * speed;
        if (rb) rb.linearVelocity = vel;
        if (faceVelocity && vel.sqrMagnitude > 1e-4f)
            transform.right = vel;

        // Completar snapshot con speed efectiva
        lastHUD.speed = speed;
        hasHUD = true;
        OnHUDSnapshotChanged?.Invoke(lastHUD);

        if (debugLog)
            Debug.Log($"[Projectile] Launch dir={dir}, speed={speed}");
    }

    void Update()
    {
        if (!rb) transform.position += (Vector3)(vel * Time.deltaTime);

        if (useLifetime)
        {
            tLife += Time.deltaTime;
            if (tLife >= lifetimeSeconds) { Destroy(gameObject); return; }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hit = other.gameObject;
        if (!hit) return;

        bool isWallLayer = !string.IsNullOrEmpty(wallsLayerName) && LayerMask.LayerToName(hit.layer) == wallsLayerName;
        bool isTilemap = hit.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();

        if (isWallLayer || isTilemap)
        {
            if (bouncing && bouncesLeft > 0)
            {
                bouncesLeft--;
                vel = -vel;
                if (rb) rb.linearVelocity = vel;
                if (debugLog) Debug.Log($"[Projectile] Bounce, left={bouncesLeft}");
                return;
            }
            if (debugLog) Debug.Log("[Projectile] Hit wall → destroy");
            Destroy(gameObject);
            return;
        }

        var hp = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
        if (hp)
        {
            DealDamage(hp, damage);
            if (!piercing) Destroy(gameObject);
        }
    }

    void DealDamage(Health hp, float dmg)
    {
        var t = hp.GetType();
        var mFloat = t.GetMethod("Damage", new System.Type[] { typeof(float) });
        if (mFloat != null) { mFloat.Invoke(hp, new object[] { dmg }); if (debugLog) Debug.Log($"[Projectile] Damage(float)={dmg} → {hp.name}"); return; }
        var mInt = t.GetMethod("Damage", new System.Type[] { typeof(int) });
        if (mInt != null) { int di = Mathf.RoundToInt(dmg); mInt.Invoke(hp, new object[] { di }); if (debugLog) Debug.Log($"[Projectile] Damage(int)={di} → {hp.name}"); return; }
    }
}
