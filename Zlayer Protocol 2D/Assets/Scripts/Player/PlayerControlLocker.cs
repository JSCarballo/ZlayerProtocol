using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// Bloquea TODOS los scripts del Player (y opcionalmente de hijos) salvo una lista blanca,
/// guarda su estado y los restaura. Incluye un auto-scan por nombre para atrapar
/// scripts de movimiento/disparo aunque no los hayas asignado manualmente.
public class PlayerControlLocker : MonoBehaviour
{
    [Header("Opcional: desactivar explícitos")]
    [Tooltip("Scripts que quieres forzar a OFF durante el lock (ej. PlayerMovement, FourWayShooter)")]
    public Behaviour[] toDisableExplicit;

    [Header("Auto-scan")]
    [Tooltip("Buscar por nombre componentes que contengan estas palabras y desactivarlos")]
    public string[] nameKeywords = new[] { "move", "movement", "controller", "input", "shoot", "fire", "weapon", "aim", "dash" };

    [Tooltip("También escanear scripts en hijos (armas en child, etc.)")]
    public bool includeChildren = true;

    [Header("Físicas")]
    [Tooltip("Poner velocidad del RB a cero al bloquear")]
    public bool zeroVelocityOnLock = true;

    readonly List<Behaviour> disabledNow = new();
    int lockCount = 0;

    public void HardLock()
    {
        lockCount++;
        if (lockCount > 1) { ApplyRBZero(); return; }

        disabledNow.Clear();

        // 1) desactivar explícitos
        foreach (var b in toDisableExplicit)
        {
            TryDisable(b);
        }

        // 2) auto-scan por nombre
        var scope = includeChildren ? GetComponentsInChildren<Behaviour>(true) : GetComponents<Behaviour>();
        foreach (var beh in scope)
        {
            if (beh == null || !beh.enabled) continue;
            if (IsWhitelisted(beh)) continue;

            var n = beh.GetType().Name.ToLowerInvariant();
            if (nameKeywords.Any(k => n.Contains(k)))
                TryDisable(beh);
        }

        ApplyRBZero();
    }

    public void HardUnlock()
    {
        if (lockCount == 0) return;
        lockCount--;
        if (lockCount > 0) return;

        // reactivar en orden inverso (por si dependencias)
        for (int i = disabledNow.Count - 1; i >= 0; --i)
        {
            var b = disabledNow[i];
            if (b != null) b.enabled = true;
        }
        disabledNow.Clear();
    }

    void TryDisable(Behaviour b)
    {
        if (b == null || !b.enabled) return;
        if (IsWhitelisted(b)) return;
        b.enabled = false;
        disabledNow.Add(b);
    }

    bool IsWhitelisted(Behaviour b)
    {
        // No desactivar el propio locker, ni render/anims/vida básicos
        var t = b.GetType();
        if (t == typeof(PlayerControlLocker)) return true;
        if (b is SpriteRenderer) return true;
        if (b is Animator) return true;
        // Permite lógica de vida/daño si tu clase se llama así:
        var n = t.Name.ToLowerInvariant();
        if (n.Contains("health") || n.Contains("flash") || n.Contains("hurt")) return true;
        return false;
    }

    void ApplyRBZero()
    {
        if (!zeroVelocityOnLock) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;
    }
}
