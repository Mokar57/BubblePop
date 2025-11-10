using UnityEngine;

/// <summary>
/// Handles enemy's ability to hold and use PickupableItems
/// Similar to PlayerControls item system, but designed for enemy AI
/// </summary>
public class EnemyItemHolder : MonoBehaviour
{
    [Header("Item Hold Positions")]
    public Transform primaryHoldPosition;   // Önde tutulacak eşyalar için (tabanca)
    public Transform secondaryHoldPosition; // Yanda tutulacak eşyalar için (beyzbol sopası)
    
    [Header("Current Items")]
    public GameObject currentPrimaryItem;   // Şu anda önde tutulan eşya
    public GameObject currentSecondaryItem; // Şu anda yanda tutulan eşya
    
    [Header("Item Throwing Settings")]
    public float throwForce = 10f;
    
    [Header("Attack Settings")]
    public bool canUseItems = true; // Enemy item kullanabilir mi?
    
    private EnemyAI enemyAI;
    
    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
    }
    
    private void LateUpdate()
    {
        // Enemy'nin görüş yönüne göre enemy ve item'ları döndür
        UpdateRotationBasedOnVision();
    }
    
    /// <summary>
    /// Enemy'nin görüş yönüne göre enemy'yi ve item'ları döndürür
    /// </summary>
    private void UpdateRotationBasedOnVision()
    {
        if (enemyAI == null) return;
        
        // EnemyAI'dan görüş yönünü al
        Vector3 visionDirection = enemyAI.GetVisionDirection();
        
        if (visionDirection.magnitude > 0.01f)
        {
            // Enemy'nin kendisini görüş yönüne döndür
            float angle = Mathf.Atan2(visionDirection.y, visionDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // -90 çünkü Unity'de up vektörü 90 derece offset'li
        }
    }
    
    #region Item Pickup System
    
    /// <summary>
    /// Enemy bir item'ı alır ve belirlenen slot'a yerleştirir
    /// </summary>
    public void PickupItem(GameObject item, ItemHoldType holdType = ItemHoldType.Primary)
    {
        if (item == null) return;
        
        // Eğer enemy zaten bir silah tutuyorsa, onu düşür
        GameObject currentWeapon = null;
        
        if (currentPrimaryItem != null)
        {
            currentWeapon = currentPrimaryItem;
        }
        else if (currentSecondaryItem != null)
        {
            currentWeapon = currentSecondaryItem;
        }
        
        // Mevcut silahı düşür
        if (currentWeapon != null)
        {
            DropCurrentWeaponAtPosition(currentWeapon, transform.position);
        }
        
        // Yeni item'ı uygun slot'a ata
        if (holdType == ItemHoldType.Primary)
        {
            currentPrimaryItem = item;
            currentSecondaryItem = null; // Diğer slot'u temizle
        }
        else
        {
            currentSecondaryItem = item;
            currentPrimaryItem = null; // Diğer slot'u temizle
        }
        
        // Item'ı enemy'nin child'ı yap
        item.transform.SetParent(transform);
        
        // Silah tutulduğunda sprite'ı güncelle
        PickupableItem pickupable = item.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.SetHeldState(true);
        }
        
        // Item'ın sprite özelliklerini koru
        SpriteRenderer itemRenderer = item.GetComponent<SpriteRenderer>();
        if (itemRenderer != null)
        {
            Color originalColor = itemRenderer.color;
            if (originalColor.a > 0)
            {
                itemRenderer.color = originalColor;
            }
        }
        
        // Item'ı belirlenen tutma pozisyonuna yerleştir
        Transform targetPosition = (holdType == ItemHoldType.Primary) ? primaryHoldPosition : secondaryHoldPosition;
        
        if (targetPosition != null)
        {
            item.transform.position = targetPosition.position;
            item.transform.rotation = targetPosition.rotation;
        }
        else
        {
            // Default pozisyonlar
            if (holdType == ItemHoldType.Primary)
            {
                item.transform.localPosition = new Vector3(1f, 0f, 0f); // Önde
            }
            else
            {
                item.transform.localPosition = new Vector3(0f, 1f, 0f); // Yanda
            }
        }
        
        // Physics'i devre dışı bırak
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Collider'ı devre dışı bırak
        Collider2D itemCollider = item.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        
        // PickupableItem component'ini devre dışı bırak (başkaları alamasın)
        if (pickupable != null)
        {
            pickupable.enabled = false;
        }
    }
    
    /// <summary>
    /// Enemy elindeki silahı belirtilen pozisyona düşürür
    /// </summary>
    private void DropCurrentWeaponAtPosition(GameObject weapon, Vector3 dropPosition)
    {
        if (weapon == null) return;
        
        // Parent'tan ayır
        weapon.transform.SetParent(null);
        
        // Pozisyonu ayarla
        weapon.transform.position = dropPosition;
        
        // Rotation'ı sıfırla
        weapon.transform.rotation = Quaternion.identity;
        
        // Scale'i sıfırla (parent transform'dan etkilenmiş olabilir)
        weapon.transform.localScale = Vector3.one;
        
        // Physics'i yeniden etkinleştir
        Rigidbody2D itemRb = weapon.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            itemRb.angularVelocity = 0f;
            itemRb.linearVelocity = Vector2.zero;
            itemRb.linearDamping = 10f;
            itemRb.angularDamping = 10f;
        }
        
        // Collider'ı yeniden etkinleştir
        Collider2D itemCollider = weapon.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
        
        // SpriteRenderer'ı kontrol et ve görünür yap
        SpriteRenderer itemRenderer = weapon.GetComponent<SpriteRenderer>();
        if (itemRenderer != null)
        {
            itemRenderer.enabled = true;
            // Alpha değerini kontrol et
            Color color = itemRenderer.color;
            if (color.a < 1f)
            {
                color.a = 1f;
                itemRenderer.color = color;
            }
        }
        
        // PickupableItem component'ini yeniden etkinleştir
        PickupableItem pickupable = weapon.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.enabled = true;
            pickupable.SetHeldState(false); // Mark as not held anymore
            pickupable.ResetPickupState();
        }
    }
    
    /// <summary>
    /// Enemy elindeki item'ı düşürür
    /// </summary>
    public void DropItem(ItemHoldType holdType = ItemHoldType.Primary)
    {
        GameObject itemToDrop = null;
        
        // Hangi item'ın düşürüleceğini bul
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
        
        DropCurrentWeaponAtPosition(itemToDrop, transform.position);
    }
    
    /// <summary>
    /// Enemy elindeki tüm item'ları düşürür
    /// </summary>
    public void DropAllItems()
    {
        if (currentPrimaryItem != null)
        {
            DropCurrentWeaponAtPosition(currentPrimaryItem, transform.position);
            currentPrimaryItem = null;
        }
        
        if (currentSecondaryItem != null)
        {
            DropCurrentWeaponAtPosition(currentSecondaryItem, transform.position);
            currentSecondaryItem = null;
        }
    }
    
    #endregion
    
    #region Attack System
    
    /// <summary>
    /// Enemy elindeki silahla saldırır
    /// </summary>
    public void PerformAttack(Vector3 targetPosition)
    {
        if (!canUseItems) return;
        
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon == null) return;
        
        // Weapon type'ına göre attack gerçekleştir
        PickupableItem weaponPickup = currentWeapon.GetComponent<PickupableItem>();
        if (weaponPickup == null) return;
        
        // Target'a doğru yön hesapla
        Vector2 direction = (targetPosition - transform.position).normalized;
        
        if (weaponPickup.holdType == ItemHoldType.Primary)
        {
            // Primary weapon - Projectile attack
            PerformProjectileAttack(currentWeapon, direction);
        }
        else if (weaponPickup.holdType == ItemHoldType.Secondary)
        {
            // Secondary weapon - Melee attack
            PerformMeleeAttack(currentWeapon, direction);
        }
    }
    
    /// <summary>
    /// Projectile (mermili) saldırı gerçekleştirir
    /// </summary>
    private void PerformProjectileAttack(GameObject weapon, Vector2 direction)
    {
        PistolItem pistol = weapon.GetComponent<PistolItem>();
        if (pistol != null)
        {
            pistol.Fire(transform.position, direction);
        }
    }
    
    /// <summary>
    /// Melee (yakın dövüş) saldırısı gerçekleştirir
    /// </summary>
    private void PerformMeleeAttack(GameObject weapon, Vector2 direction)
    {
        // Melee attack yaparken enemy'yi hedef yönüne döndür
        if (enemyAI != null && direction.magnitude > 0.01f)
        {
            enemyAI.SetVisionDirection(direction.normalized);
        }
        
        BaseballBatItem bat = weapon.GetComponent<BaseballBatItem>();
        if (bat != null)
        {
            bat.Attack(transform.position, direction);
        }
    }
    
    #endregion
    
    #region Item Throwing System
    
    /// <summary>
    /// Enemy elindeki item'ı belirtilen yöne fırlatır
    /// </summary>
    public void ThrowCurrentItem(Vector2 direction)
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
        
        // Item'ı fırlat
        ThrowItemAtDirection(currentWeapon, direction);
        
        // Item fırlatıldıktan sonra enemy'nin elinden çıkar
        if (currentPrimaryItem == currentWeapon)
            currentPrimaryItem = null;
        else if (currentSecondaryItem == currentWeapon)
            currentSecondaryItem = null;
    }
    
    /// <summary>
    /// Item'ı belirtilen yöne fiziksel olarak fırlatır
    /// </summary>
    private void ThrowItemAtDirection(GameObject item, Vector2 direction)
    {
        // Item'ı parent'tan ayır
        item.transform.SetParent(null);
        
        // Item'ın pozisyonunu enemy'nin mevcut pozisyonuna ayarla
        item.transform.position = transform.position;
        
        // Physics'i etkinleştir
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            itemRb.angularVelocity = 0f;
            
            itemRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            itemRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            if (itemRb.gravityScale == 0)
                itemRb.gravityScale = 0;
            
            itemRb.linearDamping = 0f;
            itemRb.angularDamping = 0.05f;
            
            itemRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            // Belirtilen yöne kuvvet uygula
            itemRb.linearVelocity = direction.normalized * throwForce;
        }
        
        // Collider'ı yeniden etkinleştir
        Collider2D itemCollider = item.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
            itemCollider.isTrigger = false;
        }
        
        // ThrownItem component'i ekle
        ThrownItem thrownComponent = item.GetComponent<ThrownItem>();
        if (thrownComponent == null)
        {
            thrownComponent = item.AddComponent<ThrownItem>();
        }
        thrownComponent.Initialize();
        thrownComponent.SetThrower(gameObject); // Fırlatan enemy'yi set et
        
        // PickupableItem component'ini yeniden etkinleştir
        PickupableItem pickupable = item.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.enabled = true;
            pickupable.ResetPickupState();
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Enemy'nin elinde silah var mı?
    /// </summary>
    public bool HasWeapon()
    {
        return currentPrimaryItem != null || currentSecondaryItem != null;
    }
    
    /// <summary>
    /// Enemy'nin şu anda tuttuğu silahı döndürür
    /// </summary>
    public GameObject GetCurrentWeapon()
    {
        if (currentPrimaryItem != null)
            return currentPrimaryItem;
        else if (currentSecondaryItem != null)
            return currentSecondaryItem;
        else
            return null;
    }
    
    /// <summary>
    /// Belirtilen slot'ta item var mı?
    /// </summary>
    public bool HasItemInSlot(ItemHoldType slotType)
    {
        if (slotType == ItemHoldType.Primary)
            return currentPrimaryItem != null;
        else
            return currentSecondaryItem != null;
    }
    
    /// <summary>
    /// Belirtilen slot'taki item'ı döndürür
    /// </summary>
    public GameObject GetItemInSlot(ItemHoldType slotType)
    {
        if (slotType == ItemHoldType.Primary)
            return currentPrimaryItem;
        else
            return currentSecondaryItem;
    }
    
    #endregion
}
