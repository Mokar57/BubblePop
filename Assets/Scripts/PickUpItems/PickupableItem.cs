using UnityEngine;

public enum ItemHoldType
{
    Primary,    // Önde tutulacak (tabanca için)
    Secondary   // Yanda tutulacak (beyzbol sopası için)
}

public class PickupableItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public string itemName = "Item";
    public float pickupRadius = 2f;
    public KeyCode pickupKey = KeyCode.E;
    public ItemHoldType holdType = ItemHoldType.Primary;
    
    [Header("Visual Settings")]
    public GameObject pickupIndicator; // UI element to show "Press E to pick up"
    
    private bool playerInRange = false;
    private GameObject player;

    private void Start()
    {
        // Hide pickup indicator at start
        if (pickupIndicator != null)
            pickupIndicator.SetActive(false);
    }

    private void Update()
    {
        // Check if item is currently being held by checking if parent is not null
        bool isCurrentlyHeld = transform.parent != null;
        
        if (playerInRange && !isCurrentlyHeld && Input.GetKeyDown(pickupKey))
        {
            PickupItem();
        }
        
        // Fallback distance check if trigger doesn't work
        CheckPlayerDistance();
    }
    
    private void CheckPlayerDistance()
    {
        // Find player by tag or name if not already found
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer == null)
            {
                foundPlayer = GameObject.Find("Player");
            }
            
            if (foundPlayer != null)
            {
                float distance = Vector2.Distance(transform.position, foundPlayer.transform.position);
                
                if (distance <= pickupRadius && !playerInRange)
                {
                    playerInRange = true;
                    player = foundPlayer;
                }
                else if (distance > pickupRadius && playerInRange)
                {
                    playerInRange = false;
                    player = null;
                }
            }
        }
        else
        {
            // Check distance to current player
            float distance = Vector2.Distance(transform.position, player.transform.position);
            
            if (distance > pickupRadius && playerInRange)
            {
                playerInRange = false;
                player = null;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            playerInRange = true;
            player = other.gameObject;
            
            if (pickupIndicator != null)
                pickupIndicator.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            playerInRange = false;
            player = null;
            
            if (pickupIndicator != null)
                pickupIndicator.SetActive(false);
        }
    }

    private void PickupItem()
    {
        if (player != null)
        {
            PlayerControls playerControls = player.GetComponent<PlayerControls>();
            if (playerControls != null)
            {
                playerControls.PickupItem(this.gameObject, holdType);
                
                if (pickupIndicator != null)
                    pickupIndicator.SetActive(false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup radius in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
    
    // Public method to reset pickup state when item is dropped
    public void ResetPickupState()
    {
        playerInRange = false;
        player = null;
        
        if (pickupIndicator != null)
            pickupIndicator.SetActive(false);
    }
}