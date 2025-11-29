using UnityEngine;

/// <summary>
/// ScriptableObject for projectile configuration
/// Contains settings for projectile behavior, visuals, and effects
/// </summary>
[CreateAssetMenu(fileName = "NewProjectile", menuName = "PopTheBubble/Projectile Data")]
public class ProjectileDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string projectileName = "Projectile";

    [Header("Movement")]
    [Tooltip("Speed of projectile")]
    public float speed = 15f;

    [Tooltip("Maximum lifetime before auto-destroy (seconds)")]
    public float lifetime = 999f;

    [Header("Wall Collision")]
    [Tooltip("Duration projectile stays stuck in wall before destroying")]
    public float stuckDuration = 2f;

    [Tooltip("Should projectile wobble when stuck in wall?")]
    public bool enableWobbleAnimation = true;

    [Tooltip("Wobble animation speed")]
    public float wobbleSpeed = 5f;

    [Tooltip("Wobble animation amplitude")]
    public float wobbleAmplitude = 0.1f;

    [Header("Pass-Through")]
    [Tooltip("Can projectile pass through enemies and hit multiple targets?")]
    public bool canPassThrough = true;

    [Tooltip("Maximum number of enemies that can be hit (0 = unlimited)")]
    public int maxHitCount = 0;

    [Header("Visual Effects")]
    [Tooltip("Trail renderer settings")]
    public bool enableTrail = true;

    [Tooltip("Trail width")]
    public float trailWidth = 0.1f;

    [Tooltip("Trail color")]
    public Color trailColor = Color.yellow;

    [Tooltip("Trail lifetime (seconds)")]
    public float trailTime = 0.3f;

    [Header("Hit Effects")]
    [Tooltip("Prefab to spawn on hit")]
    public GameObject hitEffectPrefab;

    [Tooltip("Prefab to spawn on wall hit")]
    public GameObject wallHitEffectPrefab;

    [Header("Audio")]
    [Tooltip("Sound played when projectile hits a wall")]
    public AudioClip wallHitSound;

    [Tooltip("Volume of wall hit sound")]
    [Range(0f, 1f)]
    public float wallHitSoundVolume = 0.7f;
}
