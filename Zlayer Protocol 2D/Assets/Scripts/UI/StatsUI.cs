using UnityEngine;
using UnityEngine.UI;

/// HUD estilo TBOI que MUESTRA lo que realmente usa el juego: los valores
/// que llegan al Projectile2D (damage, speed, flags, bounces, y rof reportado).
/// No lee PlayerWeaponStats.
public class StatsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text titleText;        // opcional
    [SerializeField] private Text damageText;       // DMG
    [SerializeField] private Text fireRateText;     // ROF (/s)
    [SerializeField] private Text bulletSpeedText;  // SPD
    [SerializeField] private Text piercingText;     // ON/OFF
    [SerializeField] private Text bouncingText;     // ON/OFF
    [SerializeField] private Text maxBouncesText;   // int

    [Header("Formato")]
    [SerializeField] private string titleString = "STATS";
    [SerializeField] private string fmtFloat = "0.00";
    [SerializeField] private string onStr = "ON";
    [SerializeField] private string offStr = "OFF";

    [Header("Respaldo")]
    [SerializeField] private float pollInterval = 0.25f;
    float t;

    void Awake()
    {
        if (titleText) titleText.text = titleString;
    }

    void OnEnable()
    {
        Projectile2D.OnHUDSnapshotChanged += OnSnapshot;
        // Pintar lo último conocido, si existe
        if (Projectile2D.HasHUD) OnSnapshot(Projectile2D.GetLastHUD());
        else ClearUI();
    }

    void OnDisable()
    {
        Projectile2D.OnHUDSnapshotChanged -= OnSnapshot;
    }

    void Update()
    {
        // Polling de respaldo (por si algo se perdiera al recargar escenas)
        t += Time.deltaTime;
        if (t >= pollInterval)
        {
            t = 0f;
            if (Projectile2D.HasHUD) OnSnapshot(Projectile2D.GetLastHUD());
        }
    }

    void OnSnapshot(Projectile2D.HUDSnapshot s)
    {
        Set(damageText, $"DMG: {s.damage.ToString(fmtFloat)}");
        Set(fireRateText, s.rof > 0f ? $"ROF: {s.rof.ToString(fmtFloat)}/s" : "ROF: -");
        Set(bulletSpeedText, $"SPD: {s.speed.ToString(fmtFloat)}");
        Set(piercingText, $"Piercing: {(s.piercing ? onStr : offStr)}");
        Set(bouncingText, $"Bouncing: {(s.bouncing ? onStr : offStr)}");
        Set(maxBouncesText, $"Max Bounces: {s.maxBounces}");
    }

    void ClearUI()
    {
        Set(damageText, "DMG: -");
        Set(fireRateText, "ROF: -");
        Set(bulletSpeedText, "SPD: -");
        Set(piercingText, "Piercing: -");
        Set(bouncingText, "Bouncing: -");
        Set(maxBouncesText, "Max Bounces: -");
    }

    void Set(Text t, string s) { if (t) t.text = s; }
}
