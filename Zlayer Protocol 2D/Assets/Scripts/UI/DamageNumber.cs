using UnityEngine;
using TMPro;
using UnityEngine.UI; // por si usas CanvasScaler

public class DamageNumber : MonoBehaviour
{
    public enum Style { Normal, Boss }

    [Header("Refs")]
    public TMP_Text text;
    [Tooltip("Canvas que contiene este número. Si se deja vacío, se buscará en padres.")]
    public Canvas targetCanvas;

    [Header("Sorting (render order)")]
    [Tooltip("Si está activo, se forzará el orden de render del Canvas para que los daños queden por encima.")]
    public bool overrideCanvasSorting = true;
    [Tooltip("Sorting Layer a usar. Déjalo en \"UI\" u otra capa encima de floor/characters.")]
    public string sortingLayerName = "UI";
    [Tooltip("Orden dentro de la capa. Usa un valor alto para ir por encima del overlay.")]
    public int sortingOrder = 500;

    [Header("Animación base")]
    public float lifetime = 0.8f;
    public Vector2 move = new Vector2(0f, 70f);        // px que sube (Canvas en px)
    public Vector2 randomJitter = new Vector2(16f, 6f);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.9f, 1, 1.15f);

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color bossColor = new Color(1f, 0.35f, 0.2f, 1f);

    [Header("Estilo Boss (extra)")]
    public float bossScaleMultiplier = 1.35f;
    public float bossShakeAmplitude = 8f;  // px
    public float bossShakeFrequency = 35f; // Hz

    // Estado
    Vector2 startPos;
    float t;
    Style style;
    float styleScale = 1f;
    float shakeAmp = 0f;
    float shakeFreq = 0f;
    RectTransform rt;

    void Awake()
    {
        // Cacheos
        if (!text) text = GetComponentInChildren<TMP_Text>(true);
        rt = GetComponent<RectTransform>();
        if (!rt) rt = gameObject.AddComponent<RectTransform>();

        // Sorting defensivo en Awake
        EnsureCanvasAndSorting();
    }

    void OnEnable()
    {
        // Si el objeto se recicla o reparenta al reiniciar escena, re-aplica sorting
        EnsureCanvasAndSorting();
    }

    void EnsureCanvasAndSorting()
    {
        if (!targetCanvas)
            targetCanvas = GetComponentInParent<Canvas>(true);

        if (!targetCanvas)
        {
            Debug.LogWarning("[DamageNumber] No se encontró Canvas padre. El sorting no podrá forzarse.", this);
            return;
        }

        // Solo Canvas World Space o Screen Space - Camera respetan sorting de capa/orden.
        // Para Screen Space - Overlay esto no aplica (ya va sobre sprites).
        if (overrideCanvasSorting)
        {
            targetCanvas.overrideSorting = true;
            if (!string.IsNullOrEmpty(sortingLayerName))
                targetCanvas.sortingLayerName = sortingLayerName;
            targetCanvas.sortingOrder = sortingOrder;
        }

        // Asegura que no bloquee raycasts (innecesario en la mayoría de casos)
        var grp = targetCanvas.GetComponent<GraphicRaycaster>();
        if (grp) grp.ignoreReversedGraphics = true;
    }

    public void Init(float amount, Style style)
    {
        this.style = style;

        if (!text) text = GetComponentInChildren<TMP_Text>(true);
        if (text)
        {
            text.text = Mathf.RoundToInt(amount).ToString();
            // Asegura que el material no esté con alpha 0 por estados previos
            var c = text.color;
            c.a = 1f;
            text.color = c;
            text.enabled = true;
        }
        else
        {
            Debug.LogWarning("[DamageNumber] TMP_Text no asignado/ausente.", this);
        }

        // Config por estilo
        if (style == Style.Boss)
        {
            styleScale = bossScaleMultiplier;
            if (text) text.color = bossColor;
            shakeAmp = bossShakeAmplitude;
            shakeFreq = bossShakeFrequency;
        }
        else
        {
            styleScale = 1f;
            if (text) text.color = normalColor;
            shakeAmp = 0f;
            shakeFreq = 0f;
        }

        // Jitter inicial y reset de tiempo
        if (!rt) rt = GetComponent<RectTransform>();
        startPos = (rt ? rt.anchoredPosition : Vector2.zero) + new Vector2(
            Random.Range(-randomJitter.x, randomJitter.x),
            Random.Range(-randomJitter.y, randomJitter.y)
        );
        if (rt) rt.anchoredPosition = startPos;

        t = 0f;

        // Aplica sorting por si el spawner instanció y luego reasignó el parent
        EnsureCanvasAndSorting();
    }

    void Update()
    {
        // Si el TimeScale es 0 (pausa), dejamos la animación congelada por diseño.
        // Si quieres ignorar pausa, usa un delta independiente de timescale.
        t += Time.deltaTime;
        float k = (lifetime > 0f) ? Mathf.Clamp01(t / lifetime) : 1f;

        // Movimiento base + shake Boss
        Vector2 pos = startPos + move * k;
        if (shakeAmp > 0f && shakeFreq > 0f)
        {
            float sh = Mathf.Sin(t * shakeFreq * Mathf.PI * 2f) * shakeAmp;
            pos.x += sh;
        }
        if (rt) rt.anchoredPosition = pos;

        // Alpha y escala
        float a = alphaCurve.Evaluate(k);
        float s = scaleCurve.Evaluate(k) * styleScale;

        if (text && rt)
        {
            var c = text.color;
            c.a = a;
            text.color = c;
            rt.localScale = Vector3.one * s;
        }

        if (t >= lifetime)
        {
            // Reset defensivo por si usamos pooling más adelante
            if (text)
            {
                var c2 = text.color; c2.a = 1f; text.color = c2;
                text.enabled = false;
            }
            Destroy(gameObject);
        }
    }
}
