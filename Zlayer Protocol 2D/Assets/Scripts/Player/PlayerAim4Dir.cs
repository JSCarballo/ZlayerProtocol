using UnityEngine;

/// Mantiene la dirección cardinal de apuntado (flechas).
/// No cambia sprites: sólo expone AimDir para toros/anim y shooter.
public class PlayerAim4Dir : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private Vector2 aimDir = Vector2.down; // default

    public Vector2 AimDir => aimDir;

    void Update()
    {
        Vector2 a = Vector2.zero;
        if (Input.GetKey(KeyCode.RightArrow)) a.x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) a.x -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) a.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) a.y -= 1f;

        if (a != Vector2.zero)
        {
            // Forzar a 4 direcciones puras
            aimDir = Mathf.Abs(a.x) > Mathf.Abs(a.y) ? new Vector2(Mathf.Sign(a.x), 0f)
                                                     : new Vector2(0f, Mathf.Sign(a.y));
        }
    }

    public Vector2 GetAimDir() => aimDir;
}
