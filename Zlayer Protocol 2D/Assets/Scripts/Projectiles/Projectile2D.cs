using UnityEngine;
using System;

/// Proyectil 2D que recibe stats del arma y aplica daño al impactar.
/// - Llama a SetStats(...) o ConfigureFromStats(PlayerWeaponStats) antes de Launch.
/// - Launch(dir [, speedOverride]) mueve la bala.
/// - Rebota si 'bouncing' con 'maxBounces' > 0; si no, se destruye en paredes.
/// - Daño a Health con Damage(float) o Damage(int) si existen.
/// - "Walls" se detecta por nombre de capa o por TilemapCollider2D.
[RequireComponent(typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private bool faceVelocity = false;

    [Header("Vida (opcional)")]
    [SerializeField] private bool useLifetime = false;
    [SerializeField] private float lifetimeSeconds = 3f;

    [Header("Paredes")]
    [SerializeField] private string wallsLayerName = "Walls";

    [Header("Daño / Comportamiento")]
    [Tooltip("Daño base si no se configura por SetStats/ConfigureFromStats.")]
    public float damage = 1f;
    public bool piercing = false;
    public bool bouncing = false;
    public int maxBounces = 0;

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

        // Garantizar visibilidad
        var sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            sr.enabled = true;
            var c = sr.color; if (c.a <= 0f) { c.a = 1f; sr.color = c; }
            var t0 = sr.transform; t0.position = new Vector3(t0.position.x, t0.position.y, 0f);
        }
    }

    // ====== API de stats ======
    public void SetStats(float dmg, bool prc, bool bnc, int maxB)
    {
        damage = dmg;
        piercing = prc;
        bouncing = bnc;
        maxBounces = Mathf.Max(0, maxB);
        bouncesLeft = maxBounces;
    }

    /// Compatibilidad con flujos previos.
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

        // Paredes o tilemap
        bool isWallLayer = !string.IsNullOrEmpty(wallsLayerName) && LayerMask.LayerToName(hit.layer) == wallsLayerName;
        bool isTilemap = hit.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();

        if (isWallLayer || isTilemap)
        {
            if (bouncing && bouncesLeft > 0)
            {
                bouncesLeft--;
                vel = -vel;
                if (rb) rb.linearVelocity = vel;
                return;
            }
            Destroy(gameObject);
            return;
        }

        // Daño a Health (objeto o su padre)
        var hp = hit.GetComponent<Health>() ?? hit.GetComponentInParent<Health>();
        if (hp)
        {
            DealDamage(hp, damage);
            if (!piercing) Destroy(gameObject);
        }
    }

    void DealDamage(Health hp, float dmg)
    {
        if (!hp) return;
        var t = hp.GetType();

        // Damage(float)
        var mFloat = t.GetMethod("Damage", new Type[] { typeof(float) });
        if (mFloat != null) { mFloat.Invoke(hp, new object[] { dmg }); return; }

        // Damage(int)
        var mInt = t.GetMethod("Damage", new Type[] { typeof(int) });
        if (mInt != null) { mInt.Invoke(hp, new object[] { Mathf.RoundToInt(dmg) }); return; }

        // Si no existe Damage, no hacemos nada.
    }
}
