using UnityEngine;

/// Bala 2D SIN autodestruirse por tiempo (a menos que lo habilites).
/// - Lanza con Launch(dir) o dale velocity con Rigidbody2D.
/// - Daño a Health y se destruye al impactar (configurable).
/// - Se destruye en paredes/tilemap (Walls) para no atravesar el mapa.
/// - NO usa lifetime por defecto (useLifetime = false).
[RequireComponent(typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 16f;
    [SerializeField] private bool faceVelocity = false; // si usas 1 prefab por dirección, déjalo en false

    [Header("Tiempo de vida (opcional)")]
    [SerializeField] private bool useLifetime = false;   // <--- OFF por defecto
    [SerializeField] private float lifetimeSeconds = 2.5f;

    [Header("Distancia máxima (opcional)")]
    [SerializeField] private bool useMaxDistance = false;
    [SerializeField] private float maxDistance = 20f;

    [Header("Daño")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask damageLayers; // 0 = cualquiera
    [SerializeField] private bool destroyOnHit = true;

    [Header("Paredes")]
    [SerializeField] private string wallsLayerName = "Walls"; // cambia si usas otra

    private Rigidbody2D rb;
    private Vector2 vel;
    private float t;
    private Vector3 spawnPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Garantizar visibilidad
        var sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr)
        {
            sr.enabled = true;
            var c = sr.color; if (c.a <= 0f) { c.a = 1f; sr.color = c; }
            var t0 = sr.transform;
            t0.position = new Vector3(t0.position.x, t0.position.y, 0f);
        }

        spawnPos = transform.position;
    }

    public void Launch(Vector2 dir)
    {
        vel = dir.normalized * speed;
        if (rb) rb.linearVelocity = vel;
        if (faceVelocity && vel.sqrMagnitude > 1e-4f)
            transform.right = vel;
    }

    void Update()
    {
        // Si no hay RB2D, mover manual
        if (!rb) transform.position += (Vector3)(vel * Time.deltaTime);

        // Lifetime (solo si está activado)
        if (useLifetime)
        {
            t += Time.deltaTime;
            if (t >= lifetimeSeconds) { Destroy(gameObject); return; }
        }

        // Distancia máxima (solo si está activada)
        if (useMaxDistance)
        {
            if ((transform.position - spawnPos).sqrMagnitude >= maxDistance * maxDistance)
            {
                Destroy(gameObject); return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other.gameObject);
    }

    void TryHit(GameObject hit)
    {
        if (!hit) return;

        // Paredes: TilemapCollider2D o layer Walls
        if (hit.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() ||
            (!string.IsNullOrEmpty(wallsLayerName) && LayerMask.LayerToName(hit.layer) == wallsLayerName))
        {
            Destroy(gameObject);
            return;
        }

        // Daño a Health
        var hp = hit.GetComponent<Health>();
        if (hp != null)
        {
            if (damageLayers.value == 0 || ((1 << hit.layer) & damageLayers.value) != 0)
            {
                hp.Damage(damage);
                if (destroyOnHit) Destroy(gameObject);
            }
        }
    }
}
