using UnityEngine;
using System.Collections;

public class PlayerControls : MonoBehaviour, ISpeedBoostable, IItemHolder, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public Rigidbody2D rb;

    [Header("Rotation Settings")]
    [Tooltip("Minimum distance from player for mouse rotation (squared distance)")]
    public float rotationDeadzone = 0.1f;

    // Speed boost variables
    private float originalMoveSpeed;
    private float speedBoostMultiplier = 1f;
    private bool isSpeedBoosted = false;

    // Fading variables
    private float maxBoostValue = 1f;
    private float currentSpeedBoostFadeTime = 0f;
    private float initialFadeDuration = 0f;
    private bool isFadingBoost = false;

    private Vector2 moveDirection;
    private Vector2 lastValidMouseDirection = Vector2.up;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Death Effects")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.1f;

    // Health variables
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDeath;

    [Header("Item Pickup System")]
    public Transform meleeHoldPosition;
    public Transform rangedHoldPosition;
    private Weapon currentWeapon;
    private BounceController bounceController;

    private void Start()
    {
        bounceController = GetComponent<BounceController>();

        // Store original speed for speed boost functionality
        originalMoveSpeed = moveSpeed;

        // Initialize health
        currentHealth = maxHealth;

        // Get sprite renderer for damage flash
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (meleeHoldPosition == null) meleeHoldPosition = transform;
        if (rangedHoldPosition == null) rangedHoldPosition = transform;

        // Subscribe to input events
        SubscribeToInputEvents();
    }

    private void OnDestroy()
    {
        // Unsubscribe from input events to prevent memory leaks
        UnsubscribeFromInputEvents();
    }

    private void SubscribeToInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed += HandleAttack;
            InputManager.Instance.OnThrowPressed += HandleThrow;
            InputManager.Instance.OnPickupPressed += TryPickupItem;
        }
    }

    private void UnsubscribeFromInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackPressed -= HandleAttack;
            InputManager.Instance.OnThrowPressed -= HandleThrow;
            InputManager.Instance.OnPickupPressed -= TryPickupItem;
        }
    }

    private void HandleAttack()
    {
        if (isDead) return;
        if (currentWeapon != null)
        {
            currentWeapon.TryAttack();
        }
    }

    private void HandleThrow()
    {
        if (isDead) return;
        if (currentWeapon != null)
        {
            currentWeapon.Throw(transform.up);
        }
    }

    private void Update()
    {
        if (isDead) return;

        ProcessInput();
        UpdateSpeedBoostFade();
    }

    private void TryPickupItem()
    {
        // Simple overlap check for items
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PickupableItem>(out var item))
            {
                if (item.TryPickup(this))
                {
                    break; // Picked up one item
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        Move();
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (InputManager.Instance == null) return;

        // Use InputManager's mouse direction from the screen center
        Vector2 direction = InputManager.Instance.MouseDirection;

        // Check if the mouse is outside the deadzone (not near the screen center)
        if (direction.sqrMagnitude > rotationDeadzone)
        {
            lastValidMouseDirection = direction;
        }

        // Calculate the exact angle from the last valid direction
        float targetAngle = Mathf.Atan2(lastValidMouseDirection.y, lastValidMouseDirection.x) * Mathf.Rad2Deg - 90f;

        // Apply immediately via Physics engine
        rb.MoveRotation(targetAngle);
    }

    void ProcessInput()
    {
        if (InputManager.Instance == null) return;

        // Get movement input from InputManager
        moveDirection = InputManager.Instance.MoveInput;
    }

    void Move()
    {
        if (bounceController != null && bounceController.IsBouncing) return;

        float currentMoveSpeed = originalMoveSpeed * speedBoostMultiplier;

        // Normalize diagonal movement so player doesn't move faster diagonally
        Vector2 normalizedDirection = moveDirection.normalized;
        rb.linearVelocity = normalizedDirection * currentMoveSpeed;
    }

    #region IItemHolder Implementation

    public Transform GetHoldPosition(ItemHoldType holdType)
    {
        return holdType == ItemHoldType.Ranged ? rangedHoldPosition : meleeHoldPosition;
    }

    public Team GetTeam()
    {
        return Team.Player;
    }

    public void OnItemPickedUp(GameObject item)
    {
        // If we already have a weapon, drop it
        if (currentWeapon != null && currentWeapon.gameObject != item)
        {
            currentWeapon.OnDropped();
        }

        if (item.TryGetComponent<Weapon>(out var weapon))
        {
            currentWeapon = weapon;
        }
    }

    public void OnItemDropped(GameObject item)
    {
        if (currentWeapon != null && currentWeapon.gameObject == item)
        {
            currentWeapon = null;
        }
    }

    public void OnDamageDealt(DamageInfo damageInfo, IDamageable target)
    {
        // Player specific logic when dealing damage (e.g. XP, stats)
    }

    #endregion

    #region ISpeedBoostable Implementation

    public void ApplySpeedBoost(float multiplier)
    {
        // Stop fading if we get a new boost
        bool wasFading = isFadingBoost;
        isFadingBoost = false;

        if (!isSpeedBoosted || wasFading || multiplier > maxBoostValue)
        {
            maxBoostValue = multiplier;
            speedBoostMultiplier = multiplier;
            isSpeedBoosted = true;
        }
    }

    public void RemoveSpeedBoost(float fadeDuration = 0f)
    {
        if (isSpeedBoosted)
        {
            if (fadeDuration > 0f)
            {
                isFadingBoost = true;
                initialFadeDuration = fadeDuration;
                currentSpeedBoostFadeTime = fadeDuration;
            }
            else
            {
                speedBoostMultiplier = 1f;
                maxBoostValue = 1f;
                isSpeedBoosted = false;
                isFadingBoost = false;
            }
        }
    }

    private void UpdateSpeedBoostFade()
    {
        if (isFadingBoost)
        {
            currentSpeedBoostFadeTime -= Time.deltaTime;

            if (currentSpeedBoostFadeTime <= 0)
            {
                isFadingBoost = false;
                isSpeedBoosted = false;
                speedBoostMultiplier = 1f;
                maxBoostValue = 1f;
            }
            else
            {
                float t = currentSpeedBoostFadeTime / initialFadeDuration;
                speedBoostMultiplier = Mathf.Lerp(1f, maxBoostValue, t);
            }
        }
    }

    public bool IsSpeedBoosted => isSpeedBoosted;

    #endregion

    #region IDamageable Implementation

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead) return;
        if (damageInfo.sourceTeam == Team.Player) return; // Friendly fire check

        if (damageInfo.damageType == DamageType.Blunt)
        {
            if (TryGetComponent<BounceController>(out var bounceController))
            {
                if (bounceController.IsBouncing)
                {
                    // Already bouncing -> Second hit kills
                    currentHealth = 0;
                    Die();
                }
                else
                {
                    // Not bouncing -> First hit triggers bounce. No damage.
                    bounceController.StartBounce(damageInfo.knockbackDirection);
                }
            }
            else
            {
                // Fallback
                TakeDamage(damageInfo.damageAmount);
            }
        }
        else
        {
            // Normal damage
            TakeDamage(damageInfo.damageAmount);
        }
    }

    // Legacy TakeDamage for compatibility if needed, or internal use
    private void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

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

    bool IDamageable.IsDead => isDead;

    #endregion

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Trigger local death event (backward compatibility)
        OnPlayerDeath?.Invoke();

        // Trigger global death event (decoupled system)
        GameEvents.TriggerPlayerDeath();

        // Disable player controls
        // this.enabled = false; // Don't disable script, just stop updates in Update/FixedUpdate

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Optional: Change sprite color or play death animation
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray out
        }

        Debug.Log("Player Died!");
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

    // Public getters
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
}
