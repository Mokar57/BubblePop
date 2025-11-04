using UnityEngine;

public class PistolItem : MonoBehaviour
{
    [Header("Pistol Settings")]
    public string pistolName = "Pistol";
    public Color pistolColor = Color.black;
    
    private void Start()
    {
        // Set the pistol's color
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = pistolColor;
        }
        
        // Add the PickupableItem component if it doesn't exist
        PickupableItem pickupable = GetComponent<PickupableItem>();
        if (pickupable == null)
        {
            pickupable = gameObject.AddComponent<PickupableItem>();
        }
        
        // Configure for primary hold (front position)
        pickupable.itemName = pistolName;
        pickupable.holdType = ItemHoldType.Primary;
    }
}