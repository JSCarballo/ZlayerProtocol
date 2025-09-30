using UnityEngine;
using System.Collections;

/// <summary>
/// Represents a basic firearm. Handles firing bullets at a specified rate and
/// applies modifier flags such as explosive or piercing bullets. Power‑ups
/// adjust public fields like damage, fireRate or boolean effects.
/// </summary>
public class Gun : MonoBehaviour
{
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.3f; // seconds between shots
    public float damage = 1f;
    public float bulletSpeed = 10f;

    [Header("Modifiers")]
    public bool explosiveBullets = false;
    public bool piercingBullets = false;

    private float fireCooldown = 0f;

    private void Update()
    {
        // Countdown the cooldown timer
        fireCooldown -= Time.deltaTime;
    }

    /// <summary>
    /// Spawns a bullet if the gun is off cooldown.
    /// </summary>
    public void Fire()
    {
        if (fireCooldown > 0f) return;
        fireCooldown = fireRate;
        // Instantiate bullet and configure its parameters
        GameObject projObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Projectile proj = projObj.GetComponent<Projectile>();
        proj.speed = bulletSpeed;
        proj.damage = damage;
        proj.explosive = explosiveBullets;
        proj.piercing = piercingBullets;
    }
}