using UnityEngine;

/// ScriptableObject de mejora de arma.
/// Crea instancias: Right click > Create > ZLayer/Weapon Upgrade
[CreateAssetMenu(menuName = "ZLayer/Weapon Upgrade", fileName = "WPN_Upgrade")]
public class WeaponUpgradeSO : ScriptableObject
{
    [Header("Meta")]
    public string displayName = "Upgrade";
    [TextArea(2, 4)] public string description = "";
    public Sprite icon;

    [Header("Aditivo")]
    public float addDamage = 0f;
    public float addFireRate = 0f;     // +disparos/seg
    public float addBulletSpeed = 0f;
    public int addMaxBounces = 0;

    [Header("Multiplicador (x)")]
    [Min(0f)] public float mulDamage = 1f;
    [Min(0f)] public float mulFireRate = 1f;
    [Min(0f)] public float mulBulletSpeed = 1f;

    [Header("Flags")]
    public bool enablePiercing = false;
    public bool enableBouncing = false;
}
