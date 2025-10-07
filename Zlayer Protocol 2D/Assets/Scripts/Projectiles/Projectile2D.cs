using UnityEngine;

/// Componente simple para balas 2D:
/// - Usa Rigidbody2D si existe; si no, mueve por Transform.
/// - Destruye al chocar con paredes/enemigos.
/// - Llama a Health.Damage si el blanco lo tiene.
[RequireComponent(typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 16f;
    public float lifetime = 2.5f;
    public bool faceVelocity = true;

    [Header("Da�o")]
    public int damage = 1;
    public LayerMask damageLayers; // d�jalo vac�o si quieres usar Health en cualquiera

    private Rigidbody2D rb;
    private Vector2 vel;
    private float t;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // para OnTriggerEnter2D
    }

    public void Launch(Vector2 dir)
    {
        vel = dir.normalized * speed;
        if (rb) rb.linearVelocity = vel;
        if (faceVelocity && vel.sqrMagnitude > 0.0001f)
            transform.right = vel; // rota sprite para mirar a la velocidad
    }

    void Update()
    {
        if (!rb) transform.position += (Vector3)(vel * Time.deltaTime);
        t += Time.deltaTime;
        if (t >= lifetime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other.gameObject);
    void OnCollisionEnter2D(Collision2D col) => TryHit(col.collider.gameObject);

    void TryHit(GameObject hit)
    {
        if (!hit) return;

        // Paredes (TilemapCollider2D o layer "Walls")
        if (hit.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() ||
            LayerMask.LayerToName(hit.layer) == "Walls")
        {
            Destroy(gameObject);
            return;
        }

        var hp = hit.GetComponent<Health>();
        if (hp != null)
        {
            if (damageLayers.value == 0 || ((1 << hit.layer) & damageLayers.value) != 0)
            {
                hp.Damage(damage);
                Destroy(gameObject);
            }
        }
    }
}
