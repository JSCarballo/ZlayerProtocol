using System;
using UnityEngine;

/// Salud de enemigos con destrucción al morir.
/// Llama OnDamaged(amount) antes de OnDeath().
[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private int currentHP = 3;

    [Header("Invulnerabilidad (opcional)")]
    [SerializeField] private bool allowInvulnerability = false;
    public bool IsInvulnerable { get; private set; }

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;

    public event Action<int> OnDamaged;
    public event Action OnDeath;

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
        OnDamaged?.Invoke(amount); // <-- primero

        if (currentHP <= 0)
        {
            OnDeath?.Invoke();
            Destroy(gameObject); // <-- el enemigo desaparece al morir
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
        StopAllCoroutines();
        StartCoroutine(InvulnRoutine(seconds));
    }

    System.Collections.IEnumerator InvulnRoutine(float seconds)
    {
        IsInvulnerable = true;
        yield return new WaitForSeconds(seconds);
        IsInvulnerable = false;
    }
}
