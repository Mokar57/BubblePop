using UnityEngine;

/// <summary>
/// Base class for any item that can be picked up by an IItemHolder
/// Handles detection, input, and visual state switching
/// </summary>
public class PickupableItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public string itemName = "Item";
    // public ItemHoldType holdType = ItemHoldType.Melee; // Removed as per request, but base class might need it?
    // Actually, PickupableItem is base for Weapon. If Weapon uses WeaponDataSO, PickupableItem might not know about it.
    // But user said "Weapon scripts doesnt need them".
    // Let's make holdType a virtual property that Weapon overrides.
    public virtual ItemHoldType HoldType => ItemHoldType.Melee;

    public float pickupRadius = 1.5f;
    public KeyCode pickupKey = KeyCode.E;
    public bool canBePickedByEnemies = true;

    [Header("Visuals")]
    public GameObject pickupIndicator; // UI hint

    protected bool isHeld = false;
    public bool IsHeld => isHeld;

    protected Collider2D itemCollider;
    protected Rigidbody2D itemRb;
    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        itemCollider = GetComponent<Collider2D>();
        itemRb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        if (isHeld) return;

        // Input handling for pickup could go here if we want the item to listen for input
        // But usually the PlayerController handles the "E" press and looks for items.
        // We'll leave this empty for now or add a simple check if needed.
        if (Input.GetKeyDown(pickupKey) && pickupIndicator != null && pickupIndicator.activeSelf)
        {
            // Find player and try to pickup?
            // Better to let PlayerController initiate this.
        }
    }

    // Called by Player/Enemy when they want to pick this up
    public virtual bool TryPickup(IItemHolder holder)
    {
        if (!enabled) return false; // Cannot pickup if component is disabled
        if (isHeld) return false;

        // Check team restrictions if any
        if (!canBePickedByEnemies && holder.GetTeam() == Team.Enemy) return false;

        OnPickedUp(holder);
        return true;
    }

    public virtual void OnPickedUp(IItemHolder holder)
    {
        isHeld = true;

        // Disable physics
        if (itemCollider) itemCollider.enabled = false;
        if (itemRb) itemRb.simulated = false;

        // Visuals
        if (pickupIndicator) pickupIndicator.SetActive(false);

        // Parent to holder
        transform.SetParent(holder.GetHoldPosition(HoldType));
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Notify holder
        holder.OnItemPickedUp(gameObject);
    }

    public virtual void OnDropped()
    {
        isHeld = false;

        // Enable physics
        if (itemCollider) itemCollider.enabled = true;
        if (itemRb) itemRb.simulated = true;

        // Unparent
        transform.SetParent(null);

        // Reset rotation (optional, maybe lay flat)
        // transform.rotation = Quaternion.identity; 
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHeld) return;

        if (other.CompareTag("Player")) // Or check component
        {
            if (pickupIndicator) pickupIndicator.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isHeld) return;

        if (other.CompareTag("Player"))
        {
            if (pickupIndicator) pickupIndicator.SetActive(false);
        }
    }
}
