using UnityEngine;

/// Proyectil genérico (único) para todo el juego.
/// - fromPlayer = true  => daña EnemyHealth.
/// - fromPlayer = false => daña PlayerHealth.
/// - Soporta Trigger y Collision.
/// - Filtro por damageLayers opcional (0 = ignora máscara y usa tipo de objetivo).
/// - Destruye al chocar con paredes (TilemapCollider2D o layer "Walls").
[RequireComponent(typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    [Header("Propiedad")]
    [SerializeField] private bool fromPlayer = true;

    [Header("Movimiento")]
    [SerializeField] private float speed = 16f;
    [SerializeField] private bool faceVelocity = false;

    [Header("Vida del proyectil (opcionales)")]
    [SerializeField] private bool useLifetime = false;
    [SerializeField] private float lifetimeSeconds = 2.5f;
    [SerializeField] private bool useMaxDistance = false;
    [SerializeField] private float maxDistance = 20f;

    [Header("Daño")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask damageLayers; // 0 => no filtra por layer (mejor para evitar confusiones)
    [SerializeField] private bool destroyOnHit = true;

    [Header("Paredes")]
    [SerializeField] private string wallsLayerName = "Walls";

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
            var p = sr.transform.position; sr.transform.position = new Vector3(p.x, p.y, 0f);
        }

        spawnPos = transform.position;
    }

    public void Launch(Vector2 dir)
    {
        vel = dir.normalized * speed;
        if (rb) rb.linearVelocity = vel;
        if (faceVelocity && vel.sqrMagnitude > 1e-4f) transform.right = vel;
    }

    void Update()
    {
        if (!rb) transform.position += (Vector3)(vel * Time.deltaTime);

        if (useLifetime)
        {
            t += Time.deltaTime;
            if (t >= lifetimeSeconds) { Destroy(gameObject); return; }
        }

        if (useMaxDistance)
        {
            if ((transform.position - spawnPos).sqrMagnitude >= maxDistance * maxDistance)
            {
                Destroy(gameObject); return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other.gameObject);
    void OnCollisionEnter2D(Collision2D col) => TryHit(col.collider.gameObject);

    void TryHit(GameObject hit)
    {
        if (!hit) return;

        // Paredes
        if (hit.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() ||
            (!string.IsNullOrEmpty(wallsLayerName) && LayerMask.LayerToName(hit.layer) == wallsLayerName))
        {
            Destroy(gameObject);
            return;
        }

        // Si damageLayers != 0, filtra; si es 0, ignora máscara y golpea por tipo
        if (damageLayers.value != 0 && ((1 << hit.layer) & damageLayers.value) == 0)
        {
            return;
        }

        if (fromPlayer)
        {
            var eh = hit.GetComponent<EnemyHealth>() ?? hit.GetComponentInParent<EnemyHealth>();
            if (eh != null)
            {
                eh.Damage(Mathf.Max(1, damage));
                if (destroyOnHit) Destroy(gameObject);
            }
        }
        else
        {
            var ph = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.Damage(Mathf.Max(1, damage));
                if (destroyOnHit) Destroy(gameObject);
            }
        }
    }
}
