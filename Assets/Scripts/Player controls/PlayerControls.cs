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
    
    [Header("Item Pickup System")]
    public Transform primaryHoldPosition;   // Önde tutulacak eşyalar için (tabanca)
    public Transform secondaryHoldPosition; // Yanda tutulacak eşyalar için (beyzbol sopası)
    public GameObject currentPrimaryItem;   // Şu anda önde tutulan eşya
    public GameObject currentSecondaryItem; // Şu anda yanda tutulan eşya
    
    private void Start()
    {
        // Store original speed for speed boost functionality
        originalMoveSpeed = moveSpeed;
    }
    
    private void Update()
    {
        ProcessInput();
        
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector2 direction = new Vector2(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y);
        
        transform.up = direction;
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
            
            Debug.Log($"Player speed boosted! Original: {originalMoveSpeed}, Multiplier: {multiplier}");
        }
    }
    
    public void RemoveSpeedBoost()
    {
        if (isSpeedBoosted)
        {
            speedBoostMultiplier = 1f;
            isSpeedBoosted = false;
            
            Debug.Log($"Player speed boost removed. Speed restored to: {originalMoveSpeed}");
        }
    }
    
    public bool IsSpeedBoosted => isSpeedBoosted;
    
    #endregion
    
    #region Item Pickup System
    
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
            // Optional: Add small random velocity to prevent stacking
            itemRb.linearVelocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
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
}
