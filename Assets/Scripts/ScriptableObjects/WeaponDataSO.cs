using UnityEngine;

/// <summary>
/// Base ScriptableObject for all weapon data
/// Contains common properties shared by all weapons
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "PopTheBubble/Weapons/Base Weapon")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName = "Weapon";
    public ItemHoldType holdType = ItemHoldType.Melee;

    [Header("Enemy Usage Settings")]
    [Tooltip("Range of attack for ENEMY")]
    public float enemyAttackRange = 1.5f;

    [Tooltip("Cooldown between attacks for ENEMIES")]
    public float enemyAttackCooldown = 1.5f;

    [Header("Player Usage Settings")]
    [Tooltip("Range of attack for PLAYER")]
    public float playerAttackRange = 2f;

    [Tooltip("Cooldown between attacks for PLAYER")]
    public float playerAttackCooldown = 0.5f;

    [Header("Durability")]
    [Tooltip("Maximum number of times this weapon can be used before breaking")]
    public int maxDurability = 10;

    [Header("Damage")]
    [Tooltip("Type of damage this weapon deals")]
    public DamageType damageType = DamageType.Piercing;

    [Tooltip("Base damage amount")]
    public float baseDamage = 25f;

    [Header("Throwing")]
    [Tooltip("Force applied when weapon is thrown")]
    public float throwForce = 10f;

    [Tooltip("Damage dealt when thrown weapon hits an enemy")]
    public float throwDamage = 100f;

    [Tooltip("Force multiplier when bouncing off walls/enemies (0-1)")]
    [Range(0f, 1f)]
    public float bounceForce = 0.3f;

    [Tooltip("How long it takes to slow down to a stop after hitting something")]
    public float slowDownDuration = 1f;

    [Header("Audio")]
    [Tooltip("Sound played when weapon is used (attack)")]
    public AudioClip useSound;

    [Tooltip("Volume of use sound")]
    [Range(0f, 1f)]
    public float useSoundVolume = 1f;

    [Tooltip("Sound played when weapon is thrown")]
    public AudioClip throwSound;

    [Tooltip("Volume of throw sound")]
    [Range(0f, 1f)]
    public float throwSoundVolume = 1f;

    [Tooltip("Radius of sound alert when used/thrown")]
    public float soundAlertRadius = 10f;

    [Header("Visuals")]
    [Tooltip("Sprite shown when weapon is held")]
    public Sprite heldSprite;

    [Tooltip("Sprite shown when weapon is dropped on ground")]
    public Sprite droppedSprite;

    [Tooltip("Sprite shown when weapon is depleted/broken")]
    public Sprite depletedSprite;
}
