using UnityEngine;
using System.Collections;

/// <summary>
/// Bloquea la cámara a los límites de una sala (Bounds) y permite
/// transiciones suaves (pan + opcionalmente resize de orthoSize).
/// 
/// Uso típico:
///   CameraRoomLock.Instance.SnapToRoom(boundsInicial);
///   StartCoroutine(CameraRoomLock.Instance.PanToRoom(nuevosBounds, dur, curva, resize));
/// </summary>
[DefaultExecutionOrder(-200)]
public class CameraRoomLock : MonoBehaviour
{
    public static CameraRoomLock Instance { get; private set; }

    [Header("Referencia de Cámara")]
    [Tooltip("Si está vacío, intenta usar Camera.main al iniciar.")]
    public Camera targetCamera;
    public bool autoFindMainCamera = true;

    [Header("Ajuste de encuadre (fit)")]
    [Tooltip("Padding en unidades de mundo alrededor del room para el cálculo de orthoSize.")]
    public float fitPadding = 0.5f;
    [Tooltip("Límites de orthoSize para evitar zooms extremos.")]
    public float minOrthoSize = 3f;
    public float maxOrthoSize = 50f;

    [Header("Plano Z")]
    [Tooltip("Si true, fuerza el Z de la cámara a cameraZ al ajustar/snap.")]
    public bool snapZToFixed = true;
    public float cameraZ = -10f;

    [Header("Transición")]
    [Tooltip("Si true, usa Time.unscaledDeltaTime para transiciones (no afectadas por Time.timeScale).")]
    public bool useUnscaledTime = false;

    /// <summary>Últimos bounds de sala a los que la cámara fue ajustada.</summary>
    public Bounds CurrentRoomBounds { get; private set; }

    /// <summary>Última posición a la que se hizo Snap/Pan (centro de la vista).</summary>
    public Vector3 LastSnapPosition { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!targetCamera && autoFindMainCamera)
            targetCamera = Camera.main;

        if (!targetCamera)
            Debug.LogWarning("[CameraRoomLock] No hay cámara asignada. Asigna targetCamera en el inspector.");
    }

    /// <summary>
    /// Ajusta la cámara inmediatamente al centro del room y calcula orthoSize para encuadrar la sala completa.
    /// </summary>
    public void SnapToRoom(Bounds room)
    {
        if (!EnsureCamera()) return;

        CurrentRoomBounds = room;

        // Posición (centro de la sala)
        var camPos = new Vector3(room.center.x, room.center.y,
                                 snapZToFixed ? cameraZ : targetCamera.transform.position.z);

        targetCamera.transform.position = camPos;
        LastSnapPosition = camPos;

        // Tamaño ortográfico que encuadre la sala
        float size = ComputeFitOrthoSize(room, targetCamera);
        targetCamera.orthographicSize = size;
    }

    /// <summary>
    /// Pan suave hasta el room objetivo. Puede opcionalmente interpolar el orthoSize.
    /// </summary>
    /// <param name="room">Bounds de la sala destino</param>
    /// <param name="duration">Duración de la transición (seg). Si <= 0, hace Snap.</param>
    /// <param name="curve">Curva de easing. Si null, usa lineal.</param>
    /// <param name="resizeDuring">Si true, interpola el orthoSize hasta encuadrar el destino.</param>
    public IEnumerator PanToRoom(Bounds room, float duration, AnimationCurve curve, bool resizeDuring)
    {
        if (!EnsureCamera()) yield break;

        if (duration <= 0f)
        {
            SnapToRoom(room);
            yield break;
        }

        CurrentRoomBounds = room;

        // Estado inicial
        Vector3 startPos = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        // Estado final
        Vector3 endPos = new Vector3(room.center.x, room.center.y,
                                     snapZToFixed ? cameraZ : startPos.z);
        float endSize = resizeDuring ? ComputeFitOrthoSize(room, targetCamera) : startSize;

        // Curva/easing
        AnimationCurve useCurve = curve ?? AnimationCurve.Linear(0, 0, 1, 1);

        float t = 0f;
        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / duration);
            float e = useCurve.Evaluate(k);

            // Interpolación de posición y tamaño
            Vector3 p = Vector3.Lerp(startPos, endPos, e);
            targetCamera.transform.position = p;

            if (resizeDuring)
            {
                float s = Mathf.Lerp(startSize, endSize, e);
                targetCamera.orthographicSize = s;
            }

            yield return null;
        }

        // Final
        targetCamera.transform.position = endPos;
        targetCamera.orthographicSize = endSize;
        LastSnapPosition = endPos;
    }

    /// <summary>
    /// Calcula el orthoSize necesario para que el room completo quepa en pantalla (con padding), respetando los límites min/max.
    /// </summary>
    float ComputeFitOrthoSize(Bounds room, Camera cam)
    {
        if (!cam.orthographic)
        {
            // Este proyecto es 2D. Si fuera perspectiva, aquí tendrías que calcular FOV/Dist.
            // Por seguridad, retornamos el tamaño actual.
            return cam.orthographicSize;
        }

        float aspect = (cam.pixelHeight > 0) ? ((float)cam.pixelWidth / cam.pixelHeight) : (16f / 9f);

        // Extents con padding
        float halfH = room.extents.y + fitPadding;              // medio alto deseado
        float halfW = room.extents.x + fitPadding;              // medio ancho deseado

        // Para ortho: orthoSize = medio alto. Para cubrir ancho, orthoSize >= halfW / aspect
        float needed = Mathf.Max(halfH, halfW / Mathf.Max(0.0001f, aspect));
        return Mathf.Clamp(needed, minOrthoSize, maxOrthoSize);
    }

    bool EnsureCamera()
    {
        if (!targetCamera && autoFindMainCamera)
            targetCamera = Camera.main;

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
