using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamagePlayerOnTouch : MonoBehaviour
{
    public float touchDamage = 1f;
    public float cooldown = 0.5f;
    float cdTimer;

    void Update() { if (cdTimer > 0) cdTimer -= Time.deltaTime; }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }
    void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider2D col)
    {
        if (cdTimer > 0) return;
        if (!col.CompareTag("Player")) return;

        var hp = col.GetComponent<Health>();
        if (hp != null)
        {
            hp.Damage(touchDamage);
            cdTimer = cooldown;
        }
    }
}
