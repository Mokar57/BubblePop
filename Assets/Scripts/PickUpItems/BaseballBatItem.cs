using UnityEngine;

public class BaseballBatItem : MonoBehaviour
{
    [Header("Baseball Bat Settings")]
    public string batName = "Baseball Bat";
    public Color batColor = Color.brown;
    
    private void Start()
    {
        // Set the bat's color
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = batColor;
        }
        
        // Add the PickupableItem component if it doesn't exist
        PickupableItem pickupable = GetComponent<PickupableItem>();
        if (pickupable == null)
        {
            pickupable = gameObject.AddComponent<PickupableItem>();
        }
        
        // Configure for secondary hold (side position)
        pickupable.itemName = batName;
        pickupable.holdType = ItemHoldType.Secondary;
    }
}