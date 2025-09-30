using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    public enum Dir { Up, Down, Left, Right }
    public Dir direction;
    public Collider2D solid;     // bloquea paso cuando cerrada
    public SpriteRenderer sr;
    public Color openColor = Color.white;
    public Color closedColor = Color.red;

    public void SetOpen(bool open)
    {
        if (solid) solid.enabled = !open;
        if (sr) sr.color = open ? openColor : closedColor;
        gameObject.tag = open ? "Untagged" : "Door";
    }
}
