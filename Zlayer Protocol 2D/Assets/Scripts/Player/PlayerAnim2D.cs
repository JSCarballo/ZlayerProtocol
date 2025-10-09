using UnityEngine;

/// Sincroniza Torso (apuntado) y Legs (caminar) en 4 direcciones con sprites 40x40.
/// - Piernas: Idle/Walk inmediato según input (no por inercia).
/// - Torso : Aim cardinal (±1,0)/(0,±1) y actualización instantánea al disparar.
/// - Pixel perfect: snap opcional del PlayerRoot (sin offsets en hijos).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnim2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement2D movement;
    [SerializeField] private PlayerAim4Dir aimProvider;

    [Header("Animators")]
    [SerializeField] private Animator torsoAnimator; // TorsoAnimator.controller (BlendTree 2D Dir con AimX/AimY)
    [SerializeField] private Animator legsAnimator;  // LegsAnimator.controller  (IdleBT/WalkBT)

    [Header("Legs state names (Animator)")]
    [SerializeField] private string legsStateIdle = "IdleBT";
    [SerializeField] private string legsStateWalk = "WalkBT";

    [Header("Pixel perfect")]
    [SerializeField] private bool pixelSnapRoot = true; // snap del PlayerRoot
    [SerializeField] private float pixelsPerUnit = 40f; // PPU = 40 para tus sprites 40x40

    [Header("Varios")]
    [SerializeField] private bool snapAimToCardinal = true; // forzar direcciones puras
    [SerializeField] private float legsSpeedParam = 1f;    // 1 para Walk, 0 para Idle (cambio inmediato)

    private Rigidbody2D rb;
    private Vector2 lastMoveDir = Vector2.down;
    private Vector2 lastAimDir = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!movement) movement = GetComponent<PlayerMovement2D>();
        if (!aimProvider) aimProvider = GetComponent<PlayerAim4Dir>();
    }

    void Update()
    {
        UpdateLegsImmediate(); // fuerza Idle/Walk al instante por input WASD
        UpdateTorsoParams();   // AimX/AimY cardinales para el BlendTree
    }

    void LateUpdate()
    {
        // Pixel snap del root para máxima estabilidad en cámara pixel-perfect
        if (pixelSnapRoot) transform.position = SnapWorld(transform.position);
    }

    // --------- LEGS: Idle/Walk inmediato (según input) ---------
    void UpdateLegsImmediate()
    {
        if (!legsAnimator) return;

        Vector2 inputMove = movement ? movement.MoveVector : Vector2.zero;
        bool isMoving = inputMove.sqrMagnitude > 0f;

        if (inputMove != Vector2.zero) lastMoveDir = inputMove;

        // Estos parámetros alimentan tus Blend Trees (IdleBT/WalkBT)
        legsAnimator.SetFloat("MoveX", inputMove.x);
        legsAnimator.SetFloat("MoveY", inputMove.y);
        legsAnimator.SetFloat("LastMoveX", lastMoveDir.x);
        legsAnimator.SetFloat("LastMoveY", lastMoveDir.y);

        // Speed como 1/0 para transiciones instantáneas por condición
        legsAnimator.SetFloat("Speed", isMoving ? legsSpeedParam : 0f);

        // Forzar cambio de estado sin duración; evita que "termine" el ciclo de caminar
        if (!legsAnimator.IsInTransition(0))
        {
            var st = legsAnimator.GetCurrentAnimatorStateInfo(0);
            if (isMoving && !st.IsName(legsStateWalk))
                legsAnimator.CrossFadeInFixedTime(legsStateWalk, 0f);
            else if (!isMoving && !st.IsName(legsStateIdle))
                legsAnimator.CrossFadeInFixedTime(legsStateIdle, 0f);
        }
    }

    // --------- TORSO: Aim 4-dir para el Blend Tree ---------
    void UpdateTorsoParams()
    {
        if (!torsoAnimator) return;

        if (aimProvider)
        {
            Vector2 aim = aimProvider.GetAimDir();
            if (aim != Vector2.zero) lastAimDir = aim;
        }

        if (snapAimToCardinal) lastAimDir = ToCardinal(lastAimDir);

        torsoAnimator.SetFloat("AimX", lastAimDir.x);
        torsoAnimator.SetFloat("AimY", lastAimDir.y);
    }

    Vector2 ToCardinal(Vector2 v)
    {
        if (v == Vector2.zero) return Vector2.down;
        return (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            ? new Vector2(Mathf.Sign(v.x), 0f)
            : new Vector2(0f, Mathf.Sign(v.y));
    }

    Vector3 SnapWorld(Vector3 pos)
    {
        if (pixelsPerUnit <= 0f) return pos;
        float ppu = pixelsPerUnit;
        pos.x = Mathf.Round(pos.x * ppu) / ppu;
        pos.y = Mathf.Round(pos.y * ppu) / ppu;
        return pos;
    }

    /// Llamado por el Shooter justo antes de disparar: fija aim YA.
    public void SetAimInstant(Vector2 aimDir)
    {
        lastAimDir = ToCardinal(aimDir == Vector2.zero ? lastAimDir : aimDir);

        if (torsoAnimator)
        {
            torsoAnimator.SetFloat("AimX", lastAimDir.x);
            torsoAnimator.SetFloat("AimY", lastAimDir.y);
        }
    }

    public void NotifyShot(Vector2 aimDir) => SetAimInstant(aimDir);
}
