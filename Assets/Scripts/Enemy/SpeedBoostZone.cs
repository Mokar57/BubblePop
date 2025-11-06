using UnityEngine;
using System.Collections.Generic;

public class SpeedBoostZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private float zoneRadius = 5f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float zoneDuration = 10f;
    [SerializeField] private Color zoneColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private LayerMask affectedLayers = ~0; // Hangi layer'lardaki objeleri etkileyeceğiz (default: hepsi)
    
    [Header("Effects")]
    [SerializeField] private GameObject zoneEffect;
    [SerializeField] private bool showZoneVisual = true;
    
    private CircleCollider2D zoneCollider;
    private SpriteRenderer zoneRenderer;
    private List<ISpeedBoostable> entitiesInZone = new List<ISpeedBoostable>();
    private float remainingTime;
    
    private void Start()
    {
        remainingTime = zoneDuration;
        SetupZone();
        
        // Zone süresini başlat
        if (zoneDuration > 0)
        {
            Invoke(nameof(DestroyZone), zoneDuration);
        }
    }
    
    private void SetupZone()
    {
        // Collider setup
        zoneCollider = GetComponent<CircleCollider2D>();
        if (zoneCollider == null)
        {
            zoneCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        zoneCollider.radius = zoneRadius;
        zoneCollider.isTrigger = true;
        
        // Visual setup
        if (showZoneVisual)
        {
            SetupVisual();
        }
        
        // Effect setup
        if (zoneEffect != null)
        {
            Instantiate(zoneEffect, transform.position, transform.rotation, transform);
        }
    }
    
    private void SetupVisual()
    {
        // Görsel alan oluştur
        GameObject visual = new GameObject("ZoneVisual");
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        
        zoneRenderer = visual.AddComponent<SpriteRenderer>();
        
        // Daire sprite oluştur
        Sprite circleSprite = CreateCircleSprite();
        zoneRenderer.sprite = circleSprite;
        zoneRenderer.color = zoneColor;
        zoneRenderer.sortingOrder = -1; // Arka planda görünsün
        
        // Scale'i zone radius'a göre ayarla
        float scale = zoneRadius * 2f;
        visual.transform.localScale = Vector3.one * scale;
    }
    
    private Sprite CreateCircleSprite()
    {
        // 64x64 texture oluştur
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private void Update()
    {
        if (zoneDuration > 0)
        {
            remainingTime -= Time.deltaTime;
            
            // Görsel alpha güncellemesi
            if (zoneRenderer != null)
            {
                float alpha = Mathf.Lerp(0f, zoneColor.a, remainingTime / zoneDuration);
                Color currentColor = zoneRenderer.color;
                currentColor.a = alpha;
                zoneRenderer.color = currentColor;
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Layer kontrolü yap
        if (((1 << other.gameObject.layer) & affectedLayers) == 0)
        {
            return;
        }
        
        ISpeedBoostable speedBoostable = other.GetComponent<ISpeedBoostable>();
        if (speedBoostable != null)
        {
            if (!entitiesInZone.Contains(speedBoostable))
            {
                entitiesInZone.Add(speedBoostable);
                speedBoostable.ApplySpeedBoost(speedMultiplier);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        ISpeedBoostable speedBoostable = other.GetComponent<ISpeedBoostable>();
        if (speedBoostable != null && entitiesInZone.Contains(speedBoostable))
        {
            entitiesInZone.Remove(speedBoostable);
            speedBoostable.RemoveSpeedBoost();
        }
    }
    
    private void DestroyZone()
    {
        // Zone yok edilmeden önce tüm entitylerin boost'unu kaldır
        foreach (var entity in entitiesInZone)
        {
            if (entity != null)
            {
                entity.RemoveSpeedBoost();
            }
        }
        
        entitiesInZone.Clear();
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = zoneColor;
        Gizmos.DrawWireSphere(transform.position, zoneRadius);
    }
    
    // Public methods for customization
    public void SetZoneRadius(float radius)
    {
        zoneRadius = radius;
        if (zoneCollider != null)
        {
            zoneCollider.radius = zoneRadius;
        }
    }
    
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }
    
    public void SetZoneDuration(float duration)
    {
        zoneDuration = duration;
        remainingTime = duration;
    }
    
    // Getters
    public float ZoneRadius => zoneRadius;
    public float SpeedMultiplier => speedMultiplier;
    public float RemainingTime => remainingTime;
}