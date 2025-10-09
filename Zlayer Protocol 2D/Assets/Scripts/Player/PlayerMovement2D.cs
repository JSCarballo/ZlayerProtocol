using UnityEngine;

/// Movimiento 2D top-down con WASD.
/// - Usa Rigidbody2D (Dynamic, Gravity 0).
/// - Expone MoveVector para animaciones.
/// - Suaviza velocidad con aceleraci�n/deceleraci�n.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 80f;

    public Vector2 MoveVector { get; private set; }

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        Vector2 input = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
            (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f)
        );

        MoveVector = input.sqrMagnitude > 1e-4f ? input.normalized : Vector2.zero;
    }

    void FixedUpdate()
    {
        Vector2 targetVel = MoveVector * moveSpeed;
        Vector2 v = rb.linearVelocity;

        float ax = Mathf.Abs(targetVel.x) > Mathf.Abs(v.x) ? acceleration : deceleration;
        float ay = Mathf.Abs(targetVel.y) > Mathf.Abs(v.y) ? acceleration : deceleration;

        v.x = Mathf.MoveTowards(v.x, targetVel.x, ax * Time.fixedDeltaTime);
        v.y = Mathf.MoveTowards(v.y, targetVel.y, ay * Time.fixedDeltaTime);

        rb.linearVelocity = v;
    }
}
