// Assets/Scripts/UI/DamageNumberOnHealth.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageNumberOnHealth : MonoBehaviour
{
    [Header("Enable")]
    public bool enabledForThis = true;

    [Header("Anclaje visual")]
    public Transform anchorOverride;     // si lo asignas, usa este punto
    public float heightOffset = 0.25f;   // metros extra por encima del tope
    public bool useSpriteTop = true;     // prioriza SpriteRenderer.bounds

    [Header("Boss Styling")]
    [Tooltip("Fuerza tratar este objeto como Boss (ignora la etiqueta).")]
    public bool treatAsBoss = false;
    [Tooltip("Si el GameObject tiene esta etiqueta, se renderiza con estilo de Boss.")]
    public string bossTag = "Boss";
    public bool useBossStyle = true;

    private Health hp;
    private SpriteRenderer[] spriteRs;
    private Collider2D[] cols;

    void Awake()
    {
        hp = GetComponent<Health>();
        spriteRs = GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
        cols = GetComponentsInChildren<Collider2D>(includeInactive: false);
    }

    void OnEnable()
    {
        if (hp) hp.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (hp) hp.OnDamaged -= HandleDamaged;
    }

    void HandleDamaged(float amount)
    {
        if (!enabledForThis || amount <= 0f) return;

        var style = (useBossStyle && (treatAsBoss || (!string.IsNullOrEmpty(bossTag) && CompareTag(bossTag))))
            ? DamageNumber.Style.Boss
            : DamageNumber.Style.Normal;

        Vector3 worldPos = GetTopWorldPosition();
        DamageNumbersManager.ShowNumber(amount, worldPos, style);
    }

    Vector3 GetTopWorldPosition()
    {
        if (anchorOverride) return anchorOverride.position;

        bool gotAny = false;
        Bounds b = new Bounds(transform.position, Vector3.zero);

        // 1) Sprite bounds (mejor coincide con lo que se ve)
        if (useSpriteTop && spriteRs != null && spriteRs.Length > 0)
        {
            foreach (var sr in spriteRs)
            {
                if (!sr || !sr.enabled || !sr.sprite) continue;
                if (!gotAny) { b = sr.bounds; gotAny = true; } else b.Encapsulate(sr.bounds);
            }
        }

        // 2) Collider bounds
        if (!gotAny && cols != null && cols.Length > 0)
        {
            foreach (var c in cols)
            {
                if (!c || !c.enabled) continue;
                if (!gotAny) { b = c.bounds; gotAny = true; } else b.Encapsulate(c.bounds);
            }
        }

        if (gotAny)
        {
            return new Vector3(b.center.x, b.max.y + heightOffset, transform.position.z);
        }

        // 3) Fallback: arriba del transform
        return transform.position + Vector3.up * (0.6f + heightOffset);
    }
}
