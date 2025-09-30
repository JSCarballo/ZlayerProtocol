using UnityEngine;

/// Bala simple: vida limitada y destrucción al tocar "Walls" o tras tiempo.
[RequireComponent(typeof(Collider2D))]
public class SimpleProjectile : MonoBehaviour
{
    public float lifeTime = 2f;

    void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0) Destroy(gameObject);
    }

    // Ajusta según tu colisión. Si tus muros están en Layer "Walls", puedes:
    void OnTriggerEnter2D(Collider2D other)
    {
        // Si luego quieres daño, aquí detectarías "Enemy" y aplicarías Health.Damage()
        if (other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Destroy(gameObject);
        }
    }
}
