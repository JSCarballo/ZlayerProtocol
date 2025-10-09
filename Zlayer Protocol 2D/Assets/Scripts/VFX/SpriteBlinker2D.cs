using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Hace parpadear (alpha) varios SpriteRenderers durante un tiempo.
/// - Auto-colección opcional de SpriteRenderers en hijos (excluye por nombre).
/// - Cambia solo el canal alpha del color (no materiales personalizados).
public class SpriteBlinker2D : MonoBehaviour
{
    [Header("Renderers a parpadear (si vacío y autoCollect=true, busca en hijos)")]
    [SerializeField] private List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    [Header("Auto-colección")]
    [SerializeField] private bool autoCollectChildren = true;
    [SerializeField] private string[] excludeNameContains = new[] { "Flash" }; // excluye fogonazos por defecto

    [Header("Parpadeo")]
    [SerializeField, Tooltip("Alpha cuando está 'apagado' el blink")]
    private float blinkAlpha = 0.25f;
    [SerializeField, Tooltip("Veces por segundo que conmuta encendido/apagado")]
    private float blinkHz = 12f;

    Color[] originalColors;
    bool isBlinking;

    void Awake()
    {
        if (autoCollectChildren && (renderers == null || renderers.Count == 0))
        {
            renderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>(true));
            // Filtro por nombre
            if (excludeNameContains != null && excludeNameContains.Length > 0)
            {
                renderers.RemoveAll(sr =>
                {
                    if (!sr) return true;
                    string n = sr.gameObject.name.ToLowerInvariant();
                    foreach (var ex in excludeNameContains)
                    {
                        if (!string.IsNullOrEmpty(ex) && n.Contains(ex.ToLowerInvariant()))
                            return true;
                    }
                    return false;
                });
            }
        }

        CacheOriginalColors();
    }

    void CacheOriginalColors()
    {
        originalColors = new Color[renderers.Count];
        for (int i = 0; i < renderers.Count; i++)
            originalColors[i] = renderers[i] ? renderers[i].color : Color.white;
    }

    public void SetRenderers(List<SpriteRenderer> list)
    {
        renderers = list ?? new List<SpriteRenderer>();
        CacheOriginalColors();
    }

    public Coroutine Play(float durationSeconds)
    {
        StopBlink();
        return StartCoroutine(BlinkRoutine(durationSeconds));
    }

    public void StopBlink()
    {
        if (isBlinking) StopAllCoroutines();
        RestoreColors();
        isBlinking = false;
    }

    IEnumerator BlinkRoutine(float duration)
    {
        isBlinking = true;
        float t = 0f;
        float period = Mathf.Max(0.01f, 1f / blinkHz);
        bool on = false;

        while (t < duration)
        {
            ApplyAlpha(on ? 1f : blinkAlpha);
            on = !on;

            // esperar un periodo de blink
            yield return new WaitForSeconds(period);
            t += period;
        }

        RestoreColors();
        isBlinking = false;
    }

    void ApplyAlpha(float a)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var sr = renderers[i];
            if (!sr) continue;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    void RestoreColors()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var sr = renderers[i];
            if (!sr) continue;
            sr.color = originalColors != null && i < originalColors.Length ? originalColors[i] : Color.white;
        }
    }
}
