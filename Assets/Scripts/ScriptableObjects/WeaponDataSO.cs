using UnityEngine;

/// <summary>
/// Base ScriptableObject for all weapon data
/// Contains common properties shared by all weapons
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuFolder = "PopTheBubble/Weapons/Base Weapon")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName = "Weapon";

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

    [Header("Visuals")]
    [Tooltip("Sprite shown when weapon is held")]
    public Sprite heldSprite;

    [Tooltip("Sprite shown when weapon is dropped on ground")]
    public Sprite droppedSprite;

    [Tooltip("Sprite shown when weapon is depleted/broken")]
    public Sprite depletedSprite;
}
