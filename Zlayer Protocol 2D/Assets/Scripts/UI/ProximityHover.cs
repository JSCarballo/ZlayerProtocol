// Assets/Scripts/UI/ProximityHover.cs
using UnityEngine;

[RequireComponent(typeof(WeaponUpgradePickup))]
public class ProximityHover : MonoBehaviour
{
    public float showRadius = 1.6f;
    public string overrideTitle;
    [TextArea] public string overrideDesc;

    Transform player;
    WeaponUpgradePickup pickup;
    bool showing;

    void Awake()
    {
        pickup = GetComponent<WeaponUpgradePickup>();
    }

    void Update()
    {
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
            if (!player) return;
        }

        float d = Vector2.Distance(player.position, transform.position);
        bool shouldShow = d <= showRadius;

        if (shouldShow && !showing && HoverLabelController.Instance)
        {
            string title = string.IsNullOrEmpty(overrideTitle) ? pickup.displayName : overrideTitle;
            string desc = string.IsNullOrEmpty(overrideDesc) ? pickup.description : overrideDesc;
            HoverLabelController.Instance.Show(transform, title, desc);
            showing = true;
        }
        else if (!shouldShow && showing && HoverLabelController.Instance)
        {
            HoverLabelController.Instance.Hide();
            showing = false;
        }
    }

    void OnDisable()
    {
        if (showing && HoverLabelController.Instance) HoverLabelController.Instance.Hide();
        showing = false;
    }
}
