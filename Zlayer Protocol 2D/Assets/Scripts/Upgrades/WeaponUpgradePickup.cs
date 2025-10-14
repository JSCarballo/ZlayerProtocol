using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class WeaponUpgradePickup : MonoBehaviour
{
    // Eventos
    public event Action<ScriptableObject> onPicked;
    public static event Action<WeaponUpgradePickup> AnyPicked;

    [Header("Detección de Player")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayers = 0; // 0 = no filtrar por capas

    [Header("Upgrade")]
    public bool useScriptable = true;
    public WeaponUpgradeSO upgradeSO;

    [Header("Feedback")]
    [SerializeField] private GameObject pickupVFX;
    [SerializeField] private AudioClip pickupSFX;
    [SerializeField] private float destroyDelay = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [SerializeField, Tooltip("Solo lectura")] private bool consumed = false;
    public bool IsConsumed => consumed;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    public void Assign(WeaponUpgradeSO so)
    {
        upgradeSO = so;
        useScriptable = so != null;
    }

    void OnTriggerEnter2D(Collider2D other) => TryPickup(other.gameObject);
    void OnCollisionEnter2D(Collision2D col) => TryPickup(col.collider.gameObject);

    void TryPickup(GameObject other)
    {
        if (consumed || !other) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        if (playerLayers.value != 0 && (playerLayers.value & (1 << other.layer)) == 0) return;

        var root = other.GetComponentInParent<Transform>(); if (!root) return;
        var pws = root.GetComponentInChildren<PlayerWeaponStats>();
        if (!pws)
        {
            if (debugLogs) Debug.LogWarning("[WeaponUpgradePickup] PlayerWeaponStats no encontrado en Player.");
            return;
        }

        // Aplicar
        if (useScriptable && upgradeSO)
        {
            pws.Apply(upgradeSO);
        }
        else
        {
            if (debugLogs) Debug.LogWarning("[WeaponUpgradePickup] upgradeSO nulo.");
            return;
        }

        // Consumir
        consumed = true;
        DisableVisualsAndColliders();

        onPicked?.Invoke(upgradeSO);
        AnyPicked?.Invoke(this);

        if (pickupVFX) Instantiate(pickupVFX, transform.position, Quaternion.identity);
        if (pickupSFX) AudioSource.PlayClipAtPoint(pickupSFX, transform.position, 1f);

        Destroy(gameObject, destroyDelay);
    }

    void DisableVisualsAndColliders()
    {
        foreach (var c in GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
        foreach (var cv in GetComponentsInChildren<Canvas>(true)) cv.enabled = false;
    }
}
