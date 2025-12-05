using UnityEngine;

/// <summary>
/// ScriptableObject for melee weapon data
/// Extends WeaponDataSO with melee-specific properties
/// </summary>
[CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "PopTheBubble/Weapons/Melee Weapon")]
public class MeleeWeaponDataSO : WeaponDataSO
{
    [Header("Melee Attack Settings")]
    [Tooltip("Angle of attack cone in degrees (360 = full circle)")]
    [Range(0f, 360f)]
    public float attackAngle = 90f;

    // attackCooldown moved to base WeaponDataSO

    [Header("Knockback (for Blunt damage)")]
    [Tooltip("Force of knockback applied to enemies")]
    public float knockbackForce = 10f;

    [Header("Visual Feedback")]
    [Tooltip("Layer mask for what can be hit")]
    public LayerMask hitLayerMask = -1;

    [Header("Hit Effect")]
    [Tooltip("Enable visual hit effect at strike location")]
    public bool showHitEffect = true;

    [Tooltip("Color of the hit effect flash")]
    public Color hitEffectColor = Color.white;

    [Tooltip("Size of the hit effect area")]
    public float hitEffectSize = 2f;

    [Tooltip("Duration of the hit effect (seconds)")]
    public float hitEffectDuration = 0.2f;

    [Tooltip("Should the effect fade out?")]
    public bool hitEffectFadeOut = true;

    [Tooltip("Should the effect expand?")]
    public bool hitEffectExpand = false;

    [Tooltip("Expansion scale multiplier")]
    public float hitEffectExpandScale = 1.5f;
}
