using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public SpriteRenderer sr;
    public Sprite flashSprite;
    public float flashTime = 0.05f;

    public void Flash(Vector3 position)
    {
        if (sr == null) return;
        sr.sprite = flashSprite;
        sr.transform.position = position;
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), flashTime);
    }

    void Hide()
    {
        if (sr) sr.sprite = null;
    }
}
