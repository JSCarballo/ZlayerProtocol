// Assets/Scripts/Combat/Projectile.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public float damage = 1f;
    public bool piercing = false;
    public bool bouncing = false;
    public int maxBounces = 1;

    int bounces = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Walls"))
        {
            if (bouncing && bounces < maxBounces)
            {
                bounces++;
                // rebote simple invirtiendo velocidad
                var rb = GetComponent<Rigidbody2D>();
                if (rb) rb.linearVelocity = new Vector2(-rb.linearVelocity.x, -rb.linearVelocity.y);
                return;
            }
            Destroy(gameObject);
            return;
        }

        var hp = other.GetComponent<Health>();
        if (hp != null)
        {
            hp.Damage(damage);
            if (!piercing) Destroy(gameObject);
        }
    }
}
