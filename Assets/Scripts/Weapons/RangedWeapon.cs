using UnityEngine;

/// <summary>
/// Logic for ranged weapons (Pistol, Shotgun, Fan, etc.)
/// Spawns projectiles and handles ammo/cooldowns
/// </summary>
public class RangedWeapon : Weapon
{
    private float lastFireTime;
    private RangedWeaponDataSO rangedData;

    public override void Initialize(WeaponDataSO data)
    {
        base.Initialize(data);
        rangedData = data as RangedWeaponDataSO;

        if (rangedData == null)
        {
            Debug.LogError($"Weapon {name} initialized with wrong data type! Expected RangedWeaponDataSO.");
        }
    }

    protected override void PerformAttack()
    {
        if (rangedData == null)
        {
            Debug.LogError($"{name}: RangedData is null! Cannot attack.");
            return;
        }
        if (Time.time < lastFireTime + rangedData.fireCooldown) return;

        lastFireTime = Time.time;

        // Play fire sound (with alert radius if SoundManager exists)
        // For now, simple play
        if (rangedData.useSound != null)
        {
            AudioSource.PlayClipAtPoint(rangedData.useSound, transform.position, rangedData.useSoundVolume);
            // TODO: Trigger GameEvents.OnSoundEmitted(transform.position, rangedData.soundAlertRadius);
        }

        // Spawn Projectile
        if (rangedData.projectilePrefab != null)
        {
            Vector2 spawnPos = currentHolder != null ? (Vector2)currentHolder.GetHoldPosition(holdType).position : (Vector2)transform.position;
            Quaternion spawnRot = currentHolder != null ? currentHolder.GetHoldPosition(holdType).rotation : transform.rotation;

            GameObject projObj = Instantiate(rangedData.projectilePrefab, spawnPos, spawnRot);

            if (projObj.TryGetComponent<Projectile>(out var projectile))
            {
                // Create DamageInfo
                DamageInfo damageInfo = new DamageInfo(
                    rangedData.baseDamage,
                    rangedData.damageType,
                    currentHolder != null ? currentHolder.GetTeam() : Team.Neutral,
                    gameObject,
                    Vector2.zero, // Knockback direction calculated on impact usually, or projectile velocity
                    0f // Knockback force handled by projectile or impact
                );

                // Initialize Projectile
                projectile.Initialize(damageInfo, rangedData.projectileData);
            }
        }

        ReduceDurability();
    }
}
