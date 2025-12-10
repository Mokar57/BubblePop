using UnityEngine;
using System.Collections;

/// <summary>
/// Base class for any item that can be picked up by an IItemHolder
/// Handles pickup logic, physics state, and visual indicators
/// Input is handled by PlayerControls via InputManager (not by this class)
/// </summary>
public class PickupableItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public string itemName = "Item";

    [Tooltip("Virtual property - Weapon class overrides this using WeaponDataSO")]
    public virtual ItemHoldType HoldType => ItemHoldType.Melee;

    [Tooltip("Detection radius for pickup indicator (used by trigger collider)")]
    public float pickupRadius = 1.5f;

    [Tooltip("Can enemies pick up this item?")]
    public bool canBePickedByEnemies = true;

    [Header("Visuals")]
    [Tooltip("UI indicator shown when player is in range (optional)")]
    public GameObject pickupIndicator;

    [Header("Melee Rotation Animation")]
    [Tooltip("Enable rotation animation for melee items during attack")]
    public bool enableMeleeRotation = true;

    [Tooltip("Rotation angle in degrees (positive = clockwise, negative = counter-clockwise)")]
    public float rotationAngle = 90f;

    [Tooltip("Duration of the rotation animation in seconds")]
    public float rotationDuration = 0.3f;

    [Tooltip("Time to wait before returning to original position in seconds")]
    public float rotationHoldTime = 0.1f;

    protected bool isHeld = false;
    public bool IsHeld => isHeld;

    protected Collider2D itemCollider;
    protected Rigidbody2D itemRb;
    protected SpriteRenderer spriteRenderer;
    private Coroutine rotationCoroutine;

    // Layer management
    private const int PLAYER_LAYER = 8;
    private const int ENEMY_LAYER = 13;

    protected virtual void Awake()
    {
        itemCollider = GetComponent<Collider2D>();
        itemRb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Set initial exclude layers (on ground state)
        SetGroundExcludeLayers();
    }

    /// <summary>
    /// Sets exclude layers for ground state (excludes Player and Enemy)
    /// </summary>
    protected void SetGroundExcludeLayers()
    {
        if (itemCollider != null)
        {
            itemCollider.excludeLayers = (1 << PLAYER_LAYER) | (1 << ENEMY_LAYER);
        }
    }

    /// <summary>
    /// Sets exclude layers for held state (no exclusions - can hit everyone)
    /// </summary>
    protected void SetHeldExcludeLayers()
    {
        if (itemCollider != null)
        {
            itemCollider.excludeLayers = 0; // Nothing excluded
        }
    }

    /// <summary>
    /// Called by PlayerControls or EnemyWeaponController when attempting to pick up this item
    /// Returns true if pickup was successful
    /// </summary>
    public virtual bool TryPickup(IItemHolder holder)
    {
        if (!enabled) return false;
        if (isHeld) return false;

        // Team-based pickup restrictions
        if (!canBePickedByEnemies && holder.GetTeam() == Team.Enemy)
        {
            return false;
        }

        OnPickedUp(holder);
        return true;
    }

    /// <summary>
    /// Called when item is successfully picked up
    /// Disables physics, parents to holder, and updates visual state
    /// </summary>
    public virtual void OnPickedUp(IItemHolder holder)
    {
        isHeld = true;

        // Set to kinematic so collider follows transform (for weapon hits)
        // Collider is NOT disabled - will be managed by Weapon class for trigger mode
        if (itemRb)
        {
            itemRb.bodyType = RigidbodyType2D.Kinematic;
            itemRb.linearVelocity = Vector2.zero;
            itemRb.angularVelocity = 0f;
        }

        // Hide pickup indicator
        if (pickupIndicator) pickupIndicator.SetActive(false);

        // Parent to holder's hand position
        transform.SetParent(holder.GetHoldPosition(HoldType));
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Set exclude layers for held state (can hit enemies/players)
        SetHeldExcludeLayers();

        // Notify holder that pickup was successful
        holder.OnItemPickedUp(gameObject);
    }

    /// <summary>
    /// Called when item is dropped by holder
    /// Re-enables physics and unparents from holder
    /// </summary>
    public virtual void OnDropped()
    {
        isHeld = false;

        // Stop any active rotation animation when dropped
        StopRotationAnimation();

        // Re-enable physics as Dynamic
        // Collider enable/disable is managed by Weapon class for trigger mode
        if (itemRb)
        {
            itemRb.bodyType = RigidbodyType2D.Dynamic;
        }

        // Unparent from holder
        transform.SetParent(null);

        // Restore exclude layers for ground state
        SetGroundExcludeLayers();
    }

    /// <summary>
    /// Shows pickup indicator when player enters trigger radius
    /// Requires CircleCollider2D with "Is Trigger" enabled on this GameObject
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHeld) return;

        if (other.CompareTag("Player"))
        {
            if (pickupIndicator) pickupIndicator.SetActive(true);
        }
    }

    /// <summary>
    /// Hides pickup indicator when player exits trigger radius
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (isHeld) return;

        if (other.CompareTag("Player"))
        {
            if (pickupIndicator) pickupIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Triggers the rotation animation for melee items during attack
    /// Called by Weapon class when attacking
    /// </summary>
    public void PlayAttackRotation()
    {
        if (!isHeld) return;
        if (HoldType != ItemHoldType.Melee) return;
        if (!enableMeleeRotation) return;
        
        StopRotationAnimation();
        rotationCoroutine = StartCoroutine(AttackRotationCoroutine());
    }

    /// <summary>
    /// Stops the rotation animation
    /// </summary>
    public void StopRotationAnimation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that handles the attack rotation animation
    /// Rotates to target angle, holds briefly, then returns to original position
    /// </summary>
    private IEnumerator AttackRotationCoroutine()
    {
        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, rotationAngle);
        float elapsedTime = 0f;

        // Rotate to target angle
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationDuration;
            // Use smoothstep for smoother animation
            t = t * t * (3f - 2f * t);
            transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.localRotation = targetRotation;

        // Hold at target angle
        if (rotationHoldTime > 0)
        {
            yield return new WaitForSeconds(rotationHoldTime);
        }

        // Return to original angle
        elapsedTime = 0f;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationDuration;
            // Use smoothstep for smoother animation
            t = t * t * (3f - 2f * t);
            transform.localRotation = Quaternion.Lerp(targetRotation, startRotation, t);
            yield return null;
        }

        transform.localRotation = startRotation;
        rotationCoroutine = null;
    }

}
