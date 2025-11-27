using UnityEngine;

/// <summary>
/// Handles enemy's ability to hold and use PickupableItems
/// Implements IItemHolder interface for consistency with player system
/// </summary>
public class EnemyWeaponController : MonoBehaviour, IItemHolder
{
    [Header("Item Hold Positions")]
    [Tooltip("Hold position for primary items (pistol)")]
    public Transform primaryHoldPosition;
    
    [Tooltip("Hold position for secondary items (baseball bat)")]
    public Transform secondaryHoldPosition;
    
    [Header("Current Items")]
    public GameObject currentPrimaryItem;
    public GameObject currentSecondaryItem;
    
    [Header("Item Throwing Settings")]
    public float throwForce = 10f;
    
    [Header("Attack Settings")]
    [Tooltip("Can this enemy use items to attack?")]
    public bool canUseItems = true;
    
    private EnemyAI enemyAI;
    
    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    #region IItemHolder Implementation
    
    /// <summary>
    /// Get the transform position where items should be held
    /// </summary>
    public Transform GetHoldPosition()
    {
        // Return primary hold position, or secondary if primary is null
        return primaryHoldPosition != null ? primaryHoldPosition : secondaryHoldPosition;
    }

    /// <summary>
    /// Get the team of this entity
    /// </summary>
    public Team GetTeam()
    {
        return Team.Enemy;
    }

    /// <summary>
    /// Called when an item is picked up
    /// </summary>
    public void OnItemPickedUp(GameObject item)
    {
        // Trigger game event
        GameEvents.TriggerItemPickedUp(item, gameObject);
    }

    /// <summary>
    /// Called when an item is dropped
    /// </summary>
    public void OnItemDropped(GameObject item)
    {
        // Trigger game event
        GameEvents.TriggerItemDropped(item, gameObject);
    }

    #endregion
    
    #region Item Pickup System
    
    /// <summary>
    /// Picks up an item and assigns it to the appropriate slot
    /// </summary>
    public void PickupItem(GameObject item, ItemHoldType holdType = ItemHoldType.Primary)
    {
        if (item == null) return;
        
        // Drop current weapon if holding one
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon != null)
        {
            DropCurrentWeaponAtPosition(currentWeapon, transform.position);
        }
        
        // Assign new item to appropriate slot
        if (holdType == ItemHoldType.Primary)
        {
            currentPrimaryItem = item;
            currentSecondaryItem = null;
        }
        else
        {
            currentSecondaryItem = item;
            currentPrimaryItem = null;
        }
        
        // Parent item to enemy
        item.transform.SetParent(transform);
        
