using UnityEngine;

/// <summary>
/// Debug script - Collider çarpışmalarını test etmek için
/// PickupableItem'a geçici olarak ekle
/// </summary>
public class DebugCollision : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[{gameObject.name}] COLLISION with: {collision.gameObject.name} | Layer: {LayerMask.LayerToName(collision.gameObject.layer)} | Tag: {collision.gameObject.tag}");
        
        // Collider bilgilerini göster
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider != null)
        {
            Debug.Log($"  → My Collider: IsTrigger={myCollider.isTrigger}, Enabled={myCollider.enabled}");
        }
        
        Rigidbody2D myRb = GetComponent<Rigidbody2D>();
        if (myRb != null)
        {
            Debug.Log($"  → My Rigidbody: BodyType={myRb.bodyType}, CollisionDetection={myRb.collisionDetectionMode}");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[{gameObject.name}] TRIGGER with: {other.gameObject.name} | Layer: {LayerMask.LayerToName(other.gameObject.layer)} | Tag: {other.gameObject.tag}");
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        Debug.LogWarning($"[{gameObject.name}] Still colliding with: {collision.gameObject.name}");
    }
}
