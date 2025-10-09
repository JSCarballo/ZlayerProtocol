using System;
using System.Collections;
using UnityEngine;

/// Salud SOLO del Player con invulnerabilidad por ventana de tiempo.
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHP = 6;
    [SerializeField] private int currentHP = 6;

    [Header("Invulnerabilidad")]
    [SerializeField] private bool allowInvulnerability = true;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;
    public bool IsInvulnerable { get; private set; }

    public event Action<int> OnDamaged; // cantidad de daño aplicada
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

    public void Damage(int amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;
        if (allowInvulnerability && IsInvulnerable) return;

        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        OnDamaged?.Invoke(amount);

        if (currentHP <= 0)
        {
            OnDeath?.Invoke();
            // opcional: Destroy(gameObject);
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
    }

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
