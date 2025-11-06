using UnityEngine;
using System.Collections;

public class ThrownItem : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D itemCollider;
    private bool hasHitTarget = false;
    private bool isSlowingDown = false;
    private bool hasDecreasedUsage = false; // Kullanım hakkını bir kere azalt
    private GameObject thrower; // Kim fırlattı
    
    [Header("Bounce Settings")]
    public float bounceForce = 0.3f; // Sekme kuvveti (orijinal hızın yüzdesi)
    
    [Header("Slow Down Settings")]
    public float slowDownDuration = 1f; // Hızın sıfırlanma süresi
    
    [Header("Damage Settings")]
    public float damageAmount = 20f; // Enemy'e verilecek hasar miktarı
    
    public void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        itemCollider = GetComponent<Collider2D>();
        hasHitTarget = false;
        isSlowingDown = false;
        hasDecreasedUsage = false;
        
        // Fırlatıldığında collider'ı sadece Player layer'ına ayarla
        SetColliderLayerMask("Player");
    }
    
    public void SetThrower(GameObject newThrower)
    {
        thrower = newThrower;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Eğer zaten yavaşlama başladıysa, tekrar işlem yapma
        if (isSlowingDown) return;
        
        // Fırlatan kişiye hasar verme
        if (thrower != null && collision.gameObject == thrower)
        {
            return; // Ignore collision with thrower
        }
        
        bool hitValidTarget = false;
        
        // Enemy'e çarptığında hasar ver
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damageAmount);
            }
            
            hitValidTarget = true;
            BounceAndSlowDown(collision);
        }
        // Player'a çarptığında hasar ver
        else if (collision.gameObject.CompareTag("Player"))
        {
            PlayerControls player = collision.gameObject.GetComponent<PlayerControls>();
            if (player != null)
            {
                player.TakeDamage(damageAmount);
            }
            
            hitValidTarget = true;
            BounceAndSlowDown(collision);
        }
        // Wall veya Tilemap'e çarptığında sadece sekip yavaşla
        else if (collision.gameObject.CompareTag("Wall") ||
                 collision.gameObject.layer == LayerMask.NameToLayer("Default") || // Tilemap genelde Default layer'da
                 collision.gameObject.GetComponent<UnityEngine.Tilemaps.Tilemap>() != null) // Tilemap component kontrolü
        {
            hitValidTarget = true;
            BounceAndSlowDown(collision);
        }
        
        // Sadece düşman veya duvara çarptığında kullanım hakkını azalt
        if (hitValidTarget && !hasDecreasedUsage)
        {
            hasDecreasedUsage = true;
            PickupableItem pickupable = GetComponent<PickupableItem>();
            if (pickupable != null)
            {
                pickupable.DecreaseUsage();
            }
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
        
        // Hız sıfırlandığında collider'ı Nothing'e ayarla
        SetColliderLayerMask("Nothing");
        
        // Kaymaması için damping'i artır
        rb.linearDamping = 10f;
        rb.angularDamping = 10f;
        
        // Bu component'i kaldır çünkü artık gerekli değil
        Destroy(this);
    }
    
    /// <summary>
    /// Collider'ın Include Layers'ını ayarlar
    /// </summary>
    private void SetColliderLayerMask(string layerName)
    {
        if (itemCollider == null) return;
        
        if (layerName == "Nothing")
        {
            // Nothing - hiçbir layer ile çarpışmaz
            itemCollider.includeLayers = 0;
        }
        else if (layerName == "Player")
        {
            // Sadece Player layer'ı ile çarpışır
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                itemCollider.includeLayers = 1 << playerLayer;
            }
        }
    }
}