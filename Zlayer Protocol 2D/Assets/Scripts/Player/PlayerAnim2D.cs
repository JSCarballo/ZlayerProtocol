using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnim2D : MonoBehaviour
{
    [Header("References")]
    public Animator bodyAnimator;
    public Animator armsAnimator;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer armsRenderer;
    public SpriteRenderer muzzleFlash;   // sprite del destello
    public float muzzleFlashTime = 0.06f;

    [Header("Locomotion")]
    public float movingThreshold = 0.05f;

    [Header("Aim Offsets (local, relativo a Arms)")]
    public Vector2 muzzleOffsetRight = new Vector2(0.45f, 0.10f);
    public Vector2 muzzleOffsetLeft = new Vector2(-0.45f, 0.10f);
    public Vector2 muzzleOffsetUp = new Vector2(0.00f, 0.55f);
    public Vector2 muzzleOffsetDown = new Vector2(0.00f, -0.20f);

    private Rigidbody2D rb;
    private Vector2 lastMoveDir = Vector2.down;
    private Vector2 lastAimDir = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (muzzleFlash) muzzleFlash.enabled = false;
    }

    void Update()
    {
        UpdateLocomotion();
        UpdateAimDirection();
    }

    void UpdateLocomotion()
    {
        Vector2 vel = rb ? rb.linearVelocity : Vector2.zero;
        float speed = vel.magnitude;
        Vector2 moveDir = (speed > 0.0001f) ? vel.normalized : Vector2.zero;

        if (bodyAnimator)
        {
            bodyAnimator.SetFloat("Speed", speed);
            if (moveDir != Vector2.zero)
            {
                bodyAnimator.SetFloat("MoveX", moveDir.x);
                bodyAnimator.SetFloat("MoveY", moveDir.y);
                bodyAnimator.SetFloat("LastMoveX", moveDir.x);
                bodyAnimator.SetFloat("LastMoveY", moveDir.y);
                lastMoveDir = moveDir;
            }
            else
            {
                bodyAnimator.SetFloat("MoveX", 0f);
                bodyAnimator.SetFloat("MoveY", 0f);
                bodyAnimator.SetFloat("LastMoveX", lastMoveDir.x);
                bodyAnimator.SetFloat("LastMoveY", lastMoveDir.y);
            }
        }
    }

    void UpdateAimDirection()
    {
        Vector2 aim = Vector2.zero;
        if (Input.GetKey(KeyCode.RightArrow)) aim.x += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) aim.x -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) aim.y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) aim.y -= 1f;

        if (aim != Vector2.zero)
        {
            if (Mathf.Abs(aim.x) > Mathf.Abs(aim.y)) aim = new Vector2(Mathf.Sign(aim.x), 0f);
            else aim = new Vector2(0f, Mathf.Sign(aim.y));
            lastAimDir = aim;
        }

        if (armsAnimator)
        {
            armsAnimator.SetFloat("AimX", lastAimDir.x);
            armsAnimator.SetFloat("AimY", lastAimDir.y);
        }

        UpdateMuzzleTransform(lastAimDir);
    }

    void UpdateMuzzleTransform(Vector2 dir)
    {
        if (!muzzleFlash || !armsRenderer) return;

        if (dir == Vector2.right)
        {
            muzzleFlash.transform.localPosition = muzzleOffsetRight;
            muzzleFlash.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (dir == Vector2.left)
        {
            muzzleFlash.transform.localPosition = muzzleOffsetLeft;
            muzzleFlash.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }
        else if (dir == Vector2.up)
        {
            muzzleFlash.transform.localPosition = muzzleOffsetUp;
            muzzleFlash.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        else if (dir == Vector2.down)
        {
            muzzleFlash.transform.localPosition = muzzleOffsetDown;
            muzzleFlash.transform.localRotation = Quaternion.Euler(0, 0, -90);
        }
    }

    // Llama esto desde el shooter para sincronizar flash/recoil con la bala
    public void NotifyShot(Vector2 aimDir)
    {
        if (aimDir != Vector2.zero)
        {
            if (Mathf.Abs(aimDir.x) > Mathf.Abs(aimDir.y)) lastAimDir = new Vector2(Mathf.Sign(aimDir.x), 0);
            else lastAimDir = new Vector2(0, Mathf.Sign(aimDir.y));
        }

        if (armsAnimator) armsAnimator.SetTrigger("Shoot");
        UpdateMuzzleTransform(lastAimDir);
        if (muzzleFlash) StartCoroutine(FlashCR());
    }

    IEnumerator FlashCR()
    {
        muzzleFlash.enabled = true;
        yield return new WaitForSeconds(muzzleFlashTime);
        muzzleFlash.enabled = false;
    }
}
