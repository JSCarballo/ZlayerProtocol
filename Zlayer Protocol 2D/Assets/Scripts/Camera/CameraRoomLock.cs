// Assets/Scripts/Camera/CameraRoomLock.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// Bloquea la cámara a los límites de una sala (Bounds) y permite
/// transiciones suaves o modo "follow" (seguir al jugador) con clamp al room.
/// </summary>
[DefaultExecutionOrder(-200)]
public class CameraRoomLock : MonoBehaviour
{
    public static CameraRoomLock Instance { get; private set; }

    [Header("Referencia de Cámara")]
    public Camera targetCamera;
    public bool autoFindMainCamera = true;

    [Header("Ajuste de encuadre (fit)")]
    public float fitPadding = 0.5f;
    public float minOrthoSize = 3f;
    public float maxOrthoSize = 50f;

    [Header("Plano Z")]
    public bool snapZToFixed = true;
    public float cameraZ = -10f;

    [Header("Transición")]
    public bool useUnscaledTime = false;

    [Header("Follow (solo para pisos tipo arena/1B)")]
    [Tooltip("Tamaño ortográfico por defecto cuando se activa el follow si no se especifica override.")]
    public float defaultFollowOrthoSize = 6f;

    public Bounds CurrentRoomBounds { get; private set; }
    public Vector3 LastSnapPosition { get; private set; }

    // --- Follow state ---
    Transform followTarget;
    Bounds followClampBounds;
    bool followActive = false;
    float followOrthoSize = -1f;

    public bool IsFollowActive => followActive;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!targetCamera && autoFindMainCamera) targetCamera = Camera.main;
        if (!targetCamera)
            Debug.LogWarning("[CameraRoomLock] No hay cámara asignada. Asigna targetCamera en el inspector.");
    }

    public void SnapToRoom(Bounds room)
    {
        if (!EnsureCamera()) return;

        CurrentRoomBounds = room;

        var camPos = new Vector3(room.center.x, room.center.y,
                                 snapZToFixed ? cameraZ : targetCamera.transform.position.z);
        targetCamera.transform.position = camPos;
        LastSnapPosition = camPos;

        float size = ComputeFitOrthoSize(room, targetCamera);
        targetCamera.orthographicSize = size;
    }

    public IEnumerator PanToRoom(Bounds room, float duration, AnimationCurve curve, bool resizeDuring)
    {
        if (!EnsureCamera()) yield break;

        if (duration <= 0f)
        {
            SnapToRoom(room);
            yield break;
        }

        CurrentRoomBounds = room;

        Vector3 startPos = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        Vector3 endPos = new Vector3(room.center.x, room.center.y,
                                     snapZToFixed ? cameraZ : startPos.z);
        float endSize = resizeDuring ? ComputeFitOrthoSize(room, targetCamera) : startSize;

        AnimationCurve useCurve = curve ?? AnimationCurve.Linear(0, 0, 1, 1);

        float t = 0f;
        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / duration);
            float e = useCurve.Evaluate(k);

            Vector3 p = Vector3.Lerp(startPos, endPos, e);
            targetCamera.transform.position = p;

            if (resizeDuring)
            {
                float s = Mathf.Lerp(startSize, endSize, e);
                targetCamera.orthographicSize = s;
            }

            yield return null;
        }

        targetCamera.transform.position = endPos;
        targetCamera.orthographicSize = endSize;
        LastSnapPosition = endPos;
    }

    /// <summary>
    /// Activa el modo follow: la cámara sigue a 'target' y se clampa a 'roomBounds'.
    /// </summary>
    /// <param name="target">Transform del jugador</param>
    /// <param name="roomBounds">Bounds de la sala/arena para clamping</param>
    /// <param name="sizeOverride">Si > 0, fija ese orthoSize; si <=0 usa defaultFollowOrthoSize</param>
    public void EnterFollowMode(Transform target, Bounds roomBounds, float sizeOverride = -1f)
    {
        if (!EnsureCamera()) return;

        followTarget = target;
        followClampBounds = roomBounds;
        followActive = true;

        // Tamaño: usar override si es válido; si no, defaultFollowOrthoSize
        float targetSize = (sizeOverride > 0f) ? sizeOverride : defaultFollowOrthoSize;
        targetCamera.orthographicSize = Mathf.Clamp(targetSize, minOrthoSize, maxOrthoSize);

        // Posicionar de inmediato una vez
        LateUpdate(); // fuerza un update inmediato para evitar salto visual
    }

    /// <summary>Desactiva el modo follow y devuelve el control al lock por salas.</summary>
    public void ExitFollowMode()
    {
        followActive = false;
        followTarget = null;
    }

    void LateUpdate()
    {
        if (!followActive || !EnsureCamera() || followTarget == null) return;

        // Clampear el centro de la cámara para no mostrar fuera del room
        float aspect = (targetCamera.pixelHeight > 0) ? ((float)targetCamera.pixelWidth / targetCamera.pixelHeight) : (16f / 9f);
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * Mathf.Max(0.0001f, aspect);

        float minX = followClampBounds.min.x + halfW;
        float maxX = followClampBounds.max.x - halfW;
        float minY = followClampBounds.min.y + halfH;
        float maxY = followClampBounds.max.y - halfH;

        Vector3 desired = followTarget.position;
        float clampedX = Mathf.Clamp(desired.x, minX, maxX);
        float clampedY = Mathf.Clamp(desired.y, minY, maxY);

        Vector3 camPos = new Vector3(clampedX, clampedY,
            snapZToFixed ? cameraZ : targetCamera.transform.position.z);

        targetCamera.transform.position = camPos;
        LastSnapPosition = camPos;
        CurrentRoomBounds = followClampBounds;
    }

    float ComputeFitOrthoSize(Bounds room, Camera cam)
    {
        if (!cam.orthographic) return cam.orthographicSize;

        float aspect = (cam.pixelHeight > 0) ? ((float)cam.pixelWidth / cam.pixelHeight) : (16f / 9f);

        float halfH = room.extents.y + fitPadding;
        float halfW = room.extents.x + fitPadding;

        float needed = Mathf.Max(halfH, halfW / Mathf.Max(0.0001f, aspect));
        return Mathf.Clamp(needed, minOrthoSize, maxOrthoSize);
    }

    bool EnsureCamera()
    {
        if (!targetCamera && autoFindMainCamera) targetCamera = Camera.main;
        if (!targetCamera)
        {
            Debug.LogWarning("[CameraRoomLock] Sin cámara. Asigna una Camera en 'targetCamera'.");
            return false;
        }
        return true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (CurrentRoomBounds.size != Vector3.zero)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawCube(CurrentRoomBounds.center, CurrentRoomBounds.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(CurrentRoomBounds.center, CurrentRoomBounds.size);
        }
    }
#endif
}
