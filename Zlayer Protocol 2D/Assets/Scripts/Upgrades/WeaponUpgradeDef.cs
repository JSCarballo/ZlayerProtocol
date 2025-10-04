using UnityEngine;

[System.Serializable]
public struct WeaponUpgrade
{
    public enum Type { DamageMult, FireRateMult, SpeedMult, ExtraProjectiles, Piercing, Bounce, SpreadAdd }

    [Header("Tipo y magnitud")]
    public Type type;
    public float amount; // Damage/FireRate/Speed=> multiplicador (1.2 = +20%), Extras=> enteros, Spread=> grados
}
