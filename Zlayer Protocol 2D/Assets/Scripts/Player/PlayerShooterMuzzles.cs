using UnityEngine;

/// Shooter 4 direcciones que usa PlayerWeaponStats y dispara desde muzzles.
/// - Cooldown = 1 / fireRate
/// - Soporta hold o solo tap (configurable)
/// - Pixel-snap del spawn y MuzzleFlash opcional
/// - Copia layer/order del Player si no indicas uno para las balas
[RequireComponent(typeof(Collider2D))]
public class PlayerShooterMuzzles : MonoBehaviour
{
    [Header("Stats (arma)")]
    [SerializeField] private PlayerWeaponStats weaponStats;

    [Header("Prefabs (fallback + por dirección)")]
    [SerializeField] private GameObject bulletPrefab;       // fallback si faltan los de abajo
    [SerializeField] private GameObject bulletPrefabRight;
    [SerializeField] private GameObject bulletPrefabLeft;
    [SerializeField] private GameObject bulletPrefabUp;
    [SerializeField] private GameObject bulletPrefabDown;

    [Header("Muzzles (asigna los 4)")]
    [SerializeField] private Transform muzzleRight;
    [SerializeField] private Transform muzzleLeft;
    [SerializeField] private Transform muzzleUp;
    [SerializeField] private Transform muzzleDown;

    [Header("Muzzle Flash (opcional)")]
    [SerializeField] private MuzzleFlash2D muzzleFlash;

    [Header("Spawn")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private bool pixelSnapSpawn = true;
    [SerializeField] private float pixelsPerUnit = 40f;

    [Header("Render de la bala (auto)")]
    [SerializeField] private string bulletSortingLayer = ""; // si vacío, copia del Player
    [SerializeField] private int bulletOrderInLayer = 10;

    [Header("Input")]
    [SerializeField] private bool fireWhileHolding = true; // true: mantener, false: solo tap

    private Collider2D ownerCol;
    private float fireCooldown;
    private Vector2 lastPressedAimDir = Vector2.right;

    // layer/name detectados del Player para copiar a las balas si bulletSortingLayer vacío
    private string detectedLayerName = "Default";
    private int detectedMaxOrder = 0;

    void Awake()
    {
        if (!weaponStats) weaponStats = GetComponentInParent<PlayerWeaponStats>();
        ownerCol = GetComponent<Collider2D>();

        // Detectar sorting layer / order del Player
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (sr.sortingOrder > detectedMaxOrder)
            {
                detectedMaxOrder = sr.sortingOrder;
                detectedLayerName = sr.sortingLayerName;
            }
        }
        if (string.IsNullOrEmpty(bulletSortingLayer))
            bulletSortingLayer = detectedLayerName;

        if (!muzzleRight || !muzzleLeft || !muzzleUp || !muzzleDown)
            Debug.LogWarning("[PlayerShooterMuzzles] Asigna los 4 muzzles en el inspector.");
        if (!bulletPrefab && !bulletPrefabRight && !bulletPrefabLeft && !bulletPrefabUp && !bulletPrefabDown)
            Debug.LogWarning("[PlayerShooterMuzzles] No hay prefabs de bala asignados.");
    }

    void Update()
    {
        float rof = weaponStats ? Mathf.Max(0.05f, weaponStats.fireRate) : 6f;
        fireCooldown -= Time.deltaTime;

        bool pressed = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                       Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);

