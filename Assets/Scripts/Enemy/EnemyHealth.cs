using UnityEngine;
using System.Collections;

/// <summary>
/// Handles enemy health, damage, and death
/// Implements IDamageable interface for consistent damage handling
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Health Settings (Override)")]
    [Tooltip("Leave at 0 to use enemyData.maxHealth")]
    public float maxHealthOverride = 0f;

    [Header("Death Effects")]
    [Tooltip("Color flash when taking damage")]
    public Color damageColor = Color.red;

    [Tooltip("Duration of damage flash")]
    public float flashDuration = 0.1f;

    // Health state
    private float currentHealth;
    private float maxHealth;
    private bool isDead = false;

    // Visual feedback
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnEnemyDeath;

    // Cached components
    private EnemyAI enemyAI;
    private EnemyWeaponController weaponController;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyAI = GetComponent<EnemyAI>();
        weaponController = GetComponent<EnemyWeaponController>();
    }

    private void Start()
    {
        // Initialize health from ScriptableObject or override
        if (enemyData != null)
        {
            maxHealth = maxHealthOverride > 0 ? maxHealthOverride : enemyData.maxHealth;
        }
        else
        {
            maxHealth = maxHealthOverride > 0 ? maxHealthOverride : 100f;
            Debug.LogWarning($"{gameObject.name}: No EnemyDataSO assigned! Using default health.");
        }

        currentHealth = maxHealth;

        // Store original sprite color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    #region IDamageable Implementation

    /// <summary>
    /// Apply damage using DamageInfo struct (new system)
    /// </summary>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead) return;

        // Check team - don't take damage from same team
        if (damageInfo.sourceTeam == Team.Enemy)
        {
            return; // Friendly fire disabled
        }

        // Apply damage based on type
        if (damageInfo.damageType == DamageType.Piercing)
        {
            // Piercing damage = instant kill
            currentHealth = 0;
        }
        else if (damageInfo.damageType == DamageType.Blunt)
        {
            // Blunt damage logic: First hit bounces, second hit (while bouncing) kills
            if (TryGetComponent<BounceController>(out var bounceController))
            {
                if (bounceController.IsBouncing)
                {
                    // Already bouncing -> Second hit kills
                    currentHealth = 0;
                }
                else
                {
                    // Not bouncing -> First hit triggers bounce. 
                    // Target is immune to death from blunt damage unless already bouncing.
                    bounceController.StartBounce(damageInfo.knockbackDirection);
                    return; // Exit early to avoid death check
                }
            }
            else
            {
                // Fallback if no BounceController
                currentHealth -= damageInfo.damageAmount;
            }

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        // Visual feedback
        StartCoroutine(DamageFlash());

        // Trigger event
        OnHealthChanged?.Invoke(currentHealth);

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Legacy damage method for backward compatibility
    /// Will be removed after full refactor
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Convert to new DamageInfo system (assume Piercing damage from Player team)
        DamageInfo damageInfo = DamageInfo.CreatePiercing(damage, Team.Player, null);
        TakeDamage(damageInfo);
    }

    /// <summary>
    /// Get team of this enemy
    /// </summary>
    public Team GetTeam()
    {
        return Team.Enemy;
    }

    /// <summary>
    /// Check if enemy is dead
    /// </summary>
    public bool IsDead => isDead;

    #endregion

    #region Health Management

    /// <summary>
    /// Heal the enemy
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// Get current health
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// Get max health
    /// </summary>
    public float MaxHealth => maxHealth;

    #endregion

    #region Death System

    /// <summary>
    /// Handle enemy death
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Play random death sound
        PlayRandomDeathSound();

        // Trigger death event FIRST (before disabling components)
        // This allows EnemyWeaponController to drop items while enemy is still active
        OnEnemyDeath?.Invoke();

        // Trigger global death event
        GameEvents.TriggerEnemyDied(gameObject);

        // Spawn death effect (soap puddle, speed boost zone, etc.)
        SpawnDeathEffect();

        // Disable AI
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // Disable NavMeshAgent
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // Destroy enemy after short delay
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// Spawn death effect prefab at death location
    /// </summary>
    private void SpawnDeathEffect()
    {
        if (enemyData != null && enemyData.deathPuddlePrefab != null)
        {
            Instantiate(enemyData.deathPuddlePrefab, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Play random death sound from enemyData using SoundManager
    /// </summary>
    private void PlayRandomDeathSound()
    {
        if (SoundManager.Instance != null && enemyData != null &&
            enemyData.deathSounds != null && enemyData.deathSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, enemyData.deathSounds.Length);
            AudioClip selectedSound = enemyData.deathSounds[randomIndex];

            if (selectedSound != null)
            {
                SoundManager.Instance.PlaySound(selectedSound, enemyData.deathSoundVolume);
            }
        }
    }

    #endregion

    #region Visual Feedback

    /// <summary>
    /// Flash sprite red when taking damage
    /// </summary>
    private IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }

    #endregion
}
