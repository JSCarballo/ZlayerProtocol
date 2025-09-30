using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProjectileHit : MonoBehaviour
{
    public float damage = 1f;
    public bool destroyOnHit = true;
    public LayerMask hitLayers; // asigna Enemy

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

        var hp = other.GetComponent<Health>();
        if (hp != null) hp.Damage(damage);

        if (destroyOnHit) Destroy(gameObject);
    }
}
