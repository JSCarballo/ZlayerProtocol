using UnityEngine;

/// Dispara usando los 4 muzzles colocados en el prefab del Player.
/// Lee la dirección actual desde PlayerAim4Dir (flechas) y dispara con cadencia.
/// Soporta disparo al mantener (auto-fire) o solo por pulsación.
[RequireComponent(typeof(Collider2D))]
public class PlayerShooterMuzzles : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Muzzles (asigna los 4 del prefab)")]
    [SerializeField] private Transform muzzleRight;
    [SerializeField] private Transform muzzleLeft;
    [SerializeField] private Transform muzzleUp;
    [SerializeField] private Transform muzzleDown;

    [Header("Disparo")]
    [Tooltip("Balas por segundo cuando mantienes la flecha.")]
    [SerializeField] private float fireRate = 6f;
    [Tooltip("Velocidad en unidades/seg si el prefab no trae su propia velocidad.")]
    [SerializeField] private float defaultBulletSpeed = 16f;
    [Tooltip("Si está activo, dispara automáticamente al mantener flechas; si no, solo al pulsarlas.")]
    [SerializeField] private bool fireOnHold = true;

    [Header("Opcional")]
    [Tooltip("Pequeño desplazamiento extra a partir del muzzle")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;

    // refs
    private PlayerAim4Dir aim;
    private Collider2D ownerCol;
    private float fireCooldown;

    void Awake()
    {
        aim = GetComponent<PlayerAim4Dir>();
        ownerCol = GetComponent<Collider2D>();

        if (!bulletPrefab) Debug.LogWarning("[PlayerShooterMuzzles] Falta bulletPrefab.");
        if (!muzzleRight || !muzzleLeft || !muzzleUp || !muzzleDown)
            Debug.LogWarning("[PlayerShooterMuzzles] Asigna los 4 muzzles en el inspector.");
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

        Vector2 dir = (aim != null) ? aim.GetAimDir() : GetDirFromKeyboard();
        if (dir == Vector2.zero) return;

        Fire(dir);
        fireCooldown = 1f / Mathf.Max(0.01f, fireRate);
    }

    Vector2 GetDirFromKeyboard()
    {
        Vector2 d = Vector2.zero;
        if (Input.GetKey(KeyCode.RightArrow)) d = Vector2.right;
        else if (Input.GetKey(KeyCode.LeftArrow)) d = Vector2.left;
        else if (Input.GetKey(KeyCode.UpArrow)) d = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)) d = Vector2.down;
        return d;
    }

    void Fire(Vector2 dir)
    {
        if (!bulletPrefab) return;

        Transform muzzle = GetMuzzle(dir);
        if (!muzzle) muzzle = transform; // fallback

        Vector3 spawnPos = muzzle.position + (Vector3)spawnOffset;

        GameObject b = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // Ignorar colisión con el Player
        var bCol = b.GetComponent<Collider2D>();
        if (ownerCol && bCol) Physics2D.IgnoreCollision(ownerCol, bCol, true);

        // Si la bala tiene Projectile2D, usarlo; si no, aplicar velocidad directa al Rigidbody2D
        var proj = b.GetComponent<Projectile2D>();
        if (proj)
        {
            proj.Launch(dir);
        }
        else
        {
            var rb = b.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = dir.normalized * defaultBulletSpeed;
        }

        // Avisar a animaciones (opcional)
        var anim = GetComponent<PlayerAnim2D>();
        if (anim) anim.NotifyShot(dir);
    }

    Transform GetMuzzle(Vector2 dir)
    {
        if (dir == Vector2.right) return muzzleRight;
        if (dir == Vector2.left) return muzzleLeft;
        if (dir == Vector2.up) return muzzleUp;
        if (dir == Vector2.down) return muzzleDown;
        return null;
    }
}
