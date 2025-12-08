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

    protected virtual void Awake()
    {
        itemCollider = GetComponent<Collider2D>();
        itemRb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        // Disable physics
        if (itemCollider) itemCollider.enabled = false;
        if (itemRb) itemRb.simulated = false;

        // Hide pickup indicator
        if (pickupIndicator) pickupIndicator.SetActive(false);

        // Parent to holder's hand position
        transform.SetParent(holder.GetHoldPosition(HoldType));
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

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

        // Re-enable physics
        if (itemCollider) itemCollider.enabled = true;
        if (itemRb) itemRb.simulated = true;

        // Unparent from holder
        transform.SetParent(null);
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
    protected void StopRotationAnimation()
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
