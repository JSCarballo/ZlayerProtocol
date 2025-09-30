using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BulletDespawnOnWalls : MonoBehaviour
{
    public string wallsLayerName = "Walls";
    int wallsLayer;
    Rigidbody2D rb;

    void Awake()
    {
        wallsLayer = LayerMask.NameToLayer(wallsLayerName);
        rb = GetComponent<Rigidbody2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == wallsLayer)
            Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.layer == wallsLayer)
            Destroy(gameObject);
    }

    void FixedUpdate()
    {
        // anti-t�nel opcional
        if (rb == null) return;
        float dist = rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        if (dist <= 0f) return;
        int mask = 1 << wallsLayer;
        var hit = Physics2D.Raycast(rb.position, rb.linearVelocity.normalized, dist, mask);
        if (hit.collider) Destroy(gameObject);
    }
}