        // Update held state
        PickupableItem pickupable = item.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.SetHeldState(true);
        }
        
        // Preserve sprite properties
        SpriteRenderer itemRenderer = item.GetComponent<SpriteRenderer>();
        if (itemRenderer != null)
        {
            Color originalColor = itemRenderer.color;
            if (originalColor.a > 0)
            {
                itemRenderer.color = originalColor;
            }
        }
        
        // Position item at hold position
        Transform targetPosition = (holdType == ItemHoldType.Primary) ? primaryHoldPosition : secondaryHoldPosition;
        
        if (targetPosition != null)
        {
            item.transform.position = targetPosition.position;
            item.transform.rotation = targetPosition.rotation;
        }
        else
        {
            // Default positions
            if (holdType == ItemHoldType.Primary)
            {
                item.transform.localPosition = new Vector3(1f, 0f, 0f);
            }
            else
            {
                item.transform.localPosition = new Vector3(0f, 1f, 0f);
            }
        }
        
        // Disable physics
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Disable collider
        Collider2D itemCollider = item.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        
        // Disable PickupableItem component
        if (pickupable != null)
        {
            pickupable.enabled = false;
        }
        
        // Trigger pickup event
        OnItemPickedUp(item);
    }
    
    /// <summary>
    /// Drops a weapon at specified position
    /// </summary>
    private void DropCurrentWeaponAtPosition(GameObject weapon, Vector3 dropPosition)
    {
        if (weapon == null) return;
        
        // Unparent
        weapon.transform.SetParent(null);
        weapon.transform.position = dropPosition;
        weapon.transform.rotation = Quaternion.identity;
        weapon.transform.localScale = Vector3.one;
        
        // Re-enable physics
        Rigidbody2D itemRb = weapon.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            itemRb.angularVelocity = 0f;
            itemRb.linearVelocity = Vector2.zero;
            itemRb.linearDamping = 10f;
            itemRb.angularDamping = 10f;
        }
        
        // Re-enable collider
        Collider2D itemCollider = weapon.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
        
        // Re-enable sprite renderer
        SpriteRenderer itemRenderer = weapon.GetComponent<SpriteRenderer>();
        if (itemRenderer != null)
        {
            itemRenderer.enabled = true;
            Color color = itemRenderer.color;
            if (color.a < 1f)
            {
                color.a = 1f;
                itemRenderer.color = color;
            }
        }
        
        // Re-enable PickupableItem component
        PickupableItem pickupable = weapon.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.enabled = true;
            pickupable.SetHeldState(false);
            pickupable.ResetPickupState();
        }
        
        // Trigger drop event
        OnItemDropped(weapon);
    }
    
    /// <summary>
    /// Drops current item
    /// </summary>
    public void DropItem(ItemHoldType holdType = ItemHoldType.Primary)
    {
        GameObject itemToDrop = null;
        
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
        
        if (itemToDrop != null)
        {
            DropCurrentWeaponAtPosition(itemToDrop, transform.position);
        }
    }
    
    /// <summary>
    /// Drops all held items
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
    /// Performs attack with current weapon
    /// </summary>
    public void PerformAttack(Vector3 targetPosition)
    {
        if (!canUseItems) return;
        
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon == null) return;
        
        PickupableItem weaponPickup = currentWeapon.GetComponent<PickupableItem>();
        if (weaponPickup == null) return;
        
        Vector2 direction = (targetPosition - transform.position).normalized;
        
        if (weaponPickup.holdType == ItemHoldType.Primary)
        {
            PerformProjectileAttack(currentWeapon, direction);
        }
        else if (weaponPickup.holdType == ItemHoldType.Secondary)
        {
            PerformMeleeAttack(currentWeapon, direction);
        }
    }
    
    /// <summary>
    /// Performs projectile attack (ranged weapons)
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
    /// Performs melee attack
    /// </summary>
    private void PerformMeleeAttack(GameObject weapon, Vector2 direction)
    {
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
    /// Throws current item in specified direction
    /// </summary>
    public void ThrowCurrentItem(Vector2 direction)
    {
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon == null) return;
        
        PickupableItem pickupable = currentWeapon.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.PlayThrowSound();
            
            // Deplete the weapon
            while (!pickupable.IsDepleted())
            {
                pickupable.DecreaseUsage();
            }
        }
        
        ThrowItemAtDirection(currentWeapon, direction);
        
        // Clear from slots
        if (currentPrimaryItem == currentWeapon)
            currentPrimaryItem = null;
        else if (currentSecondaryItem == currentWeapon)
            currentSecondaryItem = null;
    }
    
    /// <summary>
    /// Physically throws item in direction
    /// </summary>
    private void ThrowItemAtDirection(GameObject item, Vector2 direction)
    {
        item.transform.SetParent(null);
        item.transform.position = transform.position;
        
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();
        if (itemRb != null)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            itemRb.angularVelocity = 0f;
            itemRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            itemRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            itemRb.linearDamping = 0f;
            itemRb.angularDamping = 0.05f;
            itemRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            itemRb.linearVelocity = direction.normalized * throwForce;
        }
        
        Collider2D itemCollider = item.GetComponent<Collider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
            itemCollider.isTrigger = false;
        }
        
        ThrownItem thrownComponent = item.GetComponent<ThrownItem>();
        if (thrownComponent == null)
        {
            thrownComponent = item.AddComponent<ThrownItem>();
        }
        thrownComponent.Initialize();
        thrownComponent.SetThrower(gameObject);
        
        PickupableItem pickupable = item.GetComponent<PickupableItem>();
        if (pickupable != null)
        {
            pickupable.enabled = true;
            pickupable.ResetPickupState();
        }
        
        OnItemDropped(item);
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Check if enemy has any weapon
    /// </summary>
    public bool HasWeapon()
    {
        return currentPrimaryItem != null || currentSecondaryItem != null;
    }
    
    /// <summary>
    /// Get current weapon (primary or secondary)
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
    /// Check if item exists in specific slot
    /// </summary>
    public bool HasItemInSlot(ItemHoldType slotType)
    {
        if (slotType == ItemHoldType.Primary)
            return currentPrimaryItem != null;
        else
            return currentSecondaryItem != null;
    }
    
    /// <summary>
    /// Get item in specific slot
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
