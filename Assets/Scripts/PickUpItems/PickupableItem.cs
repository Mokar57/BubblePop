using UnityEngine;

public enum ItemHoldType
{
    Primary,    // Önde tutulacak (tabanca için)
    Secondary   // Yanda tutulacak (beyzbol sopası için)
}

public class PickupableItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public string itemName = "Item";
    public float pickupRadius = 2f;
    public KeyCode pickupKey = KeyCode.E;
    public ItemHoldType holdType = ItemHoldType.Primary;
    public bool canBePickedByEnemies = true; // Enemy'ler bu item'ı alabilir mi?
    
    [Header("Visual Settings")]
    public GameObject pickupIndicator; // UI element to show "Press E to pick up"
    public Sprite spriteWhenHeld; // Silah tutuluyorken gösterilecek sprite
    public Sprite spriteWhenDropped; // Silah yerde iken gösterilecek sprite
    
    [Header("Usage Settings")]
    public int maxUsageCount = 10; // Maksimum kullanım sayısı
    public Sprite depletedSprite; // Kullanım bittiğinde gösterilecek sprite
    
    [Header("Audio Settings")]
    public AudioClip useSound; // Sol tık (kullanım) sesi
    public AudioClip throwSound; // Sağ tık (fırlatma) sesi
    [Range(0f, 1f)]
    public float useSoundVolume = 1f; // Kullanım sesinin yüksekliği
    [Range(0f, 1f)]
    public float throwSoundVolume = 1f; // Fırlatma sesinin yüksekliği
    
    [Header("Area Effect Settings")]
    public bool hasAreaEffect = false; // Bu item alan efekti çıkarır mı?
    public float areaRadius = 3f; // Alan yarıçapı
    public float areaDuration = 2f; // Alan ne kadar süre açık kalacak (saniye)
    public Color areaColor = new Color(1f, 0f, 0f, 0.3f); // Alanın rengi (varsayılan: yarı saydam kırmızı)
    public int areaSegments = 50; // Dairenin kaç segmentten oluşacağı (daha yüksek = daha düzgün daire)
    
    private AudioSource audioSource; // Sesleri çalmak için AudioSource
    private int currentUsageCount; // Mevcut kullanım sayısı
    private bool isDepleted = false; // Kullanım tükendi mi?
    private Sprite originalSprite; // Orijinal sprite'ı sakla
    private bool isHeld = false; // Silah tutuluyorken true olacak
    
    private bool playerInRange = false;
    private GameObject player;

    private void Start()
    {
        // Initialize usage count
        currentUsageCount = maxUsageCount;
        
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // AudioSource ayarlarını yapılandır
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D ses için
        
        // Store original sprite
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            
            // Eğer spriteWhenDropped atanmamışsa, orijinal sprite'ı kullan
            if (spriteWhenDropped == null)
            {
                spriteWhenDropped = originalSprite;
            }
        }
        
        // Hide pickup indicator at start
        if (pickupIndicator != null)
            pickupIndicator.SetActive(false);
        
        // Başlangıçta yerde olduğu için dropped sprite'ı kullan
        UpdateSprite();
    }

    private void Update()
    {
        // Check if item is currently being held by checking if parent is not null
        bool isCurrentlyHeld = transform.parent != null;
        
        // Tutulma durumu değiştiyse sprite'ı güncelle
        if (isCurrentlyHeld != isHeld)
        {
            isHeld = isCurrentlyHeld;
            UpdateSprite();
        }
        
        // Tükenen item'ları alamaz
        if (playerInRange && !isCurrentlyHeld && !isDepleted && Input.GetKeyDown(pickupKey))
        {
            PickupItem();
        }
        
        // Fallback distance check if trigger doesn't work
        CheckPlayerDistance();
    }
    
    private void CheckPlayerDistance()
    {
        // Find player by tag or name if not already found
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer == null)
            {
                foundPlayer = GameObject.Find("Player");
            }
            
            if (foundPlayer != null)
            {
                float distance = Vector2.Distance(transform.position, foundPlayer.transform.position);
                
                if (distance <= pickupRadius && !playerInRange)
                {
                    playerInRange = true;
                    player = foundPlayer;
                }
                else if (distance > pickupRadius && playerInRange)
                {
                    playerInRange = false;
                    player = null;
                }
            }
        }
        else
        {
            // Check distance to current player
            float distance = Vector2.Distance(transform.position, player.transform.position);
            
            if (distance > pickupRadius && playerInRange)
            {
                playerInRange = false;
                player = null;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            playerInRange = true;
            player = other.gameObject;
            
            if (pickupIndicator != null)
                pickupIndicator.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            playerInRange = false;
            player = null;
            
            if (pickupIndicator != null)
                pickupIndicator.SetActive(false);
        }
    }

    private void PickupItem()
    {
        if (player != null)
        {
            PlayerControls playerControls = player.GetComponent<PlayerControls>();
            if (playerControls != null)
            {
                // Item toplandığında collider'ı trigger yap
                Collider2D col = GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                }
                
                playerControls.PickupItem(this.gameObject, holdType);
                
                if (pickupIndicator != null)
                    pickupIndicator.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Enemy'nin item'ı almasını sağlar (programatik olarak)
    /// </summary>
    public bool TryPickupByEnemy(EnemyWeaponController enemyHolder)
    {
        if (!canBePickedByEnemies || isDepleted) return false;
        
        // Check if item is currently being held
        bool isCurrentlyHeld = transform.parent != null;
        if (isCurrentlyHeld) return false;
        
        // Item toplandığında collider'ı trigger yap
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        enemyHolder.PickupItem(this.gameObject, holdType);
        
        if (pickupIndicator != null)
            pickupIndicator.SetActive(false);
            
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup radius in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
    
    // Public method to reset pickup state when item is dropped
    public void ResetPickupState()
    {
        playerInRange = false;
        player = null;
        
        // Silah bırakıldığında sprite'ı güncelle
        isHeld = false;
        UpdateSprite();
        
        if (pickupIndicator != null)
            pickupIndicator.SetActive(false);
    }
    
    // Usage count management methods
    public void DecreaseUsage()
    {
        if (isDepleted) return;
        
        currentUsageCount--;
        
        if (currentUsageCount <= 0)
        {
            currentUsageCount = 0;
            MarkAsDepleted();
        }
    }
    
    private void MarkAsDepleted()
    {
        isDepleted = true;
        
        // Sprite'ı değiştir
        if (depletedSprite != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = depletedSprite;
            }
        }
    }
    
    public bool IsDepleted()
    {
        return isDepleted;
    }
    
    public int GetCurrentUsageCount()
    {
        return currentUsageCount;
    }
    
    public int GetMaxUsageCount()
    {
        return maxUsageCount;
    }
    
    /// <summary>
    /// Silahın sprite'ını durumuna göre günceller
    /// </summary>
    private void UpdateSprite()
    {
        // Eğer silah tükendiyse depleted sprite kullan
        if (isDepleted)
            return;
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;
        
        // Tutuluyorken ve tutulmuyor iken farklı sprite'lar kullan
        if (isHeld && spriteWhenHeld != null)
        {
            spriteRenderer.sprite = spriteWhenHeld;
        }
        else if (!isHeld && spriteWhenDropped != null)
        {
            spriteRenderer.sprite = spriteWhenDropped;
        }
    }
    
    /// <summary>
    /// Silahın tutulma durumunu manuel olarak ayarlamak için
    /// </summary>
    public void SetHeldState(bool held)
    {
        if (isHeld != held)
        {
            isHeld = held;
            UpdateSprite();
        }
    }
    
    /// <summary>
    /// Silahın tutulup tutulmadığını kontrol eder
    /// </summary>
    public bool IsHeld()
    {
        return isHeld;
    }
    
    /// <summary>
    /// Sol tık (kullanım) sesini çalar
    /// </summary>
    public void PlayUseSound()
    {
        if (audioSource != null && useSound != null)
        {
            audioSource.PlayOneShot(useSound, useSoundVolume);
        }
    }
    
    /// <summary>
    /// Sağ tık (fırlatma) sesini çalar
    /// </summary>
    public void PlayThrowSound()
    {
        if (audioSource != null && throwSound != null)
        {
            audioSource.PlayOneShot(throwSound, throwSoundVolume);
        }
    }
    
    /// <summary>
    /// Sol tık kullanıldığında yuvarlak alan efekti oluşturur (eğer hasAreaEffect true ise)
    /// </summary>
    public void TriggerAreaEffect(Vector3 position)
    {
        if (!hasAreaEffect) return;
        
        // Alan efekti için GameObject oluştur
        GameObject areaObject = new GameObject("AreaEffect");
        areaObject.transform.position = position;
        
        // AreaEffect component'ini ekle
        AreaEffect areaEffect = areaObject.AddComponent<AreaEffect>();
        areaEffect.radius = areaRadius;
        areaEffect.duration = areaDuration;
        areaEffect.stunDuration = 3f; // Enemy'lerin merkezde kalma süresi
        
        // LineRenderer ekle
        LineRenderer lineRenderer = areaObject.AddComponent<LineRenderer>();
        
        // LineRenderer ayarları
        lineRenderer.positionCount = areaSegments + 1;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        
        // Çizgi kalınlığı
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        
        // Material ve renk ayarları
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = areaColor;
        lineRenderer.endColor = areaColor;
        
        // Sorting layer ayarları (karakterlerin üzerinde görünsün)
        lineRenderer.sortingOrder = 100;
        
        // Daire şeklinde pozisyonlar oluştur
        float angle = 0f;
        float angleStep = 360f / areaSegments;
        
        for (int i = 0; i <= areaSegments; i++)
        {
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * areaRadius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * areaRadius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            angle += angleStep;
        }
        
        // Alan içini doldurmak için SpriteRenderer ekle (opsiyonel)
        GameObject fillObject = new GameObject("AreaFill");
        fillObject.transform.SetParent(areaObject.transform);
        fillObject.transform.localPosition = Vector3.zero;
        
        SpriteRenderer fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CreateCircleSprite(areaRadius);
        fillRenderer.color = areaColor;
        fillRenderer.sortingOrder = 99; // LineRenderer'ın altında
        
        // Belirtilen süre sonra alanı yok et (AreaEffect component'i bunu yönetiyor)
        // Destroy(areaObject, areaDuration); // Bu satırı kaldırıyoruz çünkü AreaEffect kendi duration'ını yönetiyor
    }
    
    /// <summary>
    /// Yuvarlak bir sprite oluşturur (alan dolgusu için)
    /// </summary>
    private Sprite CreateCircleSprite(float radius)
    {
        int resolution = 256;
        Texture2D texture = new Texture2D(resolution, resolution);
        
        float center = resolution / 2f;
        float radiusPixels = (resolution / 2f) - 2;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                
                if (distance <= radiusPixels)
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
        
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution / (radius * 2f));
    }
}