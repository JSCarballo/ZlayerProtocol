using UnityEngine;

/// <summary>
/// Controls the player character in a 2D top‑down shooter. Handles movement,
/// aiming towards the mouse pointer and firing the equipped gun. Collecting
/// power‑ups is managed via OnTriggerEnter2D.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] public float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;

    [Header("Weapon")]
    [SerializeField] private Gun gun; // reference to the player's gun

    private Vector2 moveInput;
    private Vector2 mousePos;

    private void Update()
    {
        // Capture movement input; Normalize to prevent faster diagonal movement
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        // Convert mouse screen position to world position
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Fire weapon when primary mouse button is pressed
        if (Input.GetButton("Fire1"))
        {
            gun.Fire();
        }
    }

    private void FixedUpdate()
    {
        // Move the player using the Rigidbody for smooth movement and collision handling
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);

        // Rotate the player to face the mouse cursor
        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collider has a PowerUp component; if so, apply it
        PowerUp power = collision.GetComponent<PowerUp>();
        if (power != null)
        {
            power.Apply(this);
        }
    }
}