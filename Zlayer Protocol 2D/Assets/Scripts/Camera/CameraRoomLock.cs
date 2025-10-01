// Assets/Scripts/Camera/CameraRoomLock.cs
using UnityEngine;
using System.Collections;

public class CameraRoomLock : MonoBehaviour
{
    public static CameraRoomLock Instance { get; private set; }

    [Header("Refs")]
    public Camera cam;

    [Header("Framing")]
    public float margin = 0.5f;

    [Header("Transition")]
    public float defaultDuration = 0.6f; // tiempo por defecto del blend
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool useUnscaledTime = true;  // ignora Time.timeScale en la transición

    private Coroutine _transition;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!cam) cam = Camera.main;
    }

    // Salto instantáneo (tu método actual)
    public void SnapToRoom(Bounds roomBounds)
    {
        var target = ComputeTarget(roomBounds);
        cam.transform.position = target.pos;
        cam.orthographicSize = target.size;
    }

    // Transición con duración por defecto
    public void GoToRoom(Bounds roomBounds) => GoToRoom(roomBounds, defaultDuration);

    // Transición con duración custom
    public void GoToRoom(Bounds roomBounds, float duration)
    {
        if (duration <= 0f) { SnapToRoom(roomBounds); return; }
        if (_transition != null) StopCoroutine(_transition);
        _transition = StartCoroutine(DoTransition(roomBounds, duration));
    }

    private IEnumerator DoTransition(Bounds roomBounds, float duration)
    {
        var target = ComputeTarget(roomBounds);

        Vector3 startPos = cam.transform.position;
        float startSize = cam.orthographicSize;

        float t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / duration;
            float k = easing.Evaluate(Mathf.Clamp01(t));
            cam.transform.position = Vector3.Lerp(startPos, target.pos, k);
            cam.orthographicSize = Mathf.Lerp(startSize, target.size, k);
            yield return null;
        }

        cam.transform.position = target.pos;
        cam.orthographicSize = target.size;
        _transition = null;
    }

    private (Vector3 pos, float size) ComputeTarget(Bounds b)
    {
        Vector3 c = b.center; c.z = cam.transform.position.z;

        float halfH = b.extents.y + margin;
        float halfW = b.extents.x + margin;
        float size = Mathf.Max(halfH, halfW / cam.aspect);

        return (c, size);
    }
}
