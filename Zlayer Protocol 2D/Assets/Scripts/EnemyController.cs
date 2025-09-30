using UnityEngine;

/// <summary>
/// Basic enemy AI for a zombie. Moves toward the player and takes damage
/// from projectiles. Can drop power‑ups on death.
/// </summary>
public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float health = 3f;
    public float damage = 1f;
    public float dropChance = 0.2f; // chance to drop a power‑up on death
    public GameObject[] powerUpPrefabs;

    private Transform target;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (target == null) return;
        // Move towards the player
        Vector2 dir = (target.position - transform.position).normalized;
        transform.position += (Vector3)dir * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Applies damage to the enemy. Destroys the enemy when health reaches zero.
    /// Randomly spawns a power‑up upon death.
    /// </summary>
    /// <param name="amount">Damage inflicted.</param>
    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Drop a power‑up with probability dropChance
        if (powerUpPrefabs != null && powerUpPrefabs.Length > 0 && Random.value < dropChance)
        {
            int index = Random.Range(0, powerUpPrefabs.Length);
            Instantiate(powerUpPrefabs[index], transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Damage the player on contact
        PlayerController player = collision.collider.GetComponent<PlayerController>();
        if (player != null)
        {
            // TODO: Implement player health and damage
        }
    }
}