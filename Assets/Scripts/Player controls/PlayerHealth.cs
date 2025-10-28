using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    
    [Header("UI (Optional)")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text healthText;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;
    
    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDeath;
    
    private void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
            
        UpdateHealthUI();
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Visual feedback
        StartCoroutine(DamageFlash());
        
        // Update UI
        UpdateHealthUI();
        
        // Trigger event
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    public void Heal(float healAmount)
    {
        if (isDead) return;
        
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        Debug.Log($"Player healed {healAmount}. Health: {currentHealth}/{maxHealth}");
    }
    
    private void Die()
    {
        isDead = true;
        
        Debug.Log("Player died!");
        
        // Disable player controls
        PlayerControls playerControls = GetComponent<PlayerControls>();
        if (playerControls != null)
            playerControls.enabled = false;
            
        // Trigger death event
        OnPlayerDeath?.Invoke();
        
        // You can add death animation, game over screen, etc. here
        // For now, let's just reload the scene after a delay
        StartCoroutine(RestartAfterDelay());
    }
    
    private System.Collections.IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        
        // Reload current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    // Public getters
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;
    
    // Public setters
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }
    
    public void FullHeal()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        OnHealthChanged?.Invoke(1f);
    }
}