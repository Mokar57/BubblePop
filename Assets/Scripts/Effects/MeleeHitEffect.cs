using UnityEngine;

/// <summary>
/// Visual effect for melee weapon hits
/// Creates a temporary white flash/area effect at the hit location
/// Automatically destroys itself after duration
/// </summary>
public class MeleeHitEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Color of the hit effect")]
    public Color effectColor = Color.white;

    [Tooltip("Initial alpha value")]
    [Range(0f, 1f)]
    public float startAlpha = 0.8f;

    [Tooltip("Size of the effect")]
    public Vector2 effectSize = new Vector2(2f, 2f);

    [Header("Animation")]
    [Tooltip("How long the effect lasts before fading out")]
    public float duration = 0.2f;

    [Tooltip("Should the effect fade out over time?")]
    public bool fadeOut = true;

    [Tooltip("Should the effect expand over time?")]
    public bool expand = false;

    [Tooltip("Scale multiplier if expanding")]
    public float expandScale = 1.5f;

    private SpriteRenderer spriteRenderer;
    private float elapsedTime = 0f;
    private Vector3 initialScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Create a simple circle sprite if none exists
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateCircleSprite();
        }

        // Set initial color
        effectColor.a = startAlpha;
        spriteRenderer.color = effectColor;

        // Set size
        transform.localScale = new Vector3(effectSize.x, effectSize.y, 1f);
        initialScale = transform.localScale;

        // Set sorting order to appear above most objects
        spriteRenderer.sortingOrder = 100;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        float progress = elapsedTime / duration;

        // Fade out
        if (fadeOut && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(startAlpha, 0f, progress);
            spriteRenderer.color = color;
        }

        // Expand
        if (expand)
        {
            float scale = Mathf.Lerp(1f, expandScale, progress);
            transform.localScale = initialScale * scale;
        }

        // Destroy when done
        if (elapsedTime >= duration)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Creates a simple circle sprite for the effect
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
        float radius = resolution * 0.5f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(distance / radius);
                
                // Smooth edges
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution / 2f
        );
    }

    /// <summary>
    /// Static factory method to spawn a hit effect
    /// </summary>
    public static void SpawnEffect(Vector3 position, float size, float duration, Color? color = null)
    {
        GameObject effectObj = new GameObject("MeleeHitEffect");
        effectObj.transform.position = position;

        MeleeHitEffect effect = effectObj.AddComponent<MeleeHitEffect>();
        effect.effectSize = new Vector2(size, size);
        effect.duration = duration;
        
        if (color.HasValue)
        {
            effect.effectColor = color.Value;
        }
    }
}
