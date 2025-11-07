// Assets/Scripts/Enemies/EnemyChaseAI.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChaseAI : MonoBehaviour
{
    [Header("Movimiento base")]
    public float moveSpeed = 2.5f;
    public float stopDistance = 0.5f; // distancia mínima al jugador (colchón)

    [Header("Encerrona (anillo)")]
    public float encircleRadius = 1.6f;  // radio deseado del anillo
    public int encircleSlots = 6;         // # posiciones alrededor del player
    [Tooltip("Dejar en -1 para auto-asignar un slot. Si >=0 usa ese índice fijo.")]
    public int slotIndex = -1;

    [Header("Flanqueo / bloqueo")]
    [Tooltip("Distancia por delante del jugador para bloquear su avance.")]
    public float blockAheadDistance = 1.8f;
    [Tooltip("Horizonte de predicción en segundos para el pursue (0..0.6s típico).")]
    [Range(0f, 0.8f)] public float pursueHorizon = 0.45f;

    public enum SwirlMode { Auto, Clockwise, CounterClockwise, None }
    [Tooltip("Sentido de giro (tangencial) alrededor del jugador.")]
    public SwirlMode swirl = SwirlMode.Auto;

    [Header("Steering Weights")]
    [Range(0f, 5f)] public float wPursue = 0.8f;
    [Range(0f, 5f)] public float wRing = 1.4f;
    [Range(0f, 5f)] public float wTangential = 1.0f;
    [Range(0f, 5f)] public float wBlock = 1.0f;
    [Range(0f, 5f)] public float wSeparation = 1.6f;
    [Range(0f, 5f)] public float wAvoid = 2.0f;

    [Header("Separación entre enemigos")]
    public float separationRadius = 0.7f;
    public float separationFalloff = 1.0f; // 1/r^falloff

    [Header("Evitación de paredes/obstáculos")]
    public LayerMask obstacleMask;
    public float wallAvoidDistance = 0.6f;

    [Header("Búsqueda de Player")]
    public string playerTag = "Player";
    private Transform target;

    // --- Internos ---
    Rigidbody2D rb;
    static int _globalSlotCounter = 0;
    int _assignedSlot = 0;
    Vector2 lastPlayerPos;
    Vector2 playerVel;
    bool haveLastPlayerPos = false;
    float reacquireTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ReacquirePlayer();

        if (slotIndex >= 0) _assignedSlot = slotIndex % Mathf.Max(1, encircleSlots);
        else _assignedSlot = (_globalSlotCounter++) % Mathf.Max(1, encircleSlots);
    }

    void OnEnable()
    {
        ReacquirePlayer();
        haveLastPlayerPos = false;
    }

    void ReacquirePlayer()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        target = p ? p.transform : null;
    }

    void FixedUpdate()
    {
        // Reacquirir si se perdió
        if (target == null)
        {
            reacquireTimer -= Time.fixedDeltaTime;
            if (reacquireTimer <= 0f) { ReacquirePlayer(); reacquireTimer = 0.5f; }
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dt = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        Vector2 playerPos = target.position;

        // Velocidad del player por diferencia
        if (!haveLastPlayerPos)
        {
            lastPlayerPos = playerPos;
            haveLastPlayerPos = true;
        }
        playerVel = (playerPos - lastPlayerPos) / dt;
        lastPlayerPos = playerPos;

        Vector2 myPos = rb.position;
        Vector2 toPlayer = playerPos - myPos;
        float distToPlayer = toPlayer.magnitude;
        Vector2 toPlayerN = distToPlayer > 0.0001f ? (toPlayer / distToPlayer) : Vector2.zero;

        // --- 1) Pursue (predicción simple del futuro del player) ---
        float tPred = Mathf.Clamp(distToPlayer / Mathf.Max(moveSpeed, 0.01f), 0f, pursueHorizon);
        Vector2 predicted = playerPos + playerVel * tPred;
        Vector2 pursueDir = (predicted - myPos);
        if (pursueDir.sqrMagnitude > 0.0001f) pursueDir.Normalize();

        // --- 2) Encircle: punto de anillo (slot) alrededor del player ---
        // BaseAngle se ancla al vector de velocidad del player (si no, al vector enemigo->player)
        float baseAngle = (playerVel.sqrMagnitude > 0.01f)
            ? Mathf.Atan2(playerVel.y, playerVel.x) + Mathf.PI        // rodea "desde atrás"
            : Mathf.Atan2(toPlayer.y, toPlayer.x);

        float slotAngle = baseAngle + (Mathf.PI * 2f) * (_assignedSlot / Mathf.Max(1f, encircleSlots));
        Vector2 ringPoint = playerPos + new Vector2(Mathf.Cos(slotAngle), Mathf.Sin(slotAngle)) * encircleRadius;
        Vector2 ringDir = ringPoint - myPos;
        if (ringDir.sqrMagnitude > 0.0001f) ringDir.Normalize();

        // --- 3) Tangencial (orbitar para cerrar y no hacer línea recta) ---
        float swirlSign = 0f;
        switch (swirl)
        {
            case SwirlMode.Auto: swirlSign = (_assignedSlot % 2 == 0) ? +1f : -1f; break;
            case SwirlMode.Clockwise: swirlSign = -1f; break;
            case SwirlMode.CounterClockwise: swirlSign = +1f; break;
            case SwirlMode.None: swirlSign = 0f; break;
        }
        Vector2 tangentDir = new Vector2(-toPlayerN.y, toPlayerN.x) * swirlSign; // perpendicular a toPlayer

        // --- 4) Bloqueo: punto por delante del player ---
        Vector2 blockDir = Vector2.zero;
        if (playerVel.sqrMagnitude > 0.0001f)
        {
            Vector2 blockPoint = playerPos + (playerVel.normalized * blockAheadDistance);
            blockDir = (blockPoint - myPos);
            if (blockDir.sqrMagnitude > 0.0001f) blockDir.Normalize();
        }

        // --- 5) Separación con otros enemigos (anti-amontonamiento) ---
        Vector2 separation = Vector2.zero;
        if (separationRadius > 0.01f)
        {
            // Busca vecinos con tag "Enemy" (igual que este)
            Collider2D[] hits = Physics2D.OverlapCircleAll(myPos, separationRadius);
            foreach (var h in hits)
            {
                if (h.attachedRigidbody == rb) continue; // yo mismo
                if (!h || !h.transform || !h.transform.CompareTag(gameObject.tag)) continue; // solo mis "pares"
                Vector2 otherPos = h.attachedRigidbody ? h.attachedRigidbody.position : (Vector2)h.transform.position;
                Vector2 away = myPos - otherPos;
                float d2 = away.sqrMagnitude + 0.0001f;
                separation += away.normalized / Mathf.Pow(Mathf.Max(d2, 0.0001f), separationFalloff * 0.5f);
            }
            if (separation.sqrMagnitude > 0.0001f) separation.Normalize();
        }

        // --- 6) Evitar paredes/obstáculos ---
        Vector2 avoid = Vector2.zero; // se calcula después de un "pre-steer"
        // Pre-steer para decidir hacia dónde mirar el raycast
        Vector2 preSteer =
            wPursue * pursueDir +
            wRing * ringDir +
            wTangential * tangentDir +
            wBlock * blockDir +
            wSeparation * separation;

        if (preSteer.sqrMagnitude > 0.0001f)
        {
            Vector2 dir = preSteer.normalized;
            RaycastHit2D hit = Physics2D.Raycast(myPos, dir, wallAvoidDistance, obstacleMask);
            if (hit.collider != null)
            {
                // Empuja lejos de la pared siguiendo la normal de impacto.
                // También puedes usar una componente tangencial si prefieres "bordear".
                avoid = hit.normal;
                avoid.Normalize();
            }
        }

        // --- 7) Combinación final de steering ---
        Vector2 steer =
            wPursue * pursueDir +
            wRing * ringDir +
            wTangential * tangentDir +
            wBlock * blockDir +
            wSeparation * separation +
            wAvoid * avoid;

        // Si estamos demasiado encima del player, prioriza giro tangencial para rodearlo
        if (distToPlayer < stopDistance + 0.1f)
        {
            steer += tangentDir * 2.0f;
        }

        if (steer.sqrMagnitude < 0.0001f)
        {
            // Fallback: al menos mirar al player
            steer = toPlayerN;
        }
        steer.Normalize();

        // Aplicar movimiento
        rb.MovePosition(rb.position + steer * moveSpeed * dt);
    }

    // Gizmos para depurar
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        if (!target) return;

        Gizmos.color = new Color(0.1f, 0.8f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(target.position, encircleRadius);

        // Dibuja mi slot
        float baseAngle = (playerVel.sqrMagnitude > 0.01f)
            ? Mathf.Atan2(playerVel.y, playerVel.x) + Mathf.PI
            : Mathf.Atan2(((Vector2)target.position - (rb ? rb.position : (Vector2)transform.position)).y,
                          ((Vector2)target.position - (rb ? rb.position : (Vector2)transform.position)).x);

        float slotAngle = baseAngle + (Mathf.PI * 2f) * (_assignedSlot / Mathf.Max(1f, encircleSlots));
        Vector2 ringPoint = (Vector2)target.position + new Vector2(Mathf.Cos(slotAngle), Mathf.Sin(slotAngle)) * encircleRadius;
        Gizmos.color = new Color(0.9f, 0.7f, 0.1f, 0.8f);
        Gizmos.DrawSphere(ringPoint, 0.06f);
        Gizmos.DrawLine(rb ? (Vector3)rb.position : transform.position, ringPoint);
    }
}