        bool holding = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow) ||
                       Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);

        bool wantsFire = fireWhileHolding ? holding : pressed;
        if (!wantsFire || fireCooldown > 0f) return;

        Vector2 dir = DetermineAimDirImmediate();
        if (dir == Vector2.zero) return;

        // Flash opcional
        if (muzzleFlash) muzzleFlash.Show(dir);

        Fire(dir);
        fireCooldown = 1f / rof;
    }

    Vector2 DetermineAimDirImmediate()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) { lastPressedAimDir = Vector2.right; return lastPressedAimDir; }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { lastPressedAimDir = Vector2.left; return lastPressedAimDir; }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { lastPressedAimDir = Vector2.up; return lastPressedAimDir; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { lastPressedAimDir = Vector2.down; return lastPressedAimDir; }

        if (Input.GetKey(KeyCode.RightArrow)) { lastPressedAimDir = Vector2.right; return lastPressedAimDir; }
        if (Input.GetKey(KeyCode.LeftArrow)) { lastPressedAimDir = Vector2.left; return lastPressedAimDir; }
        if (Input.GetKey(KeyCode.UpArrow)) { lastPressedAimDir = Vector2.up; return lastPressedAimDir; }
        if (Input.GetKey(KeyCode.DownArrow)) { lastPressedAimDir = Vector2.down; return lastPressedAimDir; }

        return lastPressedAimDir;
    }

    void Fire(Vector2 dir)
    {
        Transform muzzle = GetMuzzle(dir);
        GameObject prefab = GetBulletPrefab(dir);
        if (!muzzle || !prefab) return;

        Vector3 spawnPos = muzzle.position + (Vector3)spawnOffset;
        if (pixelSnapSpawn && pixelsPerUnit > 0f)
        {
            spawnPos.x = Mathf.Round(spawnPos.x * pixelsPerUnit) / pixelsPerUnit;
            spawnPos.y = Mathf.Round(spawnPos.y * pixelsPerUnit) / pixelsPerUnit;
        }
        spawnPos.z = 0f;

        GameObject b = Instantiate(prefab, spawnPos, Quaternion.identity);
        b.transform.localScale = Vector3.one;

        // Ignorar colisión con el Player
        var bCol = b.GetComponent<Collider2D>();
        if (ownerCol && bCol) Physics2D.IgnoreCollision(ownerCol, bCol, true);

        EnsureBulletVisible(b);

        // Aplicar stats del arma a la bala
        ApplyStatsToBullet(b, dir);
    }

    void ApplyStatsToBullet(GameObject b, Vector2 dir)
    {
        float bulletSpeed = weaponStats ? weaponStats.bulletSpeed : 16f;

        var proj = b.GetComponent<Projectile2D>();
        if (proj)
        {
            float dmg = weaponStats ? weaponStats.damage : proj.damage;
            bool prc = weaponStats ? weaponStats.piercing : proj.piercing;
            bool bnc = weaponStats ? weaponStats.bouncing : proj.bouncing;
            int mb = weaponStats ? weaponStats.maxBounces : proj.maxBounces;

            proj.SetStats(dmg, prc, bnc, mb);
            proj.Launch(dir, bulletSpeed);
        }
        else
        {
            // Prefab sin Projectile2D: empuje directo por RB si existe
            var rb = b.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = dir.normalized * bulletSpeed;
        }
    }

    void EnsureBulletVisible(GameObject b)
    {
        var sr = b.GetComponent<SpriteRenderer>() ?? b.GetComponentInChildren<SpriteRenderer>();
        if (!sr) return;

        sr.enabled = true;
        if (!string.IsNullOrEmpty(bulletSortingLayer))
            sr.sortingLayerName = bulletSortingLayer;
        sr.sortingOrder = bulletOrderInLayer;

        var c = sr.color; c.a = Mathf.Clamp01(c.a <= 0f ? 1f : c.a); sr.color = c;

        var t = sr.transform;
        t.position = new Vector3(t.position.x, t.position.y, 0f);
    }

    Transform GetMuzzle(Vector2 dir)
    {
        if (dir == Vector2.right) return muzzleRight;
        if (dir == Vector2.left) return muzzleLeft;
        if (dir == Vector2.up) return muzzleUp;
        if (dir == Vector2.down) return muzzleDown;
        return transform;
    }

    GameObject GetBulletPrefab(Vector2 dir)
    {
        if (dir == Vector2.right && bulletPrefabRight) return bulletPrefabRight;
        if (dir == Vector2.left && bulletPrefabLeft) return bulletPrefabLeft;
        if (dir == Vector2.up && bulletPrefabUp) return bulletPrefabUp;
        if (dir == Vector2.down && bulletPrefabDown) return bulletPrefabDown;
        return bulletPrefab; // fallback
    }
}
