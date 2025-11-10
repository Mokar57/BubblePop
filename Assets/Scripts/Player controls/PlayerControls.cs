using UnityEngine;

public class PlayerControls : MonoBehaviour, ISpeedBoostable
{
    
    public float moveSpeed;
    public Rigidbody2D rb;

    // Speed boost variables
    private float originalMoveSpeed;
    private float speedBoostMultiplier = 1f;
    private bool isSpeedBoosted = false;

    private Vector2 moveDirection;
    
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Header("Death Effects")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.1f;
    
    // Health variables
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDeath;
    
    [Header("Item Pickup System")]
    public Transform primaryHoldPosition;   // Önde tutulacak eşyalar için (tabanca)
    public Transform secondaryHoldPosition; // Yanda tutulacak eşyalar için (beyzbol sopası)
    public GameObject currentPrimaryItem;   // Şu anda önde tutulan eşya
    public GameObject currentSecondaryItem; // Şu anda yanda tutulan eşya
    
    private void Start()
    {
        // Store original speed for speed boost functionality
        originalMoveSpeed = moveSpeed;
        
        // Initialize health
        currentHealth = maxHealth;
        
        // Get sprite renderer for damage flash
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    private void Update()
    {
        ProcessInput();
        
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector2 direction = new Vector2(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y);
        
        transform.up = direction;
        
        // Sol tık attack inputu
        if (Input.GetMouseButtonDown(0))
        {
            PerformAttack();
        }
        
        // Sağ tık item fırlatma inputu
        if (Input.GetMouseButtonDown(1))
        {
            ThrowCurrentItem();
        }
    }
    

    private void FixedUpdate()
    {
        Move();
    }

    void ProcessInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        
        moveDirection = new Vector2(moveX, moveY);
    }

    void Move()
    {
        float currentMoveSpeed = originalMoveSpeed * speedBoostMultiplier;
        rb.linearVelocity = new Vector2(moveDirection.x * currentMoveSpeed, moveDirection.y * currentMoveSpeed);
    }
    
    #region ISpeedBoostable Implementation
    
    public void ApplySpeedBoost(float multiplier)
    {
        if (!isSpeedBoosted)
        {
            speedBoostMultiplier = multiplier;
            isSpeedBoosted = true;
        }
    }
    
    public void RemoveSpeedBoost()
    {
        if (isSpeedBoosted)
        {
            speedBoostMultiplier = 1f;
            isSpeedBoosted = false;
        }
    }
    
    public bool IsSpeedBoosted => isSpeedBoosted;
    
    #endregion
    
    #region Attack System
    
    private void PerformAttack()
    {
        // Şu anda tuttuğu silahı kontrol et
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon == null) return;
        
        // Weapon type'ına göre attack gerçekleştir
        PickupableItem weaponPickup = currentWeapon.GetComponent<PickupableItem>();
        if (weaponPickup == null) return;
        
