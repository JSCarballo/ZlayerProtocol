// Assets/Scripts/Combat/Projectile.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Stats")]
    public float damage = 1f;
    public bool piercing = false;
    public bool bouncing = false;
    public int maxBounces = 1;

    [Header("Debug")]
    public bool debugHits = false;

    int bounces = 0;
    bool consumed = false; // evita m�ltiples da�os en el mismo frame
    readonly HashSet<int> alreadyHit = new(); // Health instanceIDs ya da�ados

    void Awake()
    {
        // Recomendado para proyectiles r�pidos
        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // trabajamos por OnTriggerEnter2D
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed && !piercing) return;

        // 1) Paredes
        if (other.CompareTag("Walls"))
        {
            if (bouncing && bounces < maxBounces)
            {
                Bounce();
                return;
            }
            ConsumeAndDestroy();
            return;
        }

        // 2) Da�o a enemigos/boss (buscar Health en el padre)
        var hp = other.GetComponentInParent<Health>();
        if (hp != null)
        {
            int id = hp.GetInstanceID();
            if (alreadyHit.Contains(id))
            {
                if (debugHits) Debug.Log($"[Projectile] Ignorado hit repetido en {hp.name}");
                return; // ya golpeado este objetivo
            }

            alreadyHit.Add(id);
            hp.Damage(damage);
            if (debugHits) Debug.Log($"[Projectile] Hit {hp.name} por {damage}");

            if (!piercing)
            {
                ConsumeAndDestroy();
            }
            return;
        }
    }

    void Bounce()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = -rb.linearVelocity; // rebote simple
            bounces++;
        }
    }

    void ConsumeAndDestroy()
    {
        if (consumed) return;
        consumed = true;
        Destroy(gameObject);
    }
}
