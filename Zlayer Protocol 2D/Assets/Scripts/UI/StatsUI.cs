using UnityEngine;
using UnityEngine.UI;
using System.Text;

/// UI compacta que muestra las stats del arma del Player y se actualiza
/// al instante cuando cambian (OnStatsChanged), con polling como respaldo.
///
/// Arrástrale el Text en el inspector. Si no asignas PlayerWeaponStats,
/// se auto-detecta en runtime.
public class StatsUI : MonoBehaviour
{
    [Header("Fuentes")]
    [SerializeField] private PlayerWeaponStats playerWeaponStats;  // se autodescubre si está vacío
    [SerializeField] private Health playerHealth;                  // opcional (solo si quieres mostrar HP)

    [Header("UI")]
    [SerializeField] private Text allStatsText;

    [Header("Etiquetas")]
    [SerializeField] private string labelHP = "HP";
    [SerializeField] private string labelDamage = "DMG";
    [SerializeField] private string labelFireRate = "ROF";
    [SerializeField] private string labelBulletSpeed = "SPD";
    [SerializeField] private string labelPiercing = "Piercing";
    [SerializeField] private string labelBouncing = "Bouncing";
    [SerializeField] private string labelMaxBounces = "Max Bounces";

    [Header("Formato")]
    [SerializeField] private string onStr = "ON";
    [SerializeField] private string offStr = "OFF";
    [SerializeField] private string floatFormat = "0.00";

    [Header("Opciones")]
    [SerializeField] private bool showHP = false;   // activa si quieres ver HP numérico

    [Header("Respaldo (polling)")]
    [SerializeField] private float pollInterval = 0.25f;
    private float pollT;

    void Awake()
    {
        AutoFindSources();
        if (!allStatsText) Debug.LogWarning("[StatsUI] Asigna el Text de salida en el inspector.");
    }

    void OnEnable()
    {
        Subscribe(true);
        ForceRefresh();

        // Backup: refrescar ante cualquier pickup por si algo externo no dispara eventos.
        WeaponUpgradePickup.AnyPicked += OnAnyPickup;
    }

    void OnDisable()
    {
        Subscribe(false);
        WeaponUpgradePickup.AnyPicked -= OnAnyPickup;
    }

    void Update()
    {
        // Respaldo: si el Player aún no existía/estaba desactivado
        pollT += Time.deltaTime;
        if (pollT >= pollInterval)
        {
            pollT = 0f;
            if (!playerWeaponStats) AutoFindSources();
            if (showHP && !playerHealth) AutoFindHealth();
            Refresh();
        }
    }

    void OnAnyPickup(WeaponUpgradePickup _)
    {
        // Si por algún motivo OnStatsChanged no llegó, forzar un refresh
        Refresh();
    }

    void AutoFindSources()
    {
        if (!playerWeaponStats) playerWeaponStats = FindObjectOfType<PlayerWeaponStats>(true);
        AutoFindHealth();
    }

    void AutoFindHealth()
    {
        if (!showHP || playerHealth) return;
        playerHealth = FindObjectOfType<Health>(true);
    }

    void Subscribe(bool on)
    {
        if (on)
        {
            if (playerWeaponStats != null) playerWeaponStats.OnStatsChanged += Refresh;
        }
        else
        {
            if (playerWeaponStats != null) playerWeaponStats.OnStatsChanged -= Refresh;
        }
    }

    public void SetPlayerWeaponStats(PlayerWeaponStats s)
    {
        if (playerWeaponStats != null) playerWeaponStats.OnStatsChanged -= Refresh;
        playerWeaponStats = s;
        if (playerWeaponStats != null) playerWeaponStats.OnStatsChanged += Refresh;
        Refresh();
    }

    public void SetPlayerHealth(Health h)
    {
        playerHealth = h;
        Refresh();
    }

    public void ForceRefresh() => Refresh();

    public void Refresh()
    {
        if (!allStatsText) return;

        if (!playerWeaponStats)
        {
            allStatsText.text = "No weapon stats.";
            return;
        }

        var sb = new StringBuilder(192);

        // (Opcional) HP si deseas verlo aquí (si no, deja showHP=false)
        if (showHP && playerHealth)
        {
            // Intento obtener campos/props comunes por reflexión (compatible con tu Health)
            int hp = GetInt(playerHealth, "CurrentHP", "currentHP", "HP", "hp", "current", "currentHealth");
            int hpMax = GetInt(playerHealth, "MaxHP", "maxHP", "Max", "max", "maxHealth");
            if (hp >= 0 && hpMax >= 0) sb.AppendLine($"{labelHP}: {hp}/{hpMax}");
        }

        // Stats del arma (PlayerWeaponStats)
        sb.AppendLine($"{labelDamage}: {playerWeaponStats.damage.ToString(floatFormat)}");
        sb.AppendLine($"{labelFireRate}: {playerWeaponStats.fireRate.ToString(floatFormat)}/s");
        sb.AppendLine($"{labelBulletSpeed}: {playerWeaponStats.bulletSpeed.ToString(floatFormat)}");
        sb.AppendLine($"{labelPiercing}: {(playerWeaponStats.piercing ? onStr : offStr)}");
        sb.AppendLine($"{labelBouncing}: {(playerWeaponStats.bouncing ? onStr : offStr)}");
        sb.AppendLine($"{labelMaxBounces}: {playerWeaponStats.maxBounces}");

        allStatsText.text = sb.ToString();
    }

    int GetInt(object obj, params string[] names)
    {
        var t = obj.GetType();
        foreach (var n in names)
        {
            var p = t.GetProperty(n);
            if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(obj);
            var f = t.GetField(n);
            if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(obj);
        }
        return -1;
    }
}
