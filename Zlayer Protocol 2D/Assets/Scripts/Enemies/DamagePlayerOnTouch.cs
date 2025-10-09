using UnityEngine;

/// Daño de contacto SOLO al Player (soporta Trigger y Collision).
[RequireComponent(typeof(Collider2D))]
public class DamagePlayerOnTouch : MonoBehaviour
{
    public enum FilterMode { ByTag, ByLayerMask, AnyPlayerHealth }

    [Header("Filtro")]
    public FilterMode filterMode = FilterMode.ByTag;
    public string playerTag = "Player";
    public LayerMask playerLayers;

    [Header("Daño")]
    [Tooltip("Se redondea hacia arriba a entero.")]
    public float contactDamage = 1f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other) => TryDamage(other.gameObject);
    void OnCollisionEnter2D(Collision2D col) => TryDamage(col.collider.gameObject);

    void TryDamage(GameObject other)
    {
        if (!IsValid(other)) return;

        var hp = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (!hp) return;

        hp.Damage(Mathf.Max(1, Mathf.CeilToInt(contactDamage)));
    }

    bool IsValid(GameObject other)
    {
        switch (filterMode)
        {
            case FilterMode.ByTag: return other.CompareTag(playerTag);
            case FilterMode.ByLayerMask: return playerLayers.value == 0 || (playerLayers.value & (1 << other.layer)) != 0;
            case FilterMode.AnyPlayerHealth: return other.GetComponent<PlayerHealth>() || other.GetComponentInParent<PlayerHealth>();
        }
        return false;
    }
}
