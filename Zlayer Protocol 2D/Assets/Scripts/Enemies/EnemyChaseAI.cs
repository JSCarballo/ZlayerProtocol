// Assets/Scripts/Enemies/EnemyChaseAI.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChaseAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float stopDistance = 0.4f;

    Rigidbody2D rb;
    Transform target;    // referencia al Player
    float reacquireTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ReacquirePlayer();
    }

    void OnEnable()
    {
        // por si el prefab estaba desactivado en el momento del Awake
        ReacquirePlayer();
    }

    void ReacquirePlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        target = p ? p.transform : null;
    }

    void FixedUpdate()
    {
        // Si perdimos referencia, reintentar cada 0.5s
        if (target == null)
        {
            reacquireTimer -= Time.fixedDeltaTime;
            if (reacquireTimer <= 0f)
            {
                ReacquirePlayer();
                reacquireTimer = 0.5f;
            }
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 to = (target.position - transform.position);
        float dist = to.magnitude;

        // Velocidad deseada
        Vector2 vel = dist > stopDistance ? to.normalized * moveSpeed : Vector2.zero;

        // MovePosition es m�s fiable que setear velocity cuando hay Tilemaps/colliders
        rb.MovePosition(rb.position + vel * Time.fixedDeltaTime);
    }
}
