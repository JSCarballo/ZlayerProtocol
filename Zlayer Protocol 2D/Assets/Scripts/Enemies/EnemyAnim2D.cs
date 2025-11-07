// Assets/Scripts/Enemies/EnemyAnim2D.cs
using UnityEngine;

/// Fuerza las piernas a reproducir siempre un estado de WALK
/// (legs_walk_right/left/up/down) según la dirección hacia el Player.
/// El torso sigue mirando al Player como ya lo tenías.
public class EnemyAnim2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator torsoAnimator;   // Animator de "SpriteTorso"
    [SerializeField] private Animator legsAnimator;    // Animator de "SpriteLegs"
    [SerializeField] private Rigidbody2D rb;           // opcional
    [SerializeField] private Transform target;         // Player; si es null se busca por tag

    [Header("Animator Params (para torso y compatibilidad)")]
    [SerializeField] private string paramIsMoving = "IsMoving";
    [SerializeField] private string paramLastDirX = "LastDirX";
    [SerializeField] private string paramLastDirY = "LastDirY";

    [Header("Control directo de estados (piernas)")]
    [Tooltip("Nombre del estado walk RIGHT en el controller de piernas.")]
    [SerializeField] private string legsWalkRightState = "legs_walk_right";
    [Tooltip("Nombre del estado walk LEFT en el controller de piernas.")]
    [SerializeField] private string legsWalkLeftState = "legs_walk_left";
    [Tooltip("Nombre del estado walk UP en el controller de piernas.")]
    [SerializeField] private string legsWalkUpState = "legs_walk_up";
    [Tooltip("Nombre del estado walk DOWN en el controller de piernas.")]
    [SerializeField] private string legsWalkDownState = "legs_walk_down";

    [Header("Comportamiento")]
    [Tooltip("El torso mira al Player.")]
    [SerializeField] private bool torsoLookAtPlayer = true;
    [Tooltip("Las piernas miran al Player (misma dirección que el torso).")]
    [SerializeField] private bool legsLookAtPlayer = true;
    [Tooltip("Velocidad fija de reproducción del walk de piernas.")]
    [SerializeField] private float legsAnimSpeed = 1.0f;

    // Cache de hashes para CrossFade/Play
    private int _walkRightHash, _walkLeftHash, _walkUpHash, _walkDownHash;

    private Vector2 _lastDir = Vector2.down;
    private float _reacquireCd = 0f;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        foreach (var a in GetComponentsInChildren<Animator>(true))
        {
            var n = a.gameObject.name.ToLower();
            if (!torsoAnimator && n.Contains("torso")) torsoAnimator = a;
            else if (!legsAnimator && n.Contains("legs")) legsAnimator = a;
        }
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        ReacquirePlayer();

        // Precalcular hashes (más robusto que strings cada frame)
        _walkRightHash = Animator.StringToHash(legsWalkRightState);
        _walkLeftHash = Animator.StringToHash(legsWalkLeftState);
        _walkUpHash = Animator.StringToHash(legsWalkUpState);
        _walkDownHash = Animator.StringToHash(legsWalkDownState);
    }

    void OnEnable()
    {
        ReacquirePlayer();
    }

    void Update()
    {
        // Reacquirir player si se pierde
        if (target == null)
        {
            _reacquireCd -= Time.deltaTime;
            if (_reacquireCd <= 0f) { ReacquirePlayer(); _reacquireCd = 0.5f; }
        }

        // --- Dirección hacia el Player (cardinal estricta) ---
        Vector2 dir = _lastDir;
        if (target)
        {
            Vector2 aim = (Vector2)(target.position - transform.position);
            if (aim.sqrMagnitude > 0.0001f) dir = Cardinal(aim);
        }

        // === TORSO: mantiene tu comportamiento original ===
        Vector2 torsoDir = torsoLookAtPlayer ? dir : _lastDir;
        if (torsoAnimator)
        {
            torsoAnimator.SetBool(paramIsMoving, true); // el torso puede ignorarlo, no afecta
            torsoAnimator.SetFloat(paramLastDirX, torsoDir.x);
            torsoAnimator.SetFloat(paramLastDirY, torsoDir.y);
            torsoAnimator.speed = 1f;
        }

        // === PIERNAS: FORZAR WALK por estado ===
        if (legsAnimator)
        {
            // Siempre enviamos IsMoving=true y la dirección (por si conservas condiciones en el controller)
            legsAnimator.SetBool(paramIsMoving, true);
            Vector2 legsDir = legsLookAtPlayer ? dir : _lastDir;
            legsAnimator.SetFloat(paramLastDirX, legsDir.x);
            legsAnimator.SetFloat(paramLastDirY, legsDir.y);

            // Elegir estado de walk según la dirección cardinal
            int targetHash = _walkDownHash; // fallback
            if (Mathf.Abs(legsDir.x) > Mathf.Abs(legsDir.y))
                targetHash = legsDir.x >= 0 ? _walkRightHash : _walkLeftHash;
            else
                targetHash = legsDir.y >= 0 ? _walkUpHash : _walkDownHash;

            // Si no está ya en ese estado, forzar el cambio
            AnimatorStateInfo st = legsAnimator.GetCurrentAnimatorStateInfo(0);
            if (!st.shortNameHash.Equals(targetHash) && !st.fullPathHash.Equals(targetHash))
            {
                // Transición instantánea; si prefieres mezcla, usa 0.05f
                legsAnimator.CrossFade(targetHash, 0f, 0);
            }

            // Velocidad de reproducción fija
            legsAnimator.speed = Mathf.Max(0.01f, legsAnimSpeed);
        }

        // Guardar última dir para idles futuros si los reactivas
        if (torsoDir != Vector2.zero) _lastDir = torsoDir;
    }

    void ReacquirePlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        target = p ? p.transform : null;
    }

    static Vector2 Cardinal(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0 ? Vector2.right : Vector2.left;
        if (Mathf.Abs(v.y) > 0f)
            return v.y >= 0 ? Vector2.up : Vector2.down;
        return Vector2.down;
    }
}
