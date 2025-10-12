using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Parpadeo (alpha) de múltiples SpriteRenderers durante un tiempo.
/// Auto-colecta SRs en hijos (opcional) y evita fogonazos u otros por nombre.
public class SpriteBlinker2D : MonoBehaviour
{
    [Header("Renderers a parpadear (si vacío y autoCollect=true, busca en hijos)")]
    [SerializeField] private List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    [Header("Auto-colección")]
    [SerializeField] private bool autoCollectChildren = true;
    [SerializeField] private string[] excludeNameContains = new[] { "Flash" };

    [Header("Parpadeo")]
    [SerializeField, Tooltip("Alpha cuando está 'apagado'")]
    private float blinkAlpha = 0.25f;
    [SerializeField, Tooltip("Veces por segundo que conmuta")]
    private float blinkHz = 12f;

    Color[] originalColors;
    bool isBlinking;

    void Awake()
    {
        if (autoCollectChildren && (renderers == null || renderers.Count == 0))
        {
            renderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>(true));
            if (excludeNameContains != null && excludeNameContains.Length > 0)
            {
                renderers.RemoveAll(sr =>
                {
                    if (!sr) return true;
                    string n = sr.gameObject.name.ToLowerInvariant();
                    foreach (var ex in excludeNameContains)
                        if (!string.IsNullOrEmpty(ex) && n.Contains(ex.ToLowerInvariant()))
                            return true;
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
            var c = sr.color; c.a = a; sr.color = c;
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
