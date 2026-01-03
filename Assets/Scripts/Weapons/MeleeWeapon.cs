using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Logic for melee weapons (Bat, Pipe, etc.)
/// Uses OnTriggerEnter2D to detect and damage enemies during attack
/// </summary>
public class MeleeWeapon : Weapon
{
    private float lastAttackTime;
    private MeleeWeaponDataSO meleeData;
    
    // Attack state tracking
    private bool isAttacking = false;
    private bool hasReducedDurabilityThisAttack = false; // Track if durability was reduced in this attack
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>(); // Track hit targets to avoid multiple hits per attack

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

        // Determine Cooldown based on holder
        float cooldown = meleeData.playerAttackCooldown;
        if (currentHolder != null && currentHolder.GetTeam() == Team.Enemy)
        {
            cooldown = meleeData.enemyAttackCooldown;
        }

        if (Time.time < lastAttackTime + cooldown) return;

        lastAttackTime = Time.time;

        // Start attack state
        isAttacking = true;
        hasReducedDurabilityThisAttack = false;
        hitTargets.Clear();

        // Trigger rotation animation for melee weapon
        PlayAttackRotation();

        // Play attack sound
        if (meleeData.useSound != null && SoundManager.Instance != null)
        {
            Vector2 soundPosition = currentHolder != null ? (Vector2)currentHolder.GetHoldPosition(HoldType).position : (Vector2)transform.position;
            SoundManager.Instance.PlaySound(meleeData.useSound, meleeData.useSoundVolume);
        }

        // End attack state after attack duration
        float attackDuration = rotationDuration + rotationHoldTime;
        Invoke(nameof(EndAttack), attackDuration);
    }

    /// <summary>
    /// Called at the end of attack animation
    /// </summary>
    private void EndAttack()
    {
        isAttacking = false;
        hasReducedDurabilityThisAttack = false;
        hitTargets.Clear();
    }

    /// <summary>
    /// OnTriggerEnter2D - Detects collisions with enemies during attack
    /// Deals damage based on damage type (Piercing or Blunt)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only damage during attack state
        if (!isAttacking) return;
        if (!isHeld) return;
        if (meleeData == null) return;

        // Avoid hitting the same target multiple times in one attack
        if (hitTargets.Contains(other.gameObject)) return;

        // Check for IDamageable
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            // Check team (no friendly fire)
            if (currentHolder != null && target.GetTeam() == currentHolder.GetTeam()) return;

            // Calculate direction for knockback
            Vector2 attackOrigin = currentHolder != null ? (Vector2)currentHolder.GetHoldPosition(HoldType).position : (Vector2)transform.position;
            Vector2 dirToTarget = (other.transform.position - (Vector3)attackOrigin).normalized;

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

            // Mark target as hit
            hitTargets.Add(other.gameObject);

            // Reduce durability only once per attack (on first hit)
            if (!hasReducedDurabilityThisAttack)
            {
                ReduceDurability();
                hasReducedDurabilityThisAttack = true;
            }

            // Show hit effect at collision point
            if (meleeData.showHitEffect)
            {
                SpawnHitEffect(other.ClosestPoint(transform.position), dirToTarget);
            }
        }
    }

    /// <summary>
    /// Spawns visual hit effect at the attack location
    /// </summary>
    private void SpawnHitEffect(Vector2 position, Vector2 direction)
    {
        GameObject effectObj = new GameObject("MeleeHitEffect");
        effectObj.transform.position = position;
        
        // Rotate effect to face attack direction (optional)
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effectObj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        MeleeHitEffect effect = effectObj.AddComponent<MeleeHitEffect>();
        effect.effectSize = new Vector2(meleeData.hitEffectSize, meleeData.hitEffectSize);
        effect.duration = meleeData.hitEffectDuration;
        effect.effectColor = meleeData.hitEffectColor;
        effect.fadeOut = meleeData.hitEffectFadeOut;
        effect.expand = meleeData.hitEffectExpand;
        effect.expandScale = meleeData.hitEffectExpandScale;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw weapon collider bounds for visualization
        if (itemCollider != null)
        {
            Gizmos.color = isAttacking ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(itemCollider.bounds.center, itemCollider.bounds.size);
        }
    }
}
