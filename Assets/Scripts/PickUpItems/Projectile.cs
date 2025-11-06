using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float damage = 25f;
    public float lifeTime = 5f; // Projectile'ın maksimum yaşam süresi
    
    [Header("Visual Effects")]
    public GameObject hitEffect; // Çarpma efekti (isteğe bağlı)
    
    private Rigidbody2D rb;
    private Vector2 direction;
    private GameObject owner; // Kim ateş etti
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Projectile'ı hareket ettir
        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }
        
        // Belirli bir süre sonra yok et
        Destroy(gameObject, lifeTime);
    }
    
    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }
    
    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kendi sahibine hasar verme
        if (owner != null && other.gameObject == owner)
        {
            return; // Sahibine çarptıysa ignore et
        }
        
        // Enemy'ye çarptığında hasar ver
        if (other.CompareTag("Enemy") || other.GetComponent<EnemyAI>() != null)
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            
            // Hit effect oluştur
            CreateHitEffect();
            
            // Projectile'ı yok et
            Destroy(gameObject);
        }
        // Player'a çarptığında hasar ver
        else if (other.CompareTag("Player") || other.GetComponent<PlayerControls>() != null)
        {
            PlayerControls player = other.GetComponent<PlayerControls>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
            
            // Hit effect oluştur
            CreateHitEffect();
            
            // Projectile'ı yok et
            Destroy(gameObject);
        }
        // Duvara çarptığında yok ol
        else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            // Hit effect oluştur
            CreateHitEffect();
            
            // Projectile'ı yok et
            Destroy(gameObject);
        }
    }
    
    private void CreateHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            // Effect'i birkaç saniye sonra yok et
            Destroy(effect, 2f);
        }
    }
}