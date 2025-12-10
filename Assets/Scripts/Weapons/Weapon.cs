using UnityEngine;

/// <summary>
/// Abstract base class for all weapons
/// Handles common functionality like durability, throwing, and data initialization
/// </summary>
public abstract class Weapon : PickupableItem
{
    [Header("Weapon Data")]
    [SerializeField] protected WeaponDataSO weaponData;
    public WeaponDataSO Data => weaponData;

    public override ItemHoldType HoldType => weaponData != null ? weaponData.holdType : base.HoldType;

    protected int currentDurability;
    protected IItemHolder currentHolder;

    public bool IsBroken => currentDurability <= 0;

    protected virtual void Start()
    {
        if (weaponData != null)
        {
            Initialize(weaponData);
        }
    }

    /// <summary>
    /// Initialize weapon with data from ScriptableObject
    /// </summary>
    public virtual void Initialize(WeaponDataSO data)
    {
        weaponData = data;
        // holdType is now accessed directly from weaponData via property if needed, 
        // but we keep the field for now if other scripts rely on it, or remove it if requested.
        // User requested: "WeaponDataSO has hold type so Weapon scripts doesnt need them"
        // So we will remove the assignment here and rely on the property.
        // holdType = weaponData.holdType; // Removed

        currentDurability = weaponData.maxDurability;

        // Update visuals from data
        if (GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            sr.sprite = weaponData.droppedSprite;
        }
    }

    /// <summary>
    /// Attempt to pick up the weapon. Returns false if broken.
    /// </summary>
    public override bool TryPickup(IItemHolder holder)
    {
        if (IsBroken)
        {
            // Optional: Play a "broken" sound or show a message?
            return false;
        }
        return base.TryPickup(holder);
    }

    /// <summary>
    /// Called when weapon is picked up
    /// </summary>
    public override void OnPickedUp(IItemHolder holder)
    {
        base.OnPickedUp(holder);
        currentHolder = holder;

        // Enable trigger mode for collision detection during attacks
        if (itemCollider) itemCollider.isTrigger = true;

        if (GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            if (currentDurability <= 0 && weaponData.depletedSprite != null)
            {
                sr.sprite = weaponData.depletedSprite;
            }
            else
            {
                sr.sprite = weaponData.heldSprite;
            }
        }
    }

    /// <summary>
    /// Called when weapon is dropped
    /// </summary>
    public override void OnDropped()
    {
        base.OnDropped();
        currentHolder = null;

        // Disable trigger mode when dropped (becomes solid physics object)
        if (itemCollider) itemCollider.isTrigger = false;

        if (GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            if (currentDurability <= 0 && weaponData.depletedSprite != null)
            {
                sr.sprite = weaponData.depletedSprite;
            }
            else
            {
                sr.sprite = weaponData.droppedSprite;
            }
        }
    }

    /// <summary>
    /// Attempt to attack with the weapon
    /// </summary>
    public void TryAttack()
    {
        if (currentDurability <= 0)
        {
            Debug.Log($"{name}: Cannot attack, durability is 0");
            return;
        }

        // Check cooldowns if necessary (handled by subclasses or here)
        PerformAttack();
    }

    /// <summary>
    /// Implementation of the specific attack logic
    /// </summary>
    protected abstract void PerformAttack();

    /// <summary>
    /// Reduce durability and check for breakage
    /// </summary>
    protected void ReduceDurability(int amount = 1)
    {
        currentDurability -= amount;
        if (currentDurability <= 0)
        {
            currentDurability = 0;
            OnWeaponBroken();
        }
    }

    /// <summary>
    /// Force the weapon to break (e.g. on throw impact)
    /// </summary>
    public void ForceBreak()
    {
        currentDurability = 0;
        OnWeaponBroken();
    }

    protected virtual void OnWeaponBroken()
    {
        // Play break sound
        if (weaponData.breakSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(weaponData.breakSound, weaponData.breakSoundVolume);
        }

        // Visual feedback for broken weapon
        if (weaponData.depletedSprite != null && GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            sr.sprite = weaponData.depletedSprite;
        }

        // Optional: Auto-drop or disable?
    }

    /// <summary>
    /// Drop the weapon without throwing it (e.g. when stunned)
    /// </summary>
    public void Drop()
    {
        // Cache holder
        IItemHolder holder = currentHolder;

        if (holder != null)
        {
            holder.OnItemDropped(gameObject);
        }

        // Detach (OnDropped will disable trigger mode)
        OnDropped();

        // Ensure physics state for ground item
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.linearDamping = 5f; // High drag to stop quickly
        }
    }

    /// <summary>
    /// Throw the weapon
    /// </summary>
    public void Throw(Vector2 direction)
    {
        // Stop any active rotation animation to prevent visual glitches
        StopRotationAnimation();

        // Cache thrower before dropping (which clears currentHolder)
        IItemHolder thrower = currentHolder;
        GameObject throwerObj = (thrower as MonoBehaviour)?.gameObject;

        if (thrower != null)
        {
            thrower.OnItemDropped(gameObject);
        }

        // Detach physically (unparent, enable physics)
        OnDropped();

        // Ensure collider is solid for physics interactions (bouncing/damage)
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = false;
        }

        // Add ThrownItem component logic
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(direction * weaponData.throwForce, ForceMode2D.Impulse);
        }

        ThrownItem thrown = gameObject.AddComponent<ThrownItem>();
        thrown.Initialize(weaponData);
        if (throwerObj != null)
        {
            thrown.SetThrower(throwerObj);
        }

        // Play throw sound
        if (weaponData.throwSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(weaponData.throwSound, weaponData.throwSoundVolume);
        }
    }
}
