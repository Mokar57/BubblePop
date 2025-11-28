using UnityEngine;

/// <summary>
/// Handles projectile movement and collision
/// Configured by RangedWeapon using ProjectileDataSO
/// </summary>
public class Projectile : MonoBehaviour
{
    private DamageInfo damageInfo;
    private ProjectileDataSO projectileData;
    private Rigidbody2D rb;
    private bool isStuck = false;
    private int hitCount = 0;

    public void Initialize(DamageInfo info, ProjectileDataSO data)
    {
        damageInfo = info;
        projectileData = data;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        // Setup Rigidbody
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Apply Velocity
        // Using transform.up because PlayerControls uses Up-vector for rotation
        rb.linearVelocity = transform.up * projectileData.speed;

        // Setup Visuals (Trail)
        if (projectileData.enableTrail && TryGetComponent<TrailRenderer>(out var trail))
        {
            trail.startWidth = projectileData.trailWidth;
            trail.endWidth = 0f;
            trail.startColor = projectileData.trailColor;
            trail.endColor = new Color(projectileData.trailColor.r, projectileData.trailColor.g, projectileData.trailColor.b, 0f);
            trail.time = projectileData.trailTime;
        }

        // Auto-destroy
        Destroy(gameObject, projectileData.lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isStuck) return;

        // Ignore self/owner
        if (other.gameObject == damageInfo.sourceObject) return;

        // Check for IDamageable
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            // Check Team
            if (target.GetTeam() == damageInfo.sourceTeam) return;

            // Apply Damage
            // Update knockback direction based on velocity
            damageInfo.knockbackDirection = rb.linearVelocity.normalized;
            target.TakeDamage(damageInfo);

            hitCount++;

            // Pass-through logic
            if (projectileData.canPassThrough)
            {
                if (projectileData.maxHitCount > 0 && hitCount >= projectileData.maxHitCount)
                {
                    Destroy(gameObject);
                }
                // Else continue flying
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else if (!other.isTrigger) // Hit a wall/obstacle
        {
            HandleWallCollision(other);
        }
    }

    private void HandleWallCollision(Collider2D wall)
    {
        isStuck = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Parent to wall so it moves with it (optional)
        transform.SetParent(wall.transform);

        // Wobble animation
        if (projectileData.enableWobbleAnimation)
        {
            StartCoroutine(WobbleRoutine());
        }

        // Destroy after stuck duration
        Destroy(gameObject, projectileData.stuckDuration);
    }

    private System.Collections.IEnumerator WobbleRoutine()
    {
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;

        while (elapsed < 0.5f) // Wobble for 0.5s
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * projectileData.wobbleSpeed) * projectileData.wobbleAmplitude;
            transform.rotation = startRot * Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            yield return null;
        }
        transform.rotation = startRot;
    }
}
