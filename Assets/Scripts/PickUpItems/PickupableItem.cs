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
    
    [Header("Usage Settings")]
    public int maxUsageCount = 10; // Maksimum kullanım sayısı
    public Sprite depletedSprite; // Kullanım bittiğinde gösterilecek sprite
    
    private int currentUsageCount; // Mevcut kullanım sayısı
    private bool isDepleted = false; // Kullanım tükendi mi?
    private Sprite originalSprite; // Orijinal sprite'ı sakla
    
    private bool playerInRange = false;
    private GameObject player;

    private void Start()
    {
        // Initialize usage count
        currentUsageCount = maxUsageCount;
        
        // Store original sprite
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
        
        // Hide pickup indicator at start
        if (pickupIndicator != null)
            pickupIndicator.SetActive(false);
    }

    private void Update()
    {
        // Check if item is currently being held by checking if parent is not null
        bool isCurrentlyHeld = transform.parent != null;
        
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
    public bool TryPickupByEnemy(EnemyItemHolder enemyHolder)
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
        
        if (pickupIndicator != null)
            pickupIndicator.SetActive(false);
    }
    
    // Usage count management methods
    public void DecreaseUsage()
    {
        if (isDepleted) return;
        
        currentUsageCount--;
        Debug.Log($"{itemName} usage decreased. Remaining: {currentUsageCount}/{maxUsageCount}");
        
        if (currentUsageCount <= 0)
        {
            currentUsageCount = 0;
            MarkAsDepleted();
        }
    }
    
    private void MarkAsDepleted()
    {
        isDepleted = true;
        Debug.Log($"{itemName} is now depleted!");
        
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
}