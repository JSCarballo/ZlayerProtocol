// Assets/Scripts/Combat/Health.cs
using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHP = 6f;
    [SerializeField] private float currentHP = -1f;

    [Header("Comportamiento")]
    public bool destroyOnDeath = true;
    public bool invincible = false;

    public event Action OnDeath;
    public event Action<float> OnDamaged; // ← NUEVO: notifica cuánto daño recibió

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP < 0f ? maxHP : currentHP;

    void Awake()
    {
        if (currentHP < 0f) currentHP = maxHP;
    }

    public void SetMax(float newMax, bool fill = true)
    {
        maxHP = Mathf.Max(1f, newMax);
        if (fill) currentHP = maxHP;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    public void Damage(float amount)
    {
        if (invincible || amount <= 0f) return;

        // Lanza evento ANTES de matar (para ver el número incluso si muere)
        OnDamaged?.Invoke(amount);

        currentHP = Mathf.Max(0f, CurrentHP - amount);
        if (currentHP <= 0f)
        {
            OnDeath?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}
