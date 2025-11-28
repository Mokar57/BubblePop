using UnityEngine;
using System.Collections;

public class ThrownItem : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D itemCollider;
    private bool isSlowingDown = false;
    private GameObject thrower;
    private WeaponDataSO data;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        itemCollider = GetComponent<Collider2D>();
    }

    public void Initialize(WeaponDataSO weaponData)
    {
        data = weaponData;
        isSlowingDown = false;

        // Disable pickup while thrown
        if (TryGetComponent<PickupableItem>(out var pickup))
        {
            pickup.enabled = false;
        }

        // Ensure physics settings
        if (rb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearDamping = 0f; // Ensure no drag while flying
            rb.angularDamping = 0f;
        }
    }

    public void SetThrower(GameObject newThrower)
    {
        thrower = newThrower;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSlowingDown) return;

        // Ignore thrower
        if (thrower != null && collision.gameObject == thrower) return;

        // Check if target is an enemy that can catch weapons
        if (collision.gameObject.TryGetComponent<EnemyAI>(out var enemyAI) &&
            enemyAI.enemyData != null &&
            enemyAI.enemyData.canCatchThrownWeapons)
        {
            // Check if enemy already has a weapon
            bool alreadyHasWeapon = false;
            if (collision.gameObject.TryGetComponent<EnemyWeaponController>(out var enemyWeaponController))
            {
                alreadyHasWeapon = enemyWeaponController.IsHoldingWeapon;
            }

            // Only try to catch if they don't have a weapon
            if (!alreadyHasWeapon)
            {
                // Try to catch
                if (collision.gameObject.TryGetComponent<IItemHolder>(out var catcherHolder))
                {
                    // Attempt pickup
                    if (TryGetComponent<PickupableItem>(out var pickupItem))
                    {
                        // Temporarily enable pickup for the catch action
                        pickupItem.enabled = true;

                        // We need to ensure the item is in a state to be picked up
                        // If we pick it up, we should destroy ThrownItem component immediately
                        if (pickupItem.TryPickup(catcherHolder))
                        {
                            // Successful catch
                            Destroy(this); // Remove ThrownItem component
                            return; // Skip damage and bounce
                        }

                        // If catch failed, re-disable
                        pickupItem.enabled = false;
                    }
                }
            }
        }

        // Use relative velocity to determine impact intensity
        float impactSpeed = collision.relativeVelocity.magnitude;

        // Only deal damage if impact is significant
        if (impactSpeed > 1f)
        {
            // Deal Damage
            if (collision.gameObject.TryGetComponent<IDamageable>(out var target))
            {
                // Check team
                Team throwerTeam = Team.Neutral;
                if (thrower != null && thrower.TryGetComponent<IItemHolder>(out var holder))
                {
                    throwerTeam = holder.GetTeam();
                }
                else if (thrower != null && thrower.TryGetComponent<IDamageable>(out var damageable))
                {
                    throwerTeam = damageable.GetTeam();
                }

                if (target.GetTeam() != throwerTeam)
                {
                    float dmg = data != null ? data.throwDamage : 20f; // Fallback
                    Vector2 knockbackDir = rb != null ? rb.linearVelocity.normalized : Vector2.zero;

                    // If velocity is zero (stopped on impact), use contact normal reversed
                    if (knockbackDir == Vector2.zero && collision.contactCount > 0)
                    {
                        knockbackDir = -collision.contacts[0].normal;
                    }

                    DamageInfo info = new DamageInfo(
                        dmg,
                        DamageType.Blunt, // Thrown items usually blunt
                        throwerTeam,
                        gameObject,
                        knockbackDir,
                        5f // Knockback force
                    );
                    target.TakeDamage(info);

                    // Notify thrower
                    if (thrower != null && thrower.TryGetComponent<IItemHolder>(out var throwerHolder))
                    {
                        throwerHolder.OnDamageDealt(info, target);
                    }
                }
            }
        }

        // Reduce Durability / Break Weapon on Impact
        if (TryGetComponent<Weapon>(out var weapon))
        {
            weapon.ForceBreak();
        }

        BounceAndSlowDown(collision);
    }

    private void BounceAndSlowDown(Collision2D collision)
    {
        // Only start slowing down if we hit something substantial (not a trigger, which collision handles)
        // But we want to bounce first.

        if (rb != null)
        {
            // Calculate bounce
            Vector2 bounceDirection = Vector2.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
            float currentSpeed = rb.linearVelocity.magnitude;
            float bounceMult = data != null ? data.bounceForce : 0.3f;

            // Apply bounce velocity
            rb.linearVelocity = bounceDirection * currentSpeed * bounceMult;

            // Start slowing down AFTER the bounce
            if (!isSlowingDown)
            {
                isSlowingDown = true;
                StartCoroutine(SlowDownOverTime());
            }
        }
    }

    private IEnumerator SlowDownOverTime()
    {
        float elapsed = 0f;
        Vector2 initialVelocity = rb.linearVelocity;
        float initialAngularVelocity = rb.angularVelocity;
        float duration = data != null ? data.slowDownDuration : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rb.linearVelocity = Vector2.Lerp(initialVelocity, Vector2.zero, t);
            rb.angularVelocity = Mathf.Lerp(initialAngularVelocity, 0f, t);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.linearDamping = 10f;
        rb.angularDamping = 10f;

        // Re-enable pickup
        if (TryGetComponent<PickupableItem>(out var pickup))
        {
            pickup.enabled = true;
            // pickup.ResetPickupState(); // If needed
        }

        // Restore collider to trigger state for pickup logic (assuming top-down walk-over pickup)
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.isTrigger = true;
        }

        Destroy(this);
    }
}
