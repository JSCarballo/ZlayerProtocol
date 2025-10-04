// Assets/Scripts/UI/HoverLabelController.cs
using UnityEngine;
using UnityEngine.UI;

public class HoverLabelController : MonoBehaviour
{
    public static HoverLabelController Instance { get; private set; }

    [Header("Refs UI")]
    public CanvasGroup group;
    public Text titleText;
    public Text descText;
    public Vector2 screenOffset = new(0f, 40f);

    Transform target;
    Camera cam;
    float fadeSpeed = 10f;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        cam = Camera.main;
        if (!group) group = GetComponentInChildren<CanvasGroup>();
        HideImmediate();
    }

    void LateUpdate()
    {
        if (!target) { FadeOut(); return; }
        if (!cam) cam = Camera.main;

        Vector3 sp = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        transform.position = sp + (Vector3)screenOffset;

        // evita mostrar si está detrás de la cámara
        if (sp.z < 0f) { FadeOut(); return; }

        group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.deltaTime * fadeSpeed);
    }

    public void Show(Transform worldTarget, string title, string desc)
    {
        target = worldTarget;
        if (titleText) titleText.text = title ?? "";
        if (descText) descText.text = desc ?? "";
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        target = null;
    }

    void FadeOut()
    {
        if (group) group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.deltaTime * fadeSpeed);
        if (group && group.alpha <= 0.01f) gameObject.SetActive(false);
    }

    void HideImmediate()
    {
        if (group) group.alpha = 0f;
        gameObject.SetActive(false);
        target = null;
    }
}
