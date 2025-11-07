// Scripts/Rooms/Door.cs
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Door : MonoBehaviour
{
    [Header("Refs")]
    public BoxCollider2D solidCollider;   // bloquea paso cuando está cerrada
    public SpriteRenderer sr;

    [Header("Sprites (según orientación)")]
    public Sprite verticalSprite;   // para puertas verticales (arriba/abajo)
    public Sprite horizontalSprite; // para puertas horizontales (izq/der)

    [Header("Colores")]
    public Color openColor = Color.white;
    public Color closedColor = Color.red;

    // info de depuración
    [SerializeField, Tooltip("Solo lectura")] private bool isHorizontal;

    void Reset()
    {
        solidCollider = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnValidate()
    {
        if (!solidCollider) solidCollider = GetComponent<BoxCollider2D>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Configura sprite + escala + collider de la puerta según orientación y tamaño en unidades de mundo.
    /// </summary>
    public void Configure(bool horizontal, Vector2 worldSize, bool open = false)
    {
        isHorizontal = horizontal;

        // 1) Elegir sprite correcto
        if (sr)
        {
            sr.sprite = horizontal ? horizontalSprite : verticalSprite;
            if (!sr.sprite)
            {
                Debug.LogWarning($"[Door] Falta sprite {(horizontal ? "horizontal" : "vertical")} en " + name);
            }
        }

        // 2) Ajustar collider al tamaño de mundo
        if (solidCollider)
            solidCollider.size = worldSize;

        // 3) Ajustar escala del sprite a tamaño de mundo (si hay sprite)
        if (sr && sr.sprite)
        {
            // Tamaño del sprite en unidades de mundo (usa PPU del sprite)
            Vector2 spriteWorld = sr.sprite.bounds.size;
            float sx = worldSize.x / Mathf.Max(0.0001f, spriteWorld.x);
            float sy = worldSize.y / Mathf.Max(0.0001f, spriteWorld.y);
            sr.transform.localScale = new Vector3(sx, sy, 1f);
        }

        // 4) Estado abierto/cerrado (color + colisión)
        SetOpen(open);
    }

    /// <summary>
    /// Abre/cierra la puerta: collider ON/OFF + color.
    /// </summary>
    public void SetOpen(bool open)
    {
        if (solidCollider) solidCollider.enabled = !open; // abierto = no bloquea
        if (sr) sr.color = open ? openColor : closedColor;
    }
}
