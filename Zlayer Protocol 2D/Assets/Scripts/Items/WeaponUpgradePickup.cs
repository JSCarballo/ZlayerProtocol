// Assets/Scripts/Items/WeaponUpgradePickup.cs
using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class WeaponUpgradePickup : MonoBehaviour
{
    [Header("Asignación")]
    public bool usePoolOnSpawn = true;           // si true, tomará uno de la pool en Start (si no se asignó antes)
    public WeaponUpgradeSO assignedUpgrade;      // si se asigna por código, ignora el uso de pool

    [Header("Visual")]
    public SpriteRenderer iconRenderer;
    public string displayName;
    [TextArea] public string description;

    [Header("Pickup")]
    public bool destroyOnPickup = true;
    public AudioClip sfx;
    public GameObject vfxPrefab;

    // callback para grupos de elección (Armory)
    public Action<WeaponUpgradeSO> onPicked;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        if (!iconRenderer) iconRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Si no nos asignaron nada, pedir a la pool (consumiendo)
        if (assignedUpgrade == null && usePoolOnSpawn && UpgradePoolManager.Instance)
        {
            if (UpgradePoolManager.Instance.DrawRandomUnique(out var so))
                Assign(so);
        }
        ApplyVisual();
    }

    public void Assign(WeaponUpgradeSO so)
    {
        assignedUpgrade = so;
        ApplyVisual();
    }

    void ApplyVisual()
    {
        if (assignedUpgrade != null)
        {
            if (iconRenderer && assignedUpgrade.icon) iconRenderer.sprite = assignedUpgrade.icon;
            displayName = assignedUpgrade.displayName;
            description = assignedUpgrade.description;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var stats = other.GetComponent<PlayerWeaponStats>();
        if (!stats) return;

        WeaponUpgrade data = assignedUpgrade != null ? assignedUpgrade.upgrade : default;
        stats.Apply(data);

        // Notifica al grupo (Armory) cuál fue elegido
        onPicked?.Invoke(assignedUpgrade);

        if (sfx) AudioSource.PlayClipAtPoint(sfx, transform.position);
        if (vfxPrefab) Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        if (destroyOnPickup) Destroy(gameObject);
    }
}
