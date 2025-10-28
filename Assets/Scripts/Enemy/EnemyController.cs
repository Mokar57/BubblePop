using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damage = 10f;
    
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private LayerMask playerLayer = -1;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private Color damageColor = Color.red;
    
    private EnemyAI enemyAI;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float lastAttackTime;
    private bool isDead = false;
    
    // Events
    public System.Action<EnemyController> OnEnemyDeath;
    public System.Action<float> OnHealthChanged;
    
    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
            
        health = maxHealth;
        OnHealthChanged?.Invoke(health / maxHealth);
    }
    
    private void Update()
    {
        if (isDead) return;
        
        CheckForAttack();
    }
    
    private void CheckForAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        // Check if player is in attack range
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        
        if (playerCollider != null && playerCollider.CompareTag("Player"))
        {
            AttackPlayer(playerCollider.gameObject);
            lastAttackTime = Time.time;
        }
    }
    
    private void AttackPlayer(GameObject player)
    {
        // Try to damage the player
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Debug.Log($"{gameObject.name} attacked player for {damage} damage!");
        }
        
        // Add attack animation or effects here
        StartCoroutine(AttackFlash());
    }
    
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        
        health -= damageAmount;
        health = Mathf.Clamp(health, 0, maxHealth);
        
        OnHealthChanged?.Invoke(health / maxHealth);
        
        // Visual feedback
        StartCoroutine(DamageFlash());
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float healAmount)
    {
        if (isDead) return;
        
        health += healAmount;
        health = Mathf.Clamp(health, 0, maxHealth);
        
        OnHealthChanged?.Invoke(health / maxHealth);
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Stop AI
        if (enemyAI != null)
            enemyAI.StopFollowing();
            
        // Disable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
            
        // Death effect
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, transform.rotation);
            
        // Notify other systems
        OnEnemyDeath?.Invoke(this);
        
        // Destroy after delay for death animation
        Destroy(gameObject, 2f);
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
    
    private System.Collections.IEnumerator AttackFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    // Public getters
    public float Health => health;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => health / maxHealth;
    public bool IsDead => isDead;
    public float Damage => damage;
    
    // Public setters
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        health = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(health / maxHealth);
    }
    
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}