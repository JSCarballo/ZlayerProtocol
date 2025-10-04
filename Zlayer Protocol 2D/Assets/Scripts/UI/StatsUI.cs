// Assets/Scripts/UI/StatsUI.cs
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    public Text linesText;
    PlayerWeaponStats stats;
    float findTimer;

    void Update()
    {
        if (!stats)
        {
            findTimer += Time.deltaTime;
            if (findTimer > 0.5f)
            {
                findTimer = 0f;
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p) stats = p.GetComponent<PlayerWeaponStats>();
            }
            return;
        }

        // Render compacto
        if (linesText)
        {
            linesText.text =
                $"DMG x{stats.damageMult:0.00}\n" +
                $"FRate x{stats.fireRateMult:0.00}\n" +
                $"BSpeed x{stats.projectileSpeedMult:0.00}\n" +
                $"+Proj {stats.extraProjectiles}\n" +
                $"Pierce {(stats.piercing ? "✓" : "✗")}\n" +
                $"Bounce {(stats.bouncing ? "✓" : "✗")}\n" +
                $"Spread {stats.spreadDegrees:0.#}°";
        }
    }
}
