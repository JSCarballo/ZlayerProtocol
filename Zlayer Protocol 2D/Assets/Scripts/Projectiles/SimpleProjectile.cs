using UnityEngine;

/// Bala simple: vida limitada y destrucci�n al tocar "Walls" o tras tiempo.
[RequireComponent(typeof(Collider2D))]
public class SimpleProjectile : MonoBehaviour
{
    public float lifeTime = 2f;

    void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0) Destroy(gameObject);
    }

    // Ajusta seg�n tu colisi�n. Si tus muros est�n en Layer "Walls", puedes:
    void OnTriggerEnter2D(Collider2D other)
    {
        // Si luego quieres da�o, aqu� detectar�as "Enemy" y aplicar�as Health.Damage()
        if (other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Destroy(gameObject);
        }
    }
}
