// Assets/Scripts/Items/WeaponUpgradePickup.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponUpgradePickup : MonoBehaviour
{
    public WeaponUpgrade upgrade;          // define qué mejora aplica
    public bool destroyOnPickup = true;

    [Header("Feedback (opcional)")]
    public AudioClip sfx;
    public GameObject vfxPrefab;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var stats = other.GetComponent<PlayerWeaponStats>();
        if (stats)
        {
            stats.Apply(upgrade);

            if (sfx) AudioSource.PlayClipAtPoint(sfx, transform.position);
            if (vfxPrefab) Instantiate(vfxPrefab, transform.position, Quaternion.identity);
            if (destroyOnPickup) Destroy(gameObject);
        }
    }
}
