using UnityEngine;

public class CameraRoomLock : MonoBehaviour
{
    public static CameraRoomLock Instance { get; private set; }
    public Camera cam;
    public float margin = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!cam) cam = Camera.main;
    }

    public void SnapToRoom(Bounds roomBounds)
    {
        var c = roomBounds.center; c.z = cam.transform.position.z;
        cam.transform.position = c;

        float halfH = roomBounds.extents.y + margin;
        float halfW = roomBounds.extents.x + margin;
        cam.orthographicSize = Mathf.Max(halfH, halfW / cam.aspect);
    }
}
