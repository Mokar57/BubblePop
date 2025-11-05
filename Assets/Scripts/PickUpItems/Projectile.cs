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
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Enemy'ye çarptığında hasar ver
        if (other.CompareTag("Enemy") || other.GetComponent<EnemyAI>() != null)
        {
            EnemyAI enemy = other.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Projectile hit enemy for {damage} damage!");
            }
            
            // Hit effect oluştur
            CreateHitEffect();
            
            // Projectile'ı yok et
            Destroy(gameObject);
        }
        // Duvara çarptığında yok ol
        else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Debug.Log("Projectile hit wall and destroyed");
            
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