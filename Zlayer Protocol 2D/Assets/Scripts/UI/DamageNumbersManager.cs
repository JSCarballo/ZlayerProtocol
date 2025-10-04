// Assets/Scripts/UI/DamageNumbersManager.cs
using UnityEngine;

public class DamageNumbersManager : MonoBehaviour
{
    public static DamageNumbersManager Instance { get; private set; }

    [Header("UI Target")]
    public RectTransform canvas;          // Canvas raíz (RectTransform)
    public DamageNumber numberPrefab;     // Prefab del numerito (UI, con TMP)

    [Header("Offsets")]
    public Vector2 screenOffset = new(0f, 0f); // px extra

    Camera cam;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        cam = Camera.main;
        if (!canvas)
        {
            var c = FindObjectOfType<Canvas>();
            if (c) canvas = c.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        if (!cam) cam = Camera.main;
    }

    public static void ShowNumber(float amount, Vector3 worldPos, DamageNumber.Style style = DamageNumber.Style.Normal)
        => Instance?._ShowNumber(amount, worldPos, style);

    void _ShowNumber(float amount, Vector3 worldPos, DamageNumber.Style style)
    {
        if (!canvas || !numberPrefab) return;

        // 1) Mundo → Pantalla
        if (cam == null) cam = Camera.main;
        Vector3 sp = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        if (sp.z < 0f) return; // detrás de cámara

        // 2) Pantalla → Local en Canvas
        Vector2 local;
        var worldCam = (canvas.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, sp, worldCam, out local))
            return;

        // 3) Instanciar y posicionar
        var num = Instantiate(numberPrefab, canvas);
        var rt = num.GetComponent<RectTransform>();
        rt.anchoredPosition = local + screenOffset;

        num.Init(amount, style);
    }
}
