// Assets/Scripts/Camera/CameraRoomLock.cs
using UnityEngine;
using System.Collections;

public class CameraRoomLock : MonoBehaviour
{
    public static CameraRoomLock Instance { get; private set; }

    [Header("References")]
    public Camera cam;

    [Header("Framing")]
    [Tooltip("Borde extra alrededor de la sala")]
    public float margin = 0.5f;

    [Header("Defaults")]
    [Tooltip("Curva por defecto de la transición (0..1)")]
    public AnimationCurve defaultCurve = null;

    Coroutine currentTransition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!cam) cam = Camera.main;
        if (defaultCurve == null) defaultCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    public void SnapToRoom(Bounds roomBounds)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
        ApplyFrame(roomBounds);
    }

    public Coroutine TransitionToRoom(Bounds roomBounds, float duration, AnimationCurve curve = null, bool resize = true)
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(PanToRoomRoutine(roomBounds, duration, curve ?? defaultCurve, resize));
        return currentTransition;
    }

    public IEnumerator PanToRoom(Bounds roomBounds, float duration, AnimationCurve curve = null, bool resize = true)
    {
        yield return PanToRoomRoutine(roomBounds, duration, curve ?? defaultCurve, resize);
    }

    IEnumerator PanToRoomRoutine(Bounds roomBounds, float duration, AnimationCurve curve, bool resize)
    {
        if (duration <= 0f)
        {
            ApplyFrame(roomBounds);
            yield break;
        }

        Vector3 startPos = cam.transform.position;
        float startSize = cam.orthographicSize;

        // Objetivo
        Vector3 targetPos = roomBounds.center; targetPos.z = startPos.z;
        float targetSize = ComputeOrthoSize(roomBounds);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = curve != null ? curve.Evaluate(k) : k;

            cam.transform.position = Vector3.Lerp(startPos, targetPos, e);
            if (resize) cam.orthographicSize = Mathf.Lerp(startSize, targetSize, e);

            yield return null;
        }

        cam.transform.position = targetPos;
        if (resize) cam.orthographicSize = targetSize;

        currentTransition = null;
    }

    void ApplyFrame(Bounds b)
    {
        Vector3 c = b.center;
        c.z = cam.transform.position.z;
        cam.transform.position = c;
        cam.orthographicSize = ComputeOrthoSize(b);
    }

    float ComputeOrthoSize(Bounds b)
    {
        float halfH = b.extents.y + margin;
        float halfW = b.extents.x + margin;
        float byW = halfW / cam.aspect;
        return Mathf.Max(halfH, byW);
    }
}