        if (weaponPickup.holdType == ItemHoldType.Primary)
        {
            // Primary weapon - Projectile attack
            PerformProjectileAttack(currentWeapon);
        }
        else if (weaponPickup.holdType == ItemHoldType.Secondary)
        {
            // Secondary weapon - Melee attack
            PerformMeleeAttack(currentWeapon);
        }
    }
    
    private void PerformProjectileAttack(GameObject weapon)
    {
        PistolItem pistol = weapon.GetComponent<PistolItem>();
        if (pistol != null)
        {
            pistol.Fire(transform.position, transform.up);
        }
    }
    
    private void PerformMeleeAttack(GameObject weapon)
    {
        BaseballBatItem bat = weapon.GetComponent<BaseballBatItem>();
        if (bat != null)
        {
            bat.Attack(transform.position, transform.up);
        }
    }
    
    #endregion
    
    #region Item Pickup System
    
    [Header("Item Throwing Settings")]
    public float throwForce = 10f;
    
    public void ThrowCurrentItem()
    {
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon == null) return;
        
        // Fırlatılan silahı hemen depleted yap
        PickupableItem pickupable = currentWeapon.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            // Kullanım sayısını 0'a çek ki depleted olsun
            while (!pickupable.IsDepleted())
            {
                pickupable.DecreaseUsage();
            }
        }
        
        // Item'ı fırlatmadan önce tüm özelliklerini koru
        ThrowItemAtDirection(currentWeapon, transform.up);
        
        // Item fırlatıldıktan sonra player'ın elinden çıkar
        if (currentPrimaryItem == currentWeapon)
            currentPrimaryItem = null;
        else if (currentSecondaryItem == currentWeapon)
            currentSecondaryItem = null;
    }
    
    private void ThrowItemAtDirection(GameObject item, Vector2 direction)
    {
        // Item'ı parent'tan ayır
        item.transform.SetParent(null);
        
        // Item'ın pozisyonunu player'ın mevcut pozisyonuna ayarla
        item.transform.position = transform.position;
        
        // Rotation'ı korumak için değiştirme
        // item.transform.rotation = Quaternion.identity;
        
        // Physics'i etkinleştir
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            itemRb.angularVelocity = 0f;
            
            // Collision Detection'ı Continuous yap (Tilemap'ten geçmeyi önler)
            itemRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            // Interpolation ekle (daha düzgün hareket)
            itemRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            // Gravity scale'i kontrol et (eğer 0 ise düşmez)
            if (itemRb.gravityScale == 0)
                itemRb.gravityScale = 0; // Top-down oyunsa 0 kalabilir
            
            // Damping değerlerini sıfırla (önceki drop'tan kalan yüksek değerleri temizle)
            itemRb.linearDamping = 0f;
            itemRb.angularDamping = 0.05f;
            
            // Constraints - Z rotation dışında her şeyi serbest bırak
            itemRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            // İleriye doğru kuvvet uygula
            itemRb.linearVelocity = direction.normalized * throwForce;
        }
        
        // Collider'ı yeniden etkinleştir ve IsTrigger'ı kapat (fiziksel çarpışma için)
        Collider2D itemCollider = item.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
            itemCollider.isTrigger = false; // Fiziksel çarpışma için trigger'ı kapat
        }
        
        // ThrownItem component'i ekle (çarpma ve yavaşlama için)
        ThrownItem thrownComponent = item.GetComponent<ThrownItem>();
        if (thrownComponent == null)
        {
            thrownComponent = item.AddComponent<ThrownItem>();
        }
        thrownComponent.Initialize();
        thrownComponent.SetThrower(gameObject); // Fırlatan kişiyi set et
        
        // PickupableItem component'ini yeniden etkinleştir
        PickupableItem pickupable = item.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.enabled = true;
            pickupable.ResetPickupState();
        }
        
        // Tüm item özellikleri korunmuş olarak kalacak (ammo, damage, vb.)
        // Çünkü sadece transform ve physics özellikleri değiştiriliyor
    }
    
    private void DropCurrentWeaponAtPosition(GameObject weapon, Vector3 dropPosition)
    {
        // Store weapon properties before dropping
        PistolItem pistolItem = weapon.GetComponent<PistolItem>();
        BaseballBatItem batItem = weapon.GetComponent<BaseballBatItem>();
        
        // Remove from parent
        weapon.transform.SetParent(null);
        
        // Set position to player's current position
        weapon.transform.position = dropPosition;
        
        // Reset rotation to (0, 0, 0)
        weapon.transform.rotation = Quaternion.identity;
        
        // Re-enable physics
        Rigidbody2D itemRb = weapon.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            // Reset angular velocity to prevent rotation
            itemRb.angularVelocity = 0f;
            // Set velocity to zero so item stays in place
            itemRb.linearVelocity = Vector2.zero;
            // Add high drag to prevent sliding
            itemRb.linearDamping = 10f;
            itemRb.angularDamping = 10f;
        }
        
        // Re-enable collider
        Collider2D itemCollider = weapon.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
        
        // Reset pickup state
        PickupableItem pickupable = weapon.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            // Reset the pickup state so it can be picked up again
            pickupable.enabled = true;
            pickupable.ResetPickupState();
        }
        
        // Restore weapon properties (they should remain the same)
        if (pistolItem != null)
        {
            // Pistol properties are already maintained
        }
        if (batItem != null)
        {
            // Baseball bat properties are already maintained
        }
    }

    public void PickupItem(GameObject item, ItemHoldType holdType = ItemHoldType.Primary)
    {
        // Check if player already has a weapon (either primary or secondary)
        GameObject currentWeapon = null;
        
        if (currentPrimaryItem != null)
        {
            currentWeapon = currentPrimaryItem;
        }
        else if (currentSecondaryItem != null)
        {
            currentWeapon = currentSecondaryItem;
        }
        
        // If player has a weapon, drop it at player's current position with same properties
        if (currentWeapon != null)
        {
            DropCurrentWeaponAtPosition(currentWeapon, transform.position);
        }
        
        // Assign new item to appropriate slot
        if (holdType == ItemHoldType.Primary)
        {
            currentPrimaryItem = item;
            currentSecondaryItem = null; // Clear other slot
        }
        else
        {
            currentSecondaryItem = item;
            currentPrimaryItem = null; // Clear other slot
        }
        
        // Make the item a child of the player
        item.transform.SetParent(transform);
        
        // Silah tutulduğunda sprite'ı güncelle
        PickupableItem pickupable = item.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.SetHeldState(true);
        }
        
        // Preserve item's original sprite properties
        SpriteRenderer itemRenderer = item.GetComponent<SpriteRenderer>();
        if (itemRenderer != null)
        {
            // Ensure the item stays visible and keeps its original color
            Color originalColor = itemRenderer.color;
            if (originalColor.a > 0) // Only preserve if item was visible
            {
                itemRenderer.color = originalColor;
            }
        }
        
        // Position the item at the designated hold position
        Transform targetPosition = (holdType == ItemHoldType.Primary) ? primaryHoldPosition : secondaryHoldPosition;
        
        if (targetPosition != null)
        {
            item.transform.position = targetPosition.position;
            item.transform.rotation = targetPosition.rotation;
        }
        else
        {
            // Default positions if no hold position is set
            if (holdType == ItemHoldType.Primary)
            {
                item.transform.localPosition = new Vector3(1f, 0f, 0f); // Önde
            }
            else
            {
                item.transform.localPosition = new Vector3(0f, 1f, 0f); // Yanda
            }
        }
        
        // Disable physics on the item so it doesn't interfere with player movement
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Disable collider so it doesn't interfere with pickup detection
        Collider2D itemCollider = item.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
    }
    
    public void DropItem(ItemHoldType holdType = ItemHoldType.Primary)
    {
        GameObject itemToDrop = null;
        
        // Find which item to drop (check both slots since player can only have one weapon)
        if (currentPrimaryItem != null)
        {
            itemToDrop = currentPrimaryItem;
            currentPrimaryItem = null;
        }
        else if (currentSecondaryItem != null)
        {
            itemToDrop = currentSecondaryItem;
            currentSecondaryItem = null;
        }
        
        if (itemToDrop == null) return;
        
        // Use the new drop method to maintain weapon properties
        DropCurrentWeaponAtPosition(itemToDrop, transform.position);
    }
    
    // Drop current weapon (since player can only hold one weapon now)
    public void DropCurrentWeapon()
    {
        DropItem();
    }
    
    // Keep for backward compatibility
    public void DropPrimaryItem()
    {
        DropItem();
    }
    
    public void DropSecondaryItem()
    {
        DropItem();
    }
    
    // Drop all items (now just drops the single weapon)
    public void DropAllItems()
    {
        DropItem();
    }
    
    // Check if player has any weapon
    public bool HasWeapon()
    {
        return currentPrimaryItem != null || currentSecondaryItem != null;
    }
    
    // Get current weapon (returns null if no weapon)
    public GameObject GetCurrentWeapon()
    {
        if (currentPrimaryItem != null)
            return currentPrimaryItem;
        else if (currentSecondaryItem != null)
            return currentSecondaryItem;
        else
            return null;
    }
    
    #endregion
    
    #region Health System
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Visual feedback
        StartCoroutine(DamageFlash());
        
        // Trigger event
        OnHealthChanged?.Invoke(currentHealth);
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Trigger event
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Trigger local death event (backward compatibility)
        OnPlayerDeath?.Invoke();
        
        // Trigger global death event (decoupled system)
        GameEvents.TriggerPlayerDeath();
        
        // Disable player controls
        this.enabled = false;
        
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        // Optional: Change sprite color or play death animation
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray out
        }
        
        Debug.Log("Player Died!");
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    // Public getters
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    
    #endregion
}
