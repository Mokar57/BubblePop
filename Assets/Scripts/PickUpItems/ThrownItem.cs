using UnityEngine;
using System.Collections;

public class ThrownItem : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool hasHitTarget = false;
    private bool isSlowingDown = false;
    
    [Header("Bounce Settings")]
    public float bounceForce = 0.3f; // Sekme kuvveti (orijinal hızın yüzdesi)
    
    [Header("Slow Down Settings")]
    public float slowDownDuration = 1f; // Hızın sıfırlanma süresi
    
    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        hasHitTarget = false;
        isSlowingDown = false;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Eğer zaten yavaşlama başladıysa, tekrar işlem yapma
        if (isSlowingDown) return;
        
        // Enemy, Wall veya Tilemap'e çarptığında sekip yavaşla
        if (collision.gameObject.CompareTag("Enemy") || 
            collision.gameObject.CompareTag("Wall") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Default") || // Tilemap genelde Default layer'da
            collision.gameObject.GetComponent<UnityEngine.Tilemaps.Tilemap>() != null) // Tilemap component kontrolü
        {
            BounceAndSlowDown(collision);
        }
    }
    
    private void BounceAndSlowDown(Collision2D collision)
    {
        hasHitTarget = true;
        isSlowingDown = true;
        
        if (rb != null)
        {
            // Çarpma noktasından sekme yönünü hesapla
            Vector2 bounceDirection = Vector2.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
            
            // Mevcut hızın bir kısmıyla sekme uygula
            float currentSpeed = rb.linearVelocity.magnitude;
            rb.linearVelocity = bounceDirection * currentSpeed * bounceForce;
            
            // Coroutine ile yavaşça durdur
            StartCoroutine(SlowDownOverTime());
        }
    }
    
    private IEnumerator SlowDownOverTime()
    {
        float elapsed = 0f;
        Vector2 initialVelocity = rb.linearVelocity;
        float initialAngularVelocity = rb.angularVelocity;
        
        while (elapsed < slowDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slowDownDuration;
            
            // Lerp ile hızı yumuşak bir şekilde sıfıra indir
            rb.linearVelocity = Vector2.Lerp(initialVelocity, Vector2.zero, t);
            rb.angularVelocity = Mathf.Lerp(initialAngularVelocity, 0f, t);
            
            yield return null;
        }
        
        // Tam olarak sıfırla
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // Kaymaması için damping'i artır
        rb.linearDamping = 10f;
        rb.angularDamping = 10f;
        
        // Bu component'i kaldır çünkü artık gerekli değil
        Destroy(this);
    }
}