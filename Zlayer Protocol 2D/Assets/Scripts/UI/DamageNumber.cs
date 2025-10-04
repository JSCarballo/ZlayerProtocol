// Assets/Scripts/UI/DamageNumber.cs
using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public enum Style { Normal, Boss }

    [Header("Refs")]
    public TMP_Text text;

    [Header("Animación base")]
    public float lifetime = 0.8f;
    public Vector2 move = new Vector2(0f, 70f);        // px que sube
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

    Vector2 startPos;
    float t;
    Style style;
    float styleScale = 1f;
    float shakeAmp = 0f;
    float shakeFreq = 0f;

    public void Init(float amount, Style style)
    {
        this.style = style;

        if (!text) text = GetComponentInChildren<TMP_Text>();
        if (text) text.text = Mathf.RoundToInt(amount).ToString();

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

        // Jitter inicial
        var rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition + new Vector2(
            Random.Range(-randomJitter.x, randomJitter.x),
            Random.Range(-randomJitter.y, randomJitter.y)
        );
        rt.anchoredPosition = startPos;
        t = 0f;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / lifetime);

        // Movimiento base
        Vector2 pos = startPos + move * k;

        // Shake estilo Boss
        if (shakeAmp > 0f && shakeFreq > 0f)
        {
            float sh = Mathf.Sin(t * shakeFreq * Mathf.PI * 2f) * shakeAmp;
            pos.x += sh;
        }

        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = pos;

        // Alpha y escala
        float a = alphaCurve.Evaluate(k);
        float s = scaleCurve.Evaluate(k) * styleScale;

        if (text)
        {
            var c = text.color; c.a = a; text.color = c;
            rt.localScale = Vector3.one * s;
        }

        if (t >= lifetime) Destroy(gameObject);
    }
}
