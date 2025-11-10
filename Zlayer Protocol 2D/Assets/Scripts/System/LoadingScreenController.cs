using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Root del overlay (Canvas/Panel).")]
    public GameObject root;
    [Tooltip("CanvasGroup para fades.")]
    public CanvasGroup group;
    [Tooltip("Imagen a mostrar durante la carga (arte).")]
    public Image artwork;

    [Header("Fades")]
    public float fadeIn = 0.25f;
    public float fadeOut = 0.25f;

    [Header("Opcional")]
    [Tooltip("Bloquear raycasts mientras se muestra.")]
    public bool blockRaycasts = true;
    [Tooltip("Ignorar timescale durante fades (recomendado).")]
    public bool unscaledTime = true;

    public bool IsShowing { get; private set; }

    void Awake()
    {
        if (!root) root = gameObject;
        if (!group) group = GetComponentInChildren<CanvasGroup>();
        if (root) root.SetActive(false);
        if (group) { group.alpha = 0f; group.blocksRaycasts = false; }
        IsShowing = false;
    }

    public void SetArtwork(Sprite sprite)
    {
        if (artwork) artwork.sprite = sprite;
    }

    public void Show(Sprite sprite = null)
    {
        if (sprite) SetArtwork(sprite);
        if (!root) root = gameObject;
        root.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeTo(1f, fadeIn));
        IsShowing = true;
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutAndDisable());
    }

    IEnumerator FadeOutAndDisable()
    {
        yield return FadeTo(0f, fadeOut);
        if (root) root.SetActive(false);
        IsShowing = false;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        if (!group) yield break;

        group.blocksRaycasts = blockRaycasts;
        float t = 0f;
        float start = group.alpha;

        while (t < duration)
        {
            t += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }
        group.alpha = target;

        if (Mathf.Approximately(target, 0f))
        {
            group.blocksRaycasts = false;
        }
    }
}
