using System.Collections.Generic;
using UnityEngine;

/// Hurtbox del Player: aplica da�o al recibir contacto con enemigos (trigger o colisi�n),
/// respetando la invulnerabilidad del Player. Lee el da�o desde DamagePlayerOnTouch si est�
/// presente en el enemigo; si no, usa un da�o por defecto.
[RequireComponent(typeof(Collider2D))]
public class PlayerHurtbox : MonoBehaviour
{
    [Header("Da�o por defecto si el enemigo no expone DamagePlayerOnTouch")]
    [SerializeField] private int defaultContactDamage = 1;

    [Header("Detecci�n de enemigos")]
    [SerializeField] private bool useLayerFilter = false;
    [SerializeField] private LayerMask enemyLayers; // si no lo usas, d�jalo en 0
    [SerializeField] private bool useTagFilter = false;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Cooldown por atacante")]
    [SerializeField, Tooltip("Tiempo m�nimo entre golpes del mismo enemigo")]
    private float perAttackerCooldown = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Health playerHealth;
    private PlayerInvulnerabilityController inv;
    private readonly Dictionary<int, float> nextAllowedFromAttacker = new();

    void Awake()
    {
        playerHealth = GetComponentInParent<Health>() ?? GetComponent<Health>();
        inv = GetComponentInParent<PlayerInvulnerabilityController>() ?? GetComponent<PlayerInvulnerabilityController>();

        if (!playerHealth)
            Debug.LogError("[PlayerHurtbox] No se encontr� Health en el Player.");
        if (!inv)
            Debug.LogWarning("[PlayerHurtbox] No se encontr� PlayerInvulnerabilityController; no habr� invuln/blink.");
    }

    // ---------- Triggers ----------
    void OnTriggerEnter2D(Collider2D other) { TryTakeDamage(other.gameObject, "OnTriggerEnter2D"); }
    void OnTriggerStay2D(Collider2D other) { TryTakeDamage(other.gameObject, "OnTriggerStay2D"); }

    // ---------- Collisions ----------
    void OnCollisionEnter2D(Collision2D col) { TryTakeDamage(col.collider.gameObject, "OnCollisionEnter2D"); }
    void OnCollisionStay2D(Collision2D col) { TryTakeDamage(col.collider.gameObject, "OnCollisionStay2D"); }

    void TryTakeDamage(GameObject other, string hook)
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (!playerHealth) return;

        // Filtros opcionales para identificar "enemigos"
        if (useLayerFilter && (enemyLayers.value & (1 << other.layer)) == 0) return;
        if (useTagFilter && !other.CompareTag(enemyTag)) return;

        // Si el Player est� invulnerable, ignorar
        if (inv != null && inv.IsInvulnerable) return;

        // Identificador del atacante (usamos root del otro)
        Transform root = other.transform.root;
        int attackerId = root.GetInstanceID();

        // Cooldown por atacante
        float now = Time.time;
        if (nextAllowedFromAttacker.TryGetValue(attackerId, out float next) && now < next)
        {
            if (debugLogs) Debug.Log($"[PlayerHurtbox] Cooldown desde {root.name} ({next - now:0.00}s) en {hook}", this);
            return;
        }

        // Determinar da�o: si el enemigo tiene DamagePlayerOnTouch, usar su cantidad
        int dmg = defaultContactDamage;
        var src = root.GetComponentInChildren<DamagePlayerOnTouch>();
        if (src != null) dmg = Mathf.Max(1, src.DamageAmount);

        if (debugLogs) Debug.Log($"[PlayerHurtbox] Recibiendo da�o {dmg} desde {root.name} en {hook}", this);

        // Aplicar da�o
        playerHealth.Damage(dmg);

        // Activar invulnerabilidad/blink en el Player (si existe controlador)
        if (inv != null) inv.StartInvulnerability();

        // Programar pr�ximo golpe desde este atacante
        nextAllowedFromAttacker[attackerId] = now + Mathf.Max(0.01f, perAttackerCooldown);
    }
}
