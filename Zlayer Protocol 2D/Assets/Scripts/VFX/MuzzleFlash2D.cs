using System.Collections;
using UnityEngine;

/// Controla 4 fogonazos (uno por dirección). Enciende el correcto durante showSeconds al disparar.
/// Pensado para sprites 40x40 y PPU=40. Puedes asignar los SpriteRenderer que están en cada muzzle.
/// Si prefieres, puedes colgar el SpriteRenderer directamente en el muzzle (FlashX) o como hijo.
public class MuzzleFlash2D : MonoBehaviour
{
    [Header("SpriteRenderers de cada dirección")]
    [SerializeField] private SpriteRenderer flashRight;
    [SerializeField] private SpriteRenderer flashLeft;
    [SerializeField] private SpriteRenderer flashUp;
    [SerializeField] private SpriteRenderer flashDown;

    [Header("Duración del flash")]
    [SerializeField] private float showSeconds = 0.06f;

    [Header("Aleatoriedad (sutil)")]
    [SerializeField] private bool randomize = true;
    [SerializeField, Tooltip("Rotación máxima aleatoria (±grados)")]
    private float randomRotMax = 8f;
    [SerializeField, Tooltip("Escala mínima y máxima aleatoria")]
    private Vector2 randomScaleRange = new Vector2(1.0f, 1.15f);

    [Header("Pixel Perfect (opcional)")]
    [SerializeField] private bool pixelSnapLocal = true;
    [SerializeField] private float pixelsPerUnit = 40f;

    // Estados internos para restaurar transform del SR
    private Vector3 baseScaleRight, baseScaleLeft, baseScaleUp, baseScaleDown;
    private Quaternion baseRotRight, baseRotLeft, baseRotUp, baseRotDown;

    private Coroutine crRight, crLeft, crUp, crDown;

    void Awake()
    {
        CacheBaseTransforms();
        DisableAll();
    }

    void CacheBaseTransforms()
    {
        if (flashRight) { baseScaleRight = flashRight.transform.localScale; baseRotRight = flashRight.transform.localRotation; }
        if (flashLeft) { baseScaleLeft = flashLeft.transform.localScale; baseRotLeft = flashLeft.transform.localRotation; }
        if (flashUp) { baseScaleUp = flashUp.transform.localScale; baseRotUp = flashUp.transform.localRotation; }
        if (flashDown) { baseScaleDown = flashDown.transform.localScale; baseRotDown = flashDown.transform.localRotation; }
    }

    public void Show(Vector2 dir)
    {
        // Convertir a cardinal exacto
        Vector2 d = ToCardinal(dir);
        // Apagar todos antes de encender el correcto
        DisableAll();

        if (d == Vector2.right && flashRight)
        {
            if (crRight != null) StopCoroutine(crRight);
            PrepareSR(flashRight, baseScaleRight, baseRotRight);
            flashRight.enabled = true;
            crRight = StartCoroutine(HideAfter(flashRight, showSeconds));
        }
        else if (d == Vector2.left && flashLeft)
        {
            if (crLeft != null) StopCoroutine(crLeft);
            PrepareSR(flashLeft, baseScaleLeft, baseRotLeft);
            flashLeft.enabled = true;
            crLeft = StartCoroutine(HideAfter(flashLeft, showSeconds));
        }
        else if (d == Vector2.up && flashUp)
        {
            if (crUp != null) StopCoroutine(crUp);
            PrepareSR(flashUp, baseScaleUp, baseRotUp);
            flashUp.enabled = true;
            crUp = StartCoroutine(HideAfter(flashUp, showSeconds));
        }
        else if (d == Vector2.down && flashDown)
        {
            if (crDown != null) StopCoroutine(crDown);
            PrepareSR(flashDown, baseScaleDown, baseRotDown);
            flashDown.enabled = true;
            crDown = StartCoroutine(HideAfter(flashDown, showSeconds));
        }
    }

    Vector2 ToCardinal(Vector2 v)
    {
        if (v == Vector2.zero) return Vector2.right;
        return (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            ? new Vector2(Mathf.Sign(v.x), 0f)
            : new Vector2(0f, Mathf.Sign(v.y));
    }

    void PrepareSR(SpriteRenderer sr, Vector3 baseScale, Quaternion baseRot)
    {
        // Restaurar base
        sr.transform.localScale = baseScale;
        sr.transform.localRotation = baseRot;

        // Aleatoriedad sutil
        if (randomize)
        {
            float s = Random.Range(randomScaleRange.x, randomScaleRange.y);
            sr.transform.localScale = baseScale * s;

            float r = Random.Range(-randomRotMax, randomRotMax);
            sr.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, r);
        }

        // Pixel snap local
        if (pixelSnapLocal && pixelsPerUnit > 0f)
        {
            Vector3 lp = sr.transform.localPosition;
            lp.x = Mathf.Round(lp.x * pixelsPerUnit) / pixelsPerUnit;
            lp.y = Mathf.Round(lp.y * pixelsPerUnit) / pixelsPerUnit;
            sr.transform.localPosition = lp;
        }
    }

    IEnumerator HideAfter(SpriteRenderer sr, float secs)
    {
        yield return new WaitForSeconds(secs);
        if (sr) sr.enabled = false;
    }

    void DisableAll()
    {
        if (flashRight) flashRight.enabled = false;
        if (flashLeft) flashLeft.enabled = false;
        if (flashUp) flashUp.enabled = false;
        if (flashDown) flashDown.enabled = false;
    }
}
