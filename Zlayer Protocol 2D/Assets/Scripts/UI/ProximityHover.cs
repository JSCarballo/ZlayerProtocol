using UnityEngine;

/// Efecto “hover” simple (bob + cambio de escala cerca del Player)
/// Útil para pickups/mejoras. No depende de UI.
public class ProximityHover : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float activateDistance = 2.0f;

    [Header("Bob")]
    [SerializeField] private bool useBob = true;
    [SerializeField] private float bobAmplitude = 0.06f;
    [SerializeField] private float bobSpeed = 3.0f;

    [Header("Escala")]
    [SerializeField] private bool useScale = true;
    [SerializeField] private float farScale = 1.0f;
    [SerializeField] private float nearScale = 1.15f;
    [SerializeField] private float scaleLerp = 10f;

    Vector3 basePos;
    Transform player;
    float t;

    void Awake()
    {
        basePos = transform.position;
    }

    void Update()
    {
        if (!player) FindPlayer();

        // Bob
        Vector3 targetPos = basePos;
        if (useBob)
        {
            t += Time.deltaTime * bobSpeed;
            targetPos.y = basePos.y + Mathf.Sin(t) * bobAmplitude;
        }
        transform.position = targetPos;

        // Escala por proximidad
        if (useScale)
        {
            float s = farScale;
            if (player)
            {
                float d = Vector2.Distance(player.position, transform.position);
                if (d <= activateDistance) s = nearScale;
            }
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * s, Time.deltaTime * scaleLerp);
        }
    }

    void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go) player = go.transform;
    }

    void OnDisable()
    {
        transform.localScale = Vector3.one * farScale;
        t = 0f;
    }
}
