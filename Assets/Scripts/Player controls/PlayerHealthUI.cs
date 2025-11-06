using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Text healthText;
    
    [Header("Player Reference")]
    public PlayerControls player;
    
    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerControls>();
        }
        
        if (player != null)
        {
            // Subscribe to health changes
            player.OnHealthChanged += UpdateHealthUI;
            player.OnPlayerDeath += OnPlayerDeath;
            
            // Initialize UI
            UpdateHealthUI(player.CurrentHealth);
        }
        else
        {
            Debug.LogError("PlayerHealthUI: Player reference not found!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHealthUI;
            player.OnPlayerDeath -= OnPlayerDeath;
        }
    }
    
    private void UpdateHealthUI(float currentHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = player.MaxHealth;
            healthSlider.value = currentHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"HP: {currentHealth:F0}/{player.MaxHealth:F0}";
        }
    }
    
    private void OnPlayerDeath()
    {
        if (healthText != null)
        {
            healthText.text = "DEAD";
            healthText.color = Color.red;
        }
    }
}
