using UnityEngine;

/// Dispara con ? ? ? ? desde 4 "muzzles" diferentes (Up/Down/Left/Right).
public class FourWayShooter : MonoBehaviour
{
    [Header("References")]
    public GameObject bulletPrefab;
    public Transform muzzleUp;
    public Transform muzzleDown;
    public Transform muzzleLeft;
    public Transform muzzleRight;

    [Header("Shooting")]
    [SerializeField] private float bulletsPerSecond = 6f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float bulletLife = 2f;

    private float shootCooldown;

    void Update()
    {
        shootCooldown -= Time.deltaTime;

        // Detecta cu�l flecha est� presionada (prioridad por �ltima presionada)
        if (Input.GetKey(KeyCode.UpArrow)) TryShoot(Vector2.up, muzzleUp);
        else if (Input.GetKey(KeyCode.DownArrow)) TryShoot(Vector2.down, muzzleDown);
        else if (Input.GetKey(KeyCode.LeftArrow)) TryShoot(Vector2.left, muzzleLeft);
        else if (Input.GetKey(KeyCode.RightArrow)) TryShoot(Vector2.right, muzzleRight);
    }

    void TryShoot(Vector2 dir, Transform muzzle)
    {
        if (shootCooldown > 0f) return;
        shootCooldown = 1f / bulletsPerSecond;

        if (bulletPrefab == null || muzzle == null) return;

        var go = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = dir.normalized * bulletSpeed;

        var proj = go.GetComponent<SimpleProjectile>();
        if (proj != null)
        {
            proj.lifeTime = bulletLife;
        }
    }
}
