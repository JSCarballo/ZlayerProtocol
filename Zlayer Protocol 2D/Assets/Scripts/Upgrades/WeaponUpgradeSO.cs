using UnityEngine;

[CreateAssetMenu(menuName = "ZLayer/Weapon Upgrade", fileName = "WU_NewUpgrade")]
public class WeaponUpgradeSO : ScriptableObject
{
    [Header("Identidad (�nica)")]
    public string id = "UP_XXXX";     // �nico en la pool (ej: UP_DMG_20)
    public string displayName = "Upgrade";
    [TextArea] public string description;

    [Header("Visual")]
    public Sprite icon;

    [Header("Definici�n de mejora")]
    public WeaponUpgrade upgrade;

    [Header("Selecci�n")]
    public bool unique = true;        // si true, NO puede repetirse en la run
    public float weight = 1f;         // peso para aleatoriedad; >1 m�s probable
}
