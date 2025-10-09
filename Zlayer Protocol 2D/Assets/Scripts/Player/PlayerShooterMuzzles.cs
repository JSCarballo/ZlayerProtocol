using UnityEngine;

/// Dispara usando 4 muzzles y 4 prefabs (uno por dirección).
/// - Corrige taps: usa SIEMPRE la última flecha presionada ESTE frame.
/// - Garantiza visibilidad de la bala: z=0, SR enabled, sorting layer/orden configurables.
/// - Pixel-snap del spawn para PPU=40 (opcional).
[RequireComponent(typeof(Collider2D))]
public class PlayerShooterMuzzles : MonoBehaviour
{
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

    [Header("Disparo")]
    [SerializeField, Tooltip("Balas/seg cuando mantienes flechas")]
    private float fireRate = 6f;
    [SerializeField, Tooltip("Velocidad si el prefab no usa Projectile2D")]
    private float defaultBulletSpeed = 16f;
    [SerializeField, Tooltip("Si ON, dispara al mantener; si OFF, solo con tap")]
    private bool fireOnHold = true;

    [Header("Spawn")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private bool pixelSnapSpawn = true;
    [SerializeField] private float pixelsPerUnit = 40f;

    [Header("Render de la bala (auto)")]
    [SerializeField] private string bulletSortingLayer = ""; // si vacío, usa la capa del SR con mayor order en el Player
    [SerializeField] private int bulletOrderInLayer = 10; // 10 por defecto para estar por encima de Torso (1/2)

    private PlayerAim4Dir aimProvider;
    private PlayerAnim2D animSync;
    private Collider2D ownerCol;
    private float fireCooldown;
    private Vector2 lastPressedAimDir = Vector2.right;

    // Defaults detectados del Player (para copiar a las balas si bulletSortingLayer está vacío)
    private string detectedLayerName = "Default";
    private int detectedMaxOrder = 0;

    void Awake()
    {
        aimProvider = GetComponent<PlayerAim4Dir>();
        animSync = GetComponent<PlayerAnim2D>();
        ownerCol = GetComponent<Collider2D>();

        // Detectar la sorting layer y el mayor order del Player (Torso/Legs/Flash)
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
        {
            bulletSortingLayer = detectedLayerName;
            // si no setean un order, deja bulletOrderInLayer como está (10) para sobresalir
        }

        if (!muzzleRight || !muzzleLeft || !muzzleUp || !muzzleDown)
            Debug.LogWarning("[PlayerShooterMuzzles] Asigna los 4 muzzles en el inspector.");
        if (!bulletPrefab && !bulletPrefabRight && !bulletPrefabLeft && !bulletPrefabUp && !bulletPrefabDown)
            Debug.LogWarning("[PlayerShooterMuzzles] No hay prefabs de bala asignados.");
    }

    void Update()
    {
        fireCooldown -= Time.deltaTime;

        bool pressed = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                       Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);

        bool holding = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow) ||
                       Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);

        bool wantsFire = fireOnHold ? holding : pressed;
        if (!wantsFire || fireCooldown > 0f) return;

        // Dirección de disparo *inmediata* este frame (corrige taps)
        Vector2 dir = DetermineAimDirImmediate();
        if (dir == Vector2.zero) return;

        // Forzar torso a esa dir este mismo frame (coherencia visual)
        if (animSync) animSync.SetAimInstant(dir);

        // Fogonazo correcto
        if (muzzleFlash) muzzleFlash.Show(dir);

        // Disparo
        Fire(dir);
        fireCooldown = 1f / Mathf.Max(0.01f, fireRate);
    }

    Vector2 DetermineAimDirImmediate()
    {
        // Prioridad: tecla presionada ESTE frame
        if (Input.GetKeyDown(KeyCode.RightArrow)) { lastPressedAimDir = Vector2.right; return lastPressedAimDir; }
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { lastPressedAimDir = Vector2.left; return lastPressedAimDir; }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { lastPressedAimDir = Vector2.up; return lastPressedAimDir; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { lastPressedAimDir = Vector2.down; return lastPressedAimDir; }

        // Si estamos en modo hold, usa la tecla mantenida
        if (fireOnHold)
        {
            if (Input.GetKey(KeyCode.RightArrow)) { lastPressedAimDir = Vector2.right; return lastPressedAimDir; }
            if (Input.GetKey(KeyCode.LeftArrow)) { lastPressedAimDir = Vector2.left; return lastPressedAimDir; }
            if (Input.GetKey(KeyCode.UpArrow)) { lastPressedAimDir = Vector2.up; return lastPressedAimDir; }
            if (Input.GetKey(KeyCode.DownArrow)) { lastPressedAimDir = Vector2.down; return lastPressedAimDir; }
        }

        // Fallback: proveedor de aim o última válida
        if (aimProvider != null)
        {
            Vector2 a = aimProvider.GetAimDir();
            if (a != Vector2.zero) { lastPressedAimDir = a; return a; }
        }
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
        spawnPos.z = 0f; // asegura render en 2D

        GameObject b = Instantiate(prefab, spawnPos, Quaternion.identity);
        b.transform.localScale = Vector3.one; // evita escalas raras heredadas

        // Ignorar colisión con el Player
        var bCol = b.GetComponent<Collider2D>();
        if (ownerCol && bCol) Physics2D.IgnoreCollision(ownerCol, bCol, true);

        // Garantizar visibilidad de la bala
        EnsureBulletVisible(b);

        // Empuje
        var proj = b.GetComponent<Projectile2D>();
        if (proj) proj.Launch(dir);
        else
        {
            var rb = b.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = dir.normalized * defaultBulletSpeed;
        }
    }

    void EnsureBulletVisible(GameObject b)
    {
        // Busca SpriteRenderer en el objeto o hijos
        var sr = b.GetComponent<SpriteRenderer>();
        if (!sr) sr = b.GetComponentInChildren<SpriteRenderer>();

        if (!sr)
        {
            Debug.LogWarning("[PlayerShooterMuzzles] La bala instanciada no tiene SpriteRenderer. Asigna uno al prefab.");
            return;
        }

        // Habilitar, capa de dibujo y orden
        sr.enabled = true;
        if (!string.IsNullOrEmpty(bulletSortingLayer))
            sr.sortingLayerName = bulletSortingLayer;
        sr.sortingOrder = bulletOrderInLayer;

        // Asegurar alpha visible
        var c = sr.color;
        c.a = Mathf.Clamp01(c.a <= 0f ? 1f : c.a);
        sr.color = c;

        // Z a 0 en el renderer por si el prefab trae offset
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
