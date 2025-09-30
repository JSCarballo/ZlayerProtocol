using UnityEngine;

/// Movimiento top-down con WASD (sin usar las flechas para evitar conflicto)
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        // Solo WASD: chequeo manual de teclas para no usar flechas
        int x = 0, y = 0;
        if (Input.GetKey(KeyCode.A)) x -= 1;
        if (Input.GetKey(KeyCode.D)) x += 1;
        if (Input.GetKey(KeyCode.S)) y -= 1;
        if (Input.GetKey(KeyCode.W)) y += 1;

        Vector2 dir = new Vector2(x, y).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }
}
