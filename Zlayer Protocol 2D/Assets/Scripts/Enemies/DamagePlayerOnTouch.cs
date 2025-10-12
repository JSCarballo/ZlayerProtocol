using System.Collections.Generic;
using UnityEngine;

/// Daño por contacto al Player, respetando invulnerabilidad (si el Player la usa).
/// Funciona con Trigger y/o Collision 2D. Incluye cooldown por objetivo.
/// Expone públicamente el daño para que el Player pueda leerlo si lo necesita.
[RequireComponent(typeof(Collider2D))]
public class DamagePlayerOnTouch : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int damage = 1;
    public int DamageAmount => damage; // <-- expuesto

    [Header("Filtros (opcionales)")]
    [SerializeField] private bool useLayerFilter = false;
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private bool useTagFilter = false;
    [SerializeField] private string requiredTag = "Player";

    [Header("Cooldown por objetivo")]
    [SerializeField, Tooltip("Tiempo mínimo entre golpes mientras permanece en contacto")]
    private float perTargetCooldown = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly Dictionary<Health, float> nextHitTime = new();

    // ---------- Triggers ----------
    void OnTriggerEnter2D(Collider2D other) { TryDamage(other.gameObject, "OnTriggerEnter2D"); }
    void OnTriggerStay2D(Collider2D other) { TryDamage(other.gameObject, "OnTriggerStay2D"); }

    // ---------- Collisions ----------
    void OnCollisionEnter2D(Collision2D col) { TryDamage(col.collider.gameObject, "OnCollisionEnter2D"); }
    void OnCollisionStay2D(Collision2D col) { TryDamage(col.collider.gameObject, "OnCollisionStay2D"); }

    void TryDamage(GameObject hitGO, string hook)
    {
        if (!hitGO) return;

        // Filtros opcionales
        if (useLayerFilter && (playerLayers.value & (1 << hitGO.layer)) == 0)
        {
            if (debugLogs) Debug.Log($"[DamagePlayerOnTouch] Ignorado por layer en {hook}", this);
            return;
        }
        if (useTagFilter && !hitGO.CompareTag(requiredTag))
        {
            if (debugLogs) Debug.Log($"[DamagePlayerOnTouch] Ignorado por tag en {hook}", this);
            return;
        }

        // Buscar Health en el Player (root/parent)
        var health = hitGO.GetComponentInParent<Health>();
        if (!health)
        {
            if (debugLogs) Debug.Log($"[DamagePlayerOnTouch] Sin Health en {hook} ({hitGO.name})", this);
            return;
        }

        // Invulnerabilidad del Player (si tiene controlador)
        var inv = hitGO.GetComponentInParent<PlayerInvulnerabilityController>();
        if (inv != null && inv.IsInvulnerable)
        {
            if (debugLogs) Debug.Log($"[DamagePlayerOnTouch] Invulnerable en {hook}", this);
            return;
        }

        // Cooldown por objetivo
        float now = Time.time;
        if (nextHitTime.TryGetValue(health, out float next) && now < next)
        {
            if (debugLogs) Debug.Log($"[DamagePlayerOnTouch] Cooldown ({next - now:0.00}s) en {hook}", this);
            return;
        }

        // Daño
        if (debugLogs) Debug.Log($"[DamagePlayerOnTouch] DAMAGING {health.name} en {hook} por {damage}", this);
        health.Damage(damage);

        // Invulnerabilidad/blink del Player si la usa
        if (inv != null) inv.StartInvulnerability();

        // Programar próximo golpe
        nextHitTime[health] = now + Mathf.Max(0.01f, perTargetCooldown);
    }
}
