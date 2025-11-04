using UnityEngine;

public class TestPickupItem : MonoBehaviour
{
    [Header("Test Item Settings")]
    public string itemName = "Test Item";
    public Color itemColor = Color.cyan;
    public ItemHoldType testHoldType = ItemHoldType.Primary;
    
    private void Start()
    {
        // Set the item's color
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = itemColor;
        }
        
        // Add the PickupableItem component if it doesn't exist
        PickupableItem pickupable = GetComponent<PickupableItem>();
        if (pickupable == null)
        {
            pickupable = gameObject.AddComponent<PickupableItem>();
        }
        
        pickupable.itemName = itemName;
        pickupable.holdType = testHoldType;
    }
}