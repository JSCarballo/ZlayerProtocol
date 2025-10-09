using System.Collections.Generic;
using UnityEngine;

/// Muestra n�meros cuando PlayerHealth o EnemyHealth reciben da�o.
/// El n�mero se crea en mundo (no es hijo) y adopta la sorting layer + order m�s altos
/// de los SpriteRenderers del objetivo para asegurarse de verse por encima.
public class DamageNumberOnHealth : MonoBehaviour
{
    [Header("Apariencia")]
    public Color color = Color.white;
    public int fontSize = 32;
    [Tooltip("Offset en mundo desde el centro del objetivo")]
    public Vector3 worldOffset = new Vector3(0f, 0.6f, 0f);

    [Header("Animaci�n")]
    public float floatUpDistance = 0.8f;
    public float duration = 0.6f;

    // cache de sorting del objetivo
    string sortLayer = "Default";
    int sortOrderTop = 50;

    PlayerHealth ph;
    EnemyHealth eh;

    void OnEnable()
    {
        // Detectar health
        ph = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        eh = GetComponent<EnemyHealth>() ?? GetComponentInParent<EnemyHealth>();

        if (ph != null) ph.OnDamaged += HandleDamaged;
        if (eh != null) eh.OnDamaged += HandleDamaged;

        // Cachear capa/orden de SR m�s alto para render por encima
        CacheSortingInfo();
    }

    void OnDisable()
    {
        if (ph != null) ph.OnDamaged -= HandleDamaged;
        if (eh != null) eh.OnDamaged -= HandleDamaged;
    }

    void CacheSortingInfo()
    {
        int best = int.MinValue;
        string bestLayer = "Default";

        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        if (srs.Length == 0)
        {
            srs = (GetComponentInParent<Transform>()?.GetComponentsInChildren<SpriteRenderer>(true)) ?? new SpriteRenderer[0];
        }

        foreach (var sr in srs)
        {
            if (!sr) continue;
            if (sr.sortingOrder >= best)
            {
                best = sr.sortingOrder;
                bestLayer = sr.sortingLayerName;
            }
        }

        sortOrderTop = (best == int.MinValue) ? 50 : best + 20; // aseguramos estar encima
        sortLayer = bestLayer;
    }

    void HandleDamaged(int amount)
    {
        // Crear objeto del n�mero y posicionarlo en mundo
        var go = new GameObject("DamageNumber");
        go.transform.position = transform.position + worldOffset;

        // Texto simple con TextMesh
        var tm = go.AddComponent<TextMesh>();
        tm.text = "-" + amount.ToString();
        tm.color = color;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.05f; // escala base del TextMesh
        tm.fontSize = fontSize;

        // Asegurar visibilidad por sorting layer/order del objetivo
        var mr = go.GetComponent<MeshRenderer>();
        mr.sortingLayerName = sortLayer;
        mr.sortingOrder = sortOrderTop;

        // Animaci�n hacia arriba y autodestrucci�n
        go.AddComponent<DamageNumberFloat>().Init(duration, floatUpDistance);
    }

    // Componente interno para animar/destruir el n�mero
    class DamageNumberFloat : MonoBehaviour
    {
        float t, dur, up; Vector3 start;

        public void Init(float duration, float floatUp)
        {
            dur = Mathf.Max(0.05f, duration);
            up = floatUp;
            start = transform.position;
        }
        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            transform.position = start + Vector3.up * (up * k);
            if (t >= dur) Destroy(gameObject);
        }
    }
}
