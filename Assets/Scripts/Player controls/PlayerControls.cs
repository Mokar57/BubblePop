using UnityEngine;
using System.Collections;

public class PlayerControls : MonoBehaviour, ISpeedBoostable, IItemHolder, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public Rigidbody2D rb;

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

    private void Start()
    {
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
    }

    private void Update()
    {
        if (isDead) return;

        ProcessInput();
        UpdateSpeedBoostFade();

        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector2 direction = new Vector2(mousePosition.x - transform.position.x, mousePosition.y - transform.position.y);
        transform.up = direction;

        // Attack Input
        if (Input.GetMouseButtonDown(0))
        {
            if (currentWeapon != null)
            {
                currentWeapon.TryAttack();
            }
        }

        // Throw Input
        if (Input.GetMouseButtonDown(1))
        {
            if (currentWeapon != null)
            {
                currentWeapon.Throw(transform.up);
                // OnItemDropped will be called by Weapon.Throw -> currentHolder.OnItemDropped
            }
        }

        // Pickup Input (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
        }
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
    }

    void ProcessInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(moveX, moveY);
    }

    void Move()
    {
        float currentMoveSpeed = originalMoveSpeed * speedBoostMultiplier;
        rb.linearVelocity = new Vector2(moveDirection.x * currentMoveSpeed, moveDirection.y * currentMoveSpeed);
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

        TakeDamage(damageInfo.damageAmount);

        // TODO: Handle Bounce if damageInfo.damageType == DamageType.Blunt
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
