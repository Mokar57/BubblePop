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

        // Ensure physics settings
        if (rb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

                DamageInfo info = new DamageInfo(
                    dmg,
                    DamageType.Blunt, // Thrown items usually blunt
                    throwerTeam,
                    gameObject,
                    knockbackDir,
                    5f // Knockback force
                );
                target.TakeDamage(info);
            }
        }

        // Reduce Durability
        if (TryGetComponent<Weapon>(out var weapon))
        {
            // weapon.ReduceDurability(1); // Protected method, need public access or friend
            // For now, let's assume throwing doesn't break it immediately unless it hits hard?
            // Or maybe we don't reduce durability on throw impact for now.
        }

        BounceAndSlowDown(collision);
    }

    private void BounceAndSlowDown(Collision2D collision)
    {
        isSlowingDown = true;

        if (rb != null)
        {
            Vector2 bounceDirection = Vector2.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
            float currentSpeed = rb.linearVelocity.magnitude;
            float bounceMult = data != null ? data.bounceForce : 0.3f;
            rb.linearVelocity = bounceDirection * currentSpeed * bounceMult;
            StartCoroutine(SlowDownOverTime());
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

        Destroy(this);
    }
}
