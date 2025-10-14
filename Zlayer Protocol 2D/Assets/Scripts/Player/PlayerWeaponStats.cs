using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PlayerWeaponStats : MonoBehaviour
{
    [Header("Base Stats (arma)")]
    [Min(0f)] public float damage = 1f;         // daño por bala
    [Min(0.05f)] public float fireRate = 3f;    // disparos/seg
    [Min(0.1f)] public float bulletSpeed = 12f; // unidades/seg

    [Header("Comportamientos")]
    public bool piercing = false;
    public bool bouncing = false;
    [Min(0)] public int maxBounces = 0;

    /// Se dispara cuando cambian stats (UI, Shooter, etc).
    public event Action OnStatsChanged;

    public void Apply(WeaponUpgradeSO so)
    {
        if (!so) return;

        // Aditivos
        damage = Mathf.Max(0f, damage + so.addDamage);
        fireRate = Mathf.Max(0.05f, fireRate + so.addFireRate);
        bulletSpeed = Mathf.Max(0.1f, bulletSpeed + so.addBulletSpeed);
        maxBounces = Mathf.Max(0, maxBounces + so.addMaxBounces);

        // Multiplicadores
        damage = Mathf.Max(0f, damage * Mathf.Max(0f, so.mulDamage));
        fireRate = Mathf.Max(0.05f, fireRate * Mathf.Max(0f, so.mulFireRate));
        bulletSpeed = Mathf.Max(0.1f, bulletSpeed * Mathf.Max(0f, so.mulBulletSpeed));

        // Flags
        piercing = piercing || so.enablePiercing;
        bouncing = bouncing || so.enableBouncing;

        NotifyChanged();
    }

    public void ApplyDirectDelta(
        float dDamage = 0f, float dFireRate = 0f, float dBulletSpeed = 0f,
        bool? setPiercing = null, bool? setBouncing = null, int dMaxBounces = 0)
    {
        damage = Mathf.Max(0f, damage + dDamage);
        fireRate = Mathf.Max(0.05f, fireRate + dFireRate);
        bulletSpeed = Mathf.Max(0.1f, bulletSpeed + dBulletSpeed);
        maxBounces = Mathf.Max(0, maxBounces + dMaxBounces);

        if (setPiercing.HasValue) piercing = setPiercing.Value;
        if (setBouncing.HasValue) bouncing = setBouncing.Value;

        NotifyChanged();
    }

    public void NotifyChanged() => OnStatsChanged?.Invoke();
}
