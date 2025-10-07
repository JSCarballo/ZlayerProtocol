using UnityEngine;
using System.Collections.Generic;

/// Controla el apuntado en 4 direcciones usando las FLECHAS y
/// cambia el Sprite del jugador en consecuencia.
/// Expone AimDir para que el shooter dispare en esa dirección.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAim4Dir : MonoBehaviour
{
    [Header("Asignación por sprites individuales (dejar vacía si usarás sheet)")]
    public Sprite aimRight;
    public Sprite aimLeft;
    public Sprite aimUp;
    public Sprite aimDown;

    [Header("O bien, usa sprites del sheet (orden esperado: Right, Up, Down, Left)")]
    public List<Sprite> sheetFrames = new List<Sprite>();

    [Header("Muzzle Flash (opcional)")]
    public SpriteRenderer muzzleRenderer;  // hijo desactivado por defecto
    public Sprite muzzleRight, muzzleLeft, muzzleUp, muzzleDown;
    public float muzzleTime = 0.06f;

    [Header("Estado (solo lectura)")]
    public Vector2 AimDir = Vector2.down; // comienza mirando abajo

    SpriteRenderer sr;
    float muzzleTimer = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (muzzleRenderer) muzzleRenderer.enabled = false;
        // Si usas sheet y no arrastras nada individual, auto-map
        if (sheetFrames != null && sheetFrames.Count >= 4 && aimRight == null && aimUp == null)
        {
            // Right, Up, Down, Left
            aimRight = sheetFrames[0];
            aimUp = sheetFrames[1];
            aimDown = sheetFrames[2];
            aimLeft = sheetFrames[3];
        }
        if (!sr.sprite && aimDown) sr.sprite = aimDown;
    }

    void Update()
    {
        Vector2 aim = Vector2.zero;

        if (Input.GetKey(KeyCode.RightArrow)) aim.x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) aim.x -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) aim.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) aim.y -= 1f;

        bool changedThisFrame = false;

        if (aim != Vector2.zero)
        {
            // Forzar a 4 direcciones puras
            if (Mathf.Abs(aim.x) > Mathf.Abs(aim.y)) aim = new Vector2(Mathf.Sign(aim.x), 0f);
            else aim = new Vector2(0f, Mathf.Sign(aim.y));

            changedThisFrame = (aim != AimDir);
            AimDir = aim;
            ApplySpriteFor(AimDir);
        }

        // Feedback de flash si pulsaste una flecha este frame
        if (muzzleRenderer)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) ShowMuzzle(Vector2.right);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) ShowMuzzle(Vector2.left);
            else if (Input.GetKeyDown(KeyCode.UpArrow)) ShowMuzzle(Vector2.up);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) ShowMuzzle(Vector2.down);

            if (muzzleRenderer.enabled)
            {
                muzzleTimer -= Time.deltaTime;
                if (muzzleTimer <= 0f) muzzleRenderer.enabled = false;
            }
        }
    }

    void ApplySpriteFor(Vector2 dir)
    {
        if (dir == Vector2.right && aimRight) sr.sprite = aimRight;
        else if (dir == Vector2.left && aimLeft) sr.sprite = aimLeft;
        else if (dir == Vector2.up && aimUp) sr.sprite = aimUp;
        else if (dir == Vector2.down && aimDown) sr.sprite = aimDown;
    }

    void ShowMuzzle(Vector2 dir)
    {
        if (!muzzleRenderer) return;

        if (dir == Vector2.right && muzzleRight) muzzleRenderer.sprite = muzzleRight;
        else if (dir == Vector2.left && muzzleLeft) muzzleRenderer.sprite = muzzleLeft;
        else if (dir == Vector2.up && muzzleUp) muzzleRenderer.sprite = muzzleUp;
        else if (dir == Vector2.down && muzzleDown) muzzleRenderer.sprite = muzzleDown;

        // Offsets sencillos (ajústalos a tu sprite)
        if (dir == Vector2.right) muzzleRenderer.transform.localPosition = new Vector3(0.60f, 0.00f, 0f);
        else if (dir == Vector2.left) muzzleRenderer.transform.localPosition = new Vector3(-0.60f, 0.00f, 0f);
        else if (dir == Vector2.up) muzzleRenderer.transform.localPosition = new Vector3(0.00f, 0.70f, 0f);
        else if (dir == Vector2.down) muzzleRenderer.transform.localPosition = new Vector3(0.00f, -0.10f, 0f);

        muzzleRenderer.enabled = true;
        muzzleTimer = muzzleTime;
    }

    // API para tu shooter: úsalo al instanciar la bala.
    public Vector2 GetAimDir() => AimDir;
}
