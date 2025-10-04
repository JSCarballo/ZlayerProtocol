using UnityEngine;

[CreateAssetMenu(menuName = "ZLayer/Weapon Upgrade", fileName = "WU_NewUpgrade")]
public class WeaponUpgradeSO : ScriptableObject
{
    [Header("Identidad (única)")]
    public string id = "UP_XXXX";     // único en la pool (ej: UP_DMG_20)
    public string displayName = "Upgrade";
    [TextArea] public string description;

    [Header("Visual")]
    public Sprite icon;

    [Header("Definición de mejora")]
    public WeaponUpgrade upgrade;

    [Header("Selección")]
    public bool unique = true;        // si true, NO puede repetirse en la run
    public float weight = 1f;         // peso para aleatoriedad; >1 más probable
}
