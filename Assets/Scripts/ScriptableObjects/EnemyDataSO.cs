using UnityEngine;

/// <summary>
/// ScriptableObject for enemy configuration data
/// Contains all AI, health, and behavior settings for an enemy type
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "PopTheBubble/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName = "Enemy";

    [Header("Health")]
    [Tooltip("Maximum health of this enemy")]
    public float maxHealth = 100f;

    [Header("Movement Speeds")]
    [Tooltip("Speed when patrolling waypoints")]
    public float patrolSpeed = 2f;

    [Tooltip("Speed when chasing the player")]
    public float chaseSpeed = 3.5f;

    [Tooltip("NavMeshAgent acceleration")]
    public float acceleration = 8f;

    [Tooltip("NavMeshAgent angular speed")]
    public float angularSpeed = 120f;

    [Header("Vision Settings")]
    [Tooltip("How far the enemy can see")]
    public float visionRange = 8f;

    [Tooltip("Vision cone angle in degrees")]
    [Range(0f, 360f)]
    public float visionAngle = 60f;

    [Tooltip("Rotation speed for vision direction (degrees per second)")]
    public float rotationSpeed = 90f;

    [Header("Proximity Detection")]
    [Tooltip("Range to detect player without vision (close proximity)")]
    public float proximityRadius = 5f;

    [Tooltip("How long player must stay in proximity before triggering chase")]
    public float proximityTriggerTime = 1f;

    [Tooltip("Enable proximity detection")]
    public bool enableProximityDetection = true;

    [Header("Sound Detection")]
    [Tooltip("How far enemy can hear sounds (gunshots, etc.)")]
    public float soundDetectionRange = 15f;

    [Header("Death Effects")]
    [Tooltip("Prefab to spawn on death (soap puddle, speed boost zone, etc.)")]
    public GameObject deathEffectPrefab;

    [Tooltip("Size of the puddle/effect spawned on death")]
    public float puddleSize = 1f;

    [Tooltip("Death sound clips (plays random)")]
    public AudioClip[] deathSounds;

    [Tooltip("Volume of death sounds")]
    [Range(0f, 1f)]
    public float deathSoundVolume = 1f;

    [Header("Item Usage")]
    [Tooltip("Can this enemy pick up and use weapons?")]
    public bool canUseWeapons = true;
}
