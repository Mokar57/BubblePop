using UnityEngine;

public class ThrownItem : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool hasHitTarget = false;
    
    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        hasHitTarget = false;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Eğer zaten bir hedefe çarptıysa, tekrar işlem yapma
        if (hasHitTarget) return;
        
        // Enemy veya Wall tag'ine sahip objelere çarptığında dur
        if (collision.gameObject.CompareTag("Enemy") || 
            collision.gameObject.CompareTag("Wall") ||
            collision.gameObject.name.ToLower().Contains("enemy") ||
            collision.gameObject.name.ToLower().Contains("wall"))
        {
            StopItem();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Eğer zaten bir hedefe çarptıysa, tekrar işlem yapma
        if (hasHitTarget) return;
        
        // Enemy veya Wall tag'ine sahip objelere çarptığında dur
        if (other.CompareTag("Enemy") || 
            other.CompareTag("Wall") ||
            other.name.ToLower().Contains("enemy") ||
            other.name.ToLower().Contains("wall"))
        {
            StopItem();
        }
    }
    
    private void StopItem()
    {
        hasHitTarget = true;
        
        if (rb != null)
        {
            // Item'ı durdur
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            // Friction'ı artır ki kaymasın
            rb.linearDamping = 10f;
            rb.angularDamping = 10f;
        }
        
        // Bu component'i kaldır çünkü artık gerekli değil
        Destroy(this);
    }
}