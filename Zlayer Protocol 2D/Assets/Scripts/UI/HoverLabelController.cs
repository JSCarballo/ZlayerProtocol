using UnityEngine;
using UnityEngine.UI;

/// Muestra Nombre + Descripción del pickup de mejora más cercano.
/// Se oculta inmediatamente cuando se toma cualquier pickup (evento AnyPicked).
public class HoverLabelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image iconImage;   // opcional
    [SerializeField] private CanvasGroup group; // opcional

    [Header("Detección")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float labelDistance = 2.25f;
    [SerializeField] private float refreshInterval = 0.12f;

    [Header("Comportamiento")]
    [SerializeField] private bool hideWhenNone = true;

    Transform player;
    float t;
    GameObject currentTarget;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        HideUI();
    }

    void OnEnable()
    {
        WeaponUpgradePickup.AnyPicked += HandleAnyPicked;
        player = null;
        currentTarget = null;
        HideUI();
    }

    void OnDisable()
    {
        WeaponUpgradePickup.AnyPicked -= HandleAnyPicked;
    }

    void HandleAnyPicked(WeaponUpgradePickup _)
    {
        // Ocultar label al instante al tomar un item
        currentTarget = null;
        HideUI();
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t < refreshInterval) return;
        t = 0f;

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
            if (!player) { HideUI(); return; }
        }

        var best = FindClosestPickup(player.position, labelDistance);
        if (best != currentTarget)
        {
            currentTarget = best;
            if (!currentTarget) { HideUI(); return; }

            if (TryGetInfo(currentTarget, out string name, out string desc, out Sprite icon))
            {
                SetTexts(name, desc);
                SetIcon(icon);
                ShowUI();
            }
            else HideUI();
        }
        else
        {
            if (currentTarget && !IsUsable(currentTarget))
            {
                currentTarget = null;
                HideUI();
            }
        }
    }

    GameObject FindClosestPickup(Vector3 pos, float radius)
    {
        var all = FindObjectsByType<WeaponUpgradePickup>(FindObjectsSortMode.None);
        float bestD = float.MaxValue;
        WeaponUpgradePickup best = null;

        foreach (var p in all)
        {
            if (!p || !IsUsable(p.gameObject)) continue;
            float d = Vector2.Distance(pos, p.transform.position);
            if (d <= radius && d < bestD)
            {
                if (p.upgradeSO != null) { bestD = d; best = p; }
            }
        }
        return best ? best.gameObject : null;
    }

    bool IsUsable(GameObject go)
    {
        if (!go || !go.activeInHierarchy) return false;
        var pick = go.GetComponent<WeaponUpgradePickup>();
        if (!pick || pick.IsConsumed) return false;

        var cols = go.GetComponentsInChildren<Collider2D>(true);
        bool anyCol = false; foreach (var c in cols) if (c && c.enabled) { anyCol = true; break; }
        if (!anyCol) return false;

        var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
        bool anySr = false; foreach (var sr in srs) if (sr && sr.enabled && sr.color.a > 0.001f) { anySr = true; break; }
        if (!anySr) return false;

        return true;
    }

    bool TryGetInfo(GameObject go, out string name, out string desc, out Sprite icon)
    {
        name = null; desc = null; icon = null;
        var p = go.GetComponent<WeaponUpgradePickup>();
        if (!p || !p.upgradeSO) return false;

        var so = p.upgradeSO;
        name = string.IsNullOrWhiteSpace(so.displayName) ? so.name : so.displayName;
        desc = so.description ?? "";
        icon = so.icon;
        return true;
    }

    void SetTexts(string title, string d)
    {
        if (titleText) titleText.text = title ?? "";
        if (descriptionText) descriptionText.text = d ?? "";
    }
    void SetIcon(Sprite sp)
    {
        if (!iconImage) return;
        if (!sp) { iconImage.enabled = false; return; }
        iconImage.enabled = true; iconImage.sprite = sp;
    }

    void HideUI()
    {
        if (hideWhenNone && group)
        {
            group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
        }
        SetTexts("", ""); SetIcon(null);
    }
    void ShowUI()
    {
        if (group)
        {
            group.alpha = 1f; group.interactable = false; group.blocksRaycasts = false;
        }
    }
}
