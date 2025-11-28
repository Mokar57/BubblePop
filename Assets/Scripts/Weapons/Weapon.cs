using UnityEngine;

/// <summary>
/// Abstract base class for all weapons
/// Handles common functionality like durability, throwing, and data initialization
/// </summary>
public abstract class Weapon : PickupableItem
{
    [Header("Weapon Data")]
    [SerializeField] protected WeaponDataSO weaponData;

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
        holdType = weaponData.holdType;
        currentDurability = weaponData.maxDurability;

        // Update visuals from data
        if (GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            sr.sprite = weaponData.droppedSprite;
        }
    }

    /// <summary>
    /// Called when weapon is picked up
    /// </summary>
    public override void OnPickedUp(IItemHolder holder)
    {
        base.OnPickedUp(holder);
        currentHolder = holder;

        if (GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            sr.sprite = weaponData.heldSprite;
        }
    }

    /// <summary>
    /// Called when weapon is dropped
    /// </summary>
    public override void OnDropped()
    {
        base.OnDropped();
        currentHolder = null;

        if (GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            sr.sprite = weaponData.droppedSprite;
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

    protected virtual void OnWeaponBroken()
    {
        // Visual feedback for broken weapon
        if (weaponData.depletedSprite != null && GetComponent<SpriteRenderer>() is SpriteRenderer sr)
        {
            sr.sprite = weaponData.depletedSprite;
        }

        // Optional: Auto-drop or disable?
    }

    /// <summary>
    /// Throw the weapon
    /// </summary>
    public void Throw(Vector2 direction)
    {
        // Cache thrower before dropping (which clears currentHolder)
        IItemHolder thrower = currentHolder;
        GameObject throwerObj = (thrower as MonoBehaviour)?.gameObject;

        if (thrower != null)
        {
            thrower.OnItemDropped(gameObject);
        }

        // Detach physically (unparent, enable physics)
        OnDropped();

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
        if (weaponData.throwSound != null)
        {
            AudioSource.PlayClipAtPoint(weaponData.throwSound, transform.position, weaponData.throwSoundVolume);
        }
    }
}
