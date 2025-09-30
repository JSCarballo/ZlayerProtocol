using UnityEngine;

/// <summary>
/// Simple projectile logic for player bullets. Moves forward at a constant
/// speed and interacts with enemies. Supports piercing and explosive
/// behaviours. Destroyed when colliding with walls or after a timeout.
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public bool explosive = false;
    public bool piercing = false;
    public float lifetime = 5f;

    private float lifeTimer;

    private void OnEnable()
    {
        lifeTimer = lifetime;
    }

    private void Update()
    {
        // Move forward in the local up direction
        transform.Translate(Vector3.up * speed * Time.deltaTime);
        // Destroy the projectile after its lifetime expires
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check collision with enemy
        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            if (explosive)
            {
                // Optional: implement area damage (not included for brevity)
            }
            if (!piercing)
            {
                Destroy(gameObject);
            }
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            // Hit a wall, destroy the projectile
            Destroy(gameObject);
        }
    }
}