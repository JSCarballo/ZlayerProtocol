// Scripts/Rooms/Door.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    public Collider2D solidCollider;   // si no asignas, toma el suyo
    public SpriteRenderer sr;
    public Color openColor = Color.white;
    public Color closedColor = Color.red;

    void Reset()
    {
        solidCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void SetOpen(bool open)
    {
        if (solidCollider) solidCollider.enabled = !open; // abierto = no bloquea
        if (sr) sr.color = open ? openColor : closedColor;
    }
}
