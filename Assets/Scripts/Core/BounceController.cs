using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BounceController : MonoBehaviour
{
    [Header("Configuration")]
    public BounceSettingsSO settings;
    public LayerMask wallLayerMask;

    // State
    private bool isBouncing = false;
    private int currentBounceCount = 0;
    private Vector2 lastVelocity;
    private Coroutine bounceCoroutine;
    private RigidbodyType2D initialBodyType;

    // Components
    private Rigidbody2D rb;
    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Stun Sprite
    private GameObject stunSpriteObject;
    private SpriteRenderer stunSpriteRenderer;
    private float floatAnimationTimer;

    // Events
    public System.Action OnBounceStart;
    public System.Action OnBounceEnd;

    public bool IsBouncing => isBouncing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            initialBodyType = rb.bodyType;
        }
    }

    public void StartBounce(Vector2 direction)
    {
        if (settings == null)
        {
            Debug.LogWarning($"{name}: BounceSettingsSO not assigned!");
            return;
        }

        if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);

        isBouncing = true;
        currentBounceCount = 0;
        OnBounceStart?.Invoke();

        // Disable Agent
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Setup Physics
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = direction.normalized * settings.bounceForce;
            rb.linearDamping = settings.linearDrag;
            rb.angularDamping = settings.angularDrag;

            // Freeze rotation to allow aiming while bouncing
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            lastVelocity = rb.linearVelocity;
        }

        // Visuals
        if (spriteRenderer != null && settings.enableColorTint)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = settings.bounceColor;
        }

        // Show Stun Sprite
        ShowStunSprite();

        bounceCoroutine = StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        float elapsed = 0f;
        while (elapsed < settings.bounceDuration)
        {
            elapsed += Time.deltaTime;

            // Track velocity for bounce calculation
            if (rb != null)
            {
                // If velocity is too low, stop early?
                if (rb.linearVelocity.magnitude < settings.minimumBounceVelocity && elapsed > 0.1f)
                {
                    break;
                }
                lastVelocity = rb.linearVelocity;
            }

            yield return null;
        }

        EndBounce();
    }

    public void EndBounce()
    {
        if (!isBouncing) return;

        isBouncing = false;
        OnBounceEnd?.Invoke();

        // Reset Physics
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = initialBodyType; // Restore original type
            rb.linearDamping = 0f;
        }

        // Re-enable Agent
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        // Reset Visuals
        if (spriteRenderer != null && settings.enableColorTint)
        {
            spriteRenderer.color = originalColor;
        }

        // Hide Stun Sprite
        HideStunSprite();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isBouncing || rb == null) return;

        // Check wall layer
        if (((1 << collision.gameObject.layer) & wallLayerMask) != 0)
        {
            HandleWallBounce(collision);
        }
    }

    private void HandleWallBounce(Collision2D collision)
    {
        currentBounceCount++;

        if (currentBounceCount > settings.maxBounces)
        {
            EndBounce();
            return;
        }

        if (collision.contacts.Length > 0)
        {
            Vector2 normal = collision.contacts[0].normal;

            // Offset to prevent sticking (User's fix)
            transform.position += (Vector3)(normal * 0.1f);

            // Reflect
            Vector2 reflected = Vector2.Reflect(lastVelocity, normal);
            rb.linearVelocity = reflected * settings.wallBounceMultiplier;
            lastVelocity = rb.linearVelocity;

            // Play wall bounce sound
            if (settings.wallBounceSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound(settings.wallBounceSound, settings.wallBounceSoundVolume);
            }
        }
    }

    private void Update()
    {
        // Update stun sprite position and animation
        if (stunSpriteObject != null && stunSpriteObject.activeSelf)
        {
            UpdateStunSpritePosition();
        }
    }

    private void ShowStunSprite()
    {
        if (settings == null || !settings.enableStunSprite || settings.stunSprite == null)
            return;

        // Create sprite object if it doesn't exist
        if (stunSpriteObject == null)
        {
            stunSpriteObject = new GameObject($"{name}_StunSprite");
            stunSpriteObject.transform.SetParent(transform);
            
            stunSpriteRenderer = stunSpriteObject.AddComponent<SpriteRenderer>();
            stunSpriteRenderer.sprite = settings.stunSprite;
            stunSpriteRenderer.sortingOrder = 100; // Render on top
            
            // Set sprite size
            stunSpriteObject.transform.localScale = new Vector3(
                settings.stunSpriteSize.x,
                settings.stunSpriteSize.y,
                1f
            );
        }

        // Update sprite in case settings changed
        if (stunSpriteRenderer != null)
        {
            stunSpriteRenderer.sprite = settings.stunSprite;
        }

        // Reset animation timer
        floatAnimationTimer = 0f;

        // Show sprite
        stunSpriteObject.SetActive(true);
        UpdateStunSpritePosition();
    }

    private void HideStunSprite()
    {
        if (stunSpriteObject != null)
        {
            stunSpriteObject.SetActive(false);
        }
    }

    private void UpdateStunSpritePosition()
    {
        if (stunSpriteObject == null || settings == null) return;

        Vector3 baseOffset = settings.stunSpriteOffset;
        
        // Add floating animation
        if (settings.enableFloatingAnimation)
        {
            floatAnimationTimer += Time.deltaTime * settings.floatAnimationSpeed;
            float floatOffset = Mathf.Sin(floatAnimationTimer * Mathf.PI * 2f) * settings.floatAnimationRange;
            baseOffset.y += floatOffset;
        }

        stunSpriteObject.transform.localPosition = baseOffset;
    }

    private void OnDestroy()
    {
        // Clean up stun sprite
        if (stunSpriteObject != null)
        {
            Destroy(stunSpriteObject);
        }
    }
}
