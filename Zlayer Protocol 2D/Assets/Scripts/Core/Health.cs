using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public float maxHP = 6f;
    public float currentHP;
    public bool destroyOnDeath = true;

    public event Action<float, float> OnHealthChanged; // current/max
    public event Action OnDeath;

    void Awake() => currentHP = maxHP;

    public void Damage(float amount)
    {
        if (currentHP <= 0) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        if (currentHP <= 0) Die();
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        OnDeath?.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }
}
