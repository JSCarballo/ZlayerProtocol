// Assets/Scripts/Player/FourWayShooter.cs
using UnityEngine;

public class FourWayShooter : MonoBehaviour
{
    [Header("Base")]
    public GameObject bulletPrefab;
    public float baseDamage = 1f;
    public float baseCooldown = 0.25f;     // seg entre disparos (1/4 = 4/s)
    public float baseBulletSpeed = 8f;

    [Header("Inputs")]
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode downKey = KeyCode.DownArrow;
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;

    PlayerWeaponStats stats;
    float cooldownTimer;

    void Awake()
    {
        stats = GetComponent<PlayerWeaponStats>();
        if (!stats) stats = gameObject.AddComponent<PlayerWeaponStats>(); // garantiza que exista
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;
        Vector2 dir = Vector2.zero;

        if (Input.GetKey(upKey)) dir = Vector2.up;
        if (Input.GetKey(downKey)) dir = Vector2.down;
        if (Input.GetKey(leftKey)) dir = Vector2.left;
        if (Input.GetKey(rightKey)) dir = Vector2.right;

        if (dir != Vector2.zero && cooldownTimer <= 0f)
        {
            Shoot(dir.normalized);
            float cd = baseCooldown / Mathf.Max(0.01f, stats.fireRateMult);
            cooldownTimer = cd;
        }
    }

    void Shoot(Vector2 dir)
    {
        int total = 1 + Mathf.Max(0, stats.extraProjectiles);
        float spread = stats.spreadDegrees;

        if (spread <= 0f || total == 1)
        {
            SpawnBullet(dir, 0f);
        }
        else
        {
            // centrado en dir, abanico
            float step = total > 1 ? spread / (total - 1) : 0f;
            float start = -spread * 0.5f;
            for (int i = 0; i < total; i++)
            {
                float ang = start + i * step;
                SpawnBullet(dir, ang);
            }
        }
    }

    void SpawnBullet(Vector2 dir, float deltaAngleDeg)
    {
        float dmg = baseDamage * stats.damageMult;
        float spd = baseBulletSpeed * stats.projectileSpeedMult;

        Vector2 d = Quaternion.Euler(0, 0, deltaAngleDeg) * dir;
        var go = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = d * spd;

        var b = go.GetComponent<Projectile>();
        if (b)
        {
            b.damage = dmg;
            b.piercing = stats.piercing;
            b.bouncing = stats.bouncing;
        }
    }
}
