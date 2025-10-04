using UnityEngine;

public class PlayerWeaponStats : MonoBehaviour
{
    [Header("Multiplicadores")]
    public float damageMult = 1f;
    public float fireRateMult = 1f;        // >1 = dispara más rápido
    public float projectileSpeedMult = 1f;

    [Header("Extras")]
    public int extraProjectiles = 0;       // proyectiles adicionales
    public bool piercing = false;
    public bool bouncing = false;
    public float spreadDegrees = 0f;       // abanico en grados

    public void Apply(WeaponUpgrade upgrade)
    {
        switch (upgrade.type)
        {
            case WeaponUpgrade.Type.DamageMult:
                damageMult *= upgrade.amount; break;
            case WeaponUpgrade.Type.FireRateMult:
                fireRateMult *= upgrade.amount; break;
            case WeaponUpgrade.Type.SpeedMult:
                projectileSpeedMult *= upgrade.amount; break;
            case WeaponUpgrade.Type.ExtraProjectiles:
                extraProjectiles += Mathf.RoundToInt(upgrade.amount); break;
            case WeaponUpgrade.Type.Piercing:
                piercing = true; break;
            case WeaponUpgrade.Type.Bounce:
                bouncing = true; break;
            case WeaponUpgrade.Type.SpreadAdd:
                spreadDegrees += upgrade.amount; break;
        }
    }
}
