using UnityEngine;
using System.Collections;

/// <summary>
/// Generic power‑up script. Assign a type and it will apply the effect to the
/// player on pickup for the specified duration. The enum defines supported
/// power‑ups such as explosive bullets, shield, speed boost, piercing and
/// magnetic pickup.
/// </summary>
public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        ExplosiveBullets,
        Shield,
        SpeedBoost,
        PiercingBullets,
        Magnet
    }

    public PowerUpType type;
    public float duration = 10f;
    public float magnitude = 1f;

    /// <summary>
    /// Called by PlayerController when the player collides with this power‑up.
    /// Starts a coroutine that applies the effect and cleans up afterwards.
    /// </summary>
    /// <param name="player">The player picking up the power‑up.</param>
    public void Apply(PlayerController player)
    {
        StartCoroutine(ApplyCoroutine(player));
    }

    private IEnumerator ApplyCoroutine(PlayerController player)
    {
        // Handle each power‑up type
        switch (type)
        {
            case PowerUpType.ExplosiveBullets:
                Gun gun = player.GetComponentInChildren<Gun>();
                if (gun != null)
                {
                    gun.explosiveBullets = true;
                    yield return new WaitForSeconds(duration);
                    gun.explosiveBullets = false;
                }
                break;
            case PowerUpType.PiercingBullets:
                Gun gunPiercing = player.GetComponentInChildren<Gun>();
                if (gunPiercing != null)
                {
                    gunPiercing.piercingBullets = true;
                    yield return new WaitForSeconds(duration);
                    gunPiercing.piercingBullets = false;
                }
                break;
            case PowerUpType.SpeedBoost:
                float originalSpeed = player.moveSpeed;
                player.moveSpeed = originalSpeed * (1f + magnitude);
                yield return new WaitForSeconds(duration);
                player.moveSpeed = originalSpeed;
                break;
            case PowerUpType.Shield:
                // Example shield implementation: you would need to implement a health
                // system and a shield component. This placeholder just waits.
                yield return new WaitForSeconds(duration);
                break;
            case PowerUpType.Magnet:
                // In a real game this would enable a magnetic pickup effect. Placeholder.
                yield return new WaitForSeconds(duration);
                break;
        }
        // Destroy the power‑up after applying
        Destroy(gameObject);
    }
}