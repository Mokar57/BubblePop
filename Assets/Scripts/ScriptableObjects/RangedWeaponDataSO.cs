using UnityEngine;

/// <summary>
/// ScriptableObject for ranged weapon data (pistols, rifles, etc.)
/// Extends WeaponDataSO with ranged-specific properties
/// </summary>
[CreateAssetMenu(fileName = "NewRangedWeapon", menuName = "PopTheBubble/Weapons/Ranged Weapon")]
public class RangedWeaponDataSO : WeaponDataSO
{
    [Header("Ranged Attack Settings")]
    [Tooltip("Projectile prefab to spawn when firing")]
    public GameObject projectilePrefab;

    [Tooltip("Cooldown between shots in seconds")]
    public float fireCooldown = 0.5f;

    [Tooltip("Speed of fired projectile")]
    public float projectileSpeed = 15f;

    // soundAlertRadius moved to base WeaponDataSO

    [Header("Projectile Settings")]
    [Tooltip("Reference to ProjectileDataSO for additional projectile configuration")]
    public ProjectileDataSO projectileData;
}
