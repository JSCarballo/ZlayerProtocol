using System;
using System.Collections;
using UnityEngine;

/// Health genérico con compuerta de invulnerabilidad opcional.
/// - Si IsInvulnerable es true y allowInvulnerability es true, ignora Damage().
/// - Expone eventos OnDamaged y OnDeath.
/// - Métodos para activar/desactivar invulnerabilidad con duración.
public class Health : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHP = 6;
    [SerializeField] private int currentHP = 6;

    [Header("Invulnerabilidad")]
    [Tooltip("Si está activo, cuando IsInvulnerable sea true, Damage() se ignorará.")]
    [SerializeField] private bool allowInvulnerability = true;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;
    public bool IsInvulnerable { get; private set; }

    public event Action<int> OnDamaged; // amount (ya aplicado)
    public event Action OnDeath;

    Coroutine invulnCR;

    void Awake()
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    public void SetMaxHP(int value, bool refill = true)
    {
        maxHP = Mathf.Max(1, value);
        if (refill) currentHP = maxHP;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }

    /// Aplica daño si no está invulnerable (o si no se permite invuln).
    public void Damage(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        if (allowInvulnerability && IsInvulnerable)
            return; // ignora daño durante ventana de invulnerabilidad

        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        OnDamaged?.Invoke(amount);

        if (currentHP <= 0)
        {
            OnDeath?.Invoke();
            // Aquí puedes hacer Destroy(gameObject) si tu flujo lo requiere
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
    }

    /// Comienza una ventana de invulnerabilidad (si allowInvulnerability = true).
    public void StartInvulnerability(float seconds)
    {
        if (!allowInvulnerability || seconds <= 0f) return;
        if (invulnCR != null) StopCoroutine(invulnCR);
        invulnCR = StartCoroutine(InvulnRoutine(seconds));
    }

    public void SetInvulnerable(bool state)
    {
        if (!allowInvulnerability) return;
        IsInvulnerable = state;
        if (!state && invulnCR != null) { StopCoroutine(invulnCR); invulnCR = null; }
    }

    IEnumerator InvulnRoutine(float seconds)
    {
        IsInvulnerable = true;
        yield return new WaitForSeconds(seconds);
        IsInvulnerable = false;
        invulnCR = null;
    }
}
