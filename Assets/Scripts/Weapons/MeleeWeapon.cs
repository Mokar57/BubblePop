using UnityEngine;

/// <summary>
/// Logic for melee weapons (Bat, Pipe, etc.)
/// Uses OverlapCircle to detect and damage enemies
/// </summary>
public class MeleeWeapon : Weapon
{
    private float lastAttackTime;
    private MeleeWeaponDataSO meleeData;

    public override void Initialize(WeaponDataSO data)
    {
        base.Initialize(data);
        meleeData = data as MeleeWeaponDataSO;

        if (meleeData == null)
        {
            Debug.LogError($"Weapon {name} initialized with wrong data type! Expected MeleeWeaponDataSO.");
        }
    }

    protected override void PerformAttack()
    {
        if (meleeData == null)
        {
            Debug.LogError($"{name}: MeleeData is null! Cannot attack.");
            return;
        }
        if (Time.time < lastAttackTime + meleeData.attackCooldown) return;

        lastAttackTime = Time.time;

        // Play attack sound
        if (meleeData.useSound != null)
        {
            // Use SoundManager if available, otherwise simple play
            AudioSource.PlayClipAtPoint(meleeData.useSound, transform.position, meleeData.useSoundVolume);
        }

        // Detect hits
        Vector2 attackOrigin = currentHolder != null ? (Vector2)currentHolder.GetHoldPosition(HoldType).position : (Vector2)transform.position;
        // Use Up as forward since PlayerControls uses transform.up for rotation
        Vector2 attackDirection = currentHolder != null ? currentHolder.GetHoldPosition(HoldType).up : transform.up;

        // Calculate hit position based on range
        Vector2 hitPos = attackOrigin + (attackDirection * (meleeData.attackRange * 0.5f));

        // OverlapCircle to find targets
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitPos, meleeData.attackRange);

        bool hitEnemy = false;

        foreach (var hit in hits)
        {
            // Skip self
            if (hit.gameObject == currentHolder?.GetHoldPosition(HoldType).gameObject) continue;

            // Check for IDamageable
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                // Check team (no friendly fire)
                if (currentHolder != null && target.GetTeam() == currentHolder.GetTeam()) continue;

                // Check angle (if we want cone attacks)
                Vector2 dirToTarget = (hit.transform.position - (Vector3)attackOrigin).normalized;
                float angle = Vector2.Angle(attackDirection, dirToTarget);

                if (angle <= meleeData.attackAngle * 0.5f)
                {
                    // Create DamageInfo
                    DamageInfo damageInfo = new DamageInfo(
                        meleeData.baseDamage,
                        meleeData.damageType,
                        currentHolder != null ? currentHolder.GetTeam() : Team.Neutral,
                        gameObject,
                        dirToTarget, // Knockback direction
                        meleeData.knockbackForce
                    );

                    // Apply damage
                    target.TakeDamage(damageInfo);

                    // Notify holder
                    currentHolder?.OnDamageDealt(damageInfo, target);

                    hitEnemy = true;
                }
            }
        }

        if (hitEnemy)
        {
            ReduceDurability();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (meleeData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + (transform.up * (meleeData.attackRange * 0.5f)), meleeData.attackRange);
        }
    }
}
