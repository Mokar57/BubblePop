using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Handles enemy movement using NavMeshAgent
/// Manages patrol waypoints, chase behavior, and stuck detection
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovementController : MonoBehaviour
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Patrol Settings")]
    [Tooltip("Enable patrol waypoint system")]
    public bool enablePatrol = true;

    [Tooltip("List of waypoints to patrol")]
    public List<Waypoint> patrolWaypoints = new List<Waypoint>();

    [Tooltip("Loop back to first waypoint after reaching last")]
    public bool loopPatrol = true;

    [Tooltip("Distance to consider waypoint as reached")]
    public float waypointReachDistance = 0.1f;

    [Tooltip("Patrol waypoints in random order")]
    public bool randomPatrolOrder = false;

    [Header("Stuck Detection")]
    [Tooltip("Velocity threshold to consider enemy as stuck")]
    public float stuckVelocityThreshold = 0.5f;

    [Tooltip("How long before considering enemy stuck")]
    public float stuckCheckDuration = 3f;

    [Tooltip("Wait time after being stuck before next waypoint")]
    public float stuckWaitDuration = 2f;

    [Header("Waypoint Timeout")]
    [Tooltip("Maximum time to reach waypoint before giving up")]
    public float waypointTimeout = 30f;

    [Tooltip("Wait time after timeout before next waypoint")]
    public float timeoutWaitDuration = 2f;

    [Header("Movement Speeds (Override)")]
    [Tooltip("Leave at 0 to use enemyData values")]
    public float patrolSpeedOverride = 0f;
    public float chaseSpeedOverride = 0f;

    [Header("Chase Settings")]
    [Tooltip("Distance to stop from target when chasing")]
    public float stopDistance = 1f;
    public float StopDistance => stopDistance;

    [Tooltip("Maximum chase distance before giving up")]
    public float maxChaseDistance = 50f;
    public float MaxChaseDistance => maxChaseDistance;

    // Movement speeds
    private float patrolSpeed;
    private float chaseSpeed;
    private float originalPatrolSpeed;
    private float originalChaseSpeed;

    // Speed boost
    private float speedBoostMultiplier = 1f;
    private float currentBaseSpeed = 0f; // Track the base speed (patrol or chase)

    // NavMeshAgent
    private NavMeshAgent agent;
    public NavMeshAgent Agent => agent;
    private EnemyVisionSystem visionSystem;

    // Patrol state
    // Logic moved to PatrolState
    
    // Stuck detection
    private float stuckTimer = 0f;
    private bool isStuck = false;

    // Public state queries
    public bool IsStuck => isStuck;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        visionSystem = GetComponent<EnemyVisionSystem>();
        ConfigureAgent();
        ConfigureRigidbody2D();
    }

    private void Update()
    {
        UpdateStuckDetection();
    }

    private void LateUpdate()
    {
        UpdateRotationBasedOnVision();
    }

    /// <summary>
    /// Rotates the enemy based on vision direction
    /// </summary>
    private void UpdateRotationBasedOnVision()
    {
        if (visionSystem == null) return;
        
        Vector3 visionDirection = visionSystem.GetVisionDirection();
        
        if (visionDirection.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(visionDirection.y, visionDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    private void Start()
    {
        // Initialize speeds from ScriptableObject or override
        if (enemyData != null)
        {
            patrolSpeed = patrolSpeedOverride > 0 ? patrolSpeedOverride : enemyData.patrolSpeed;
            chaseSpeed = chaseSpeedOverride > 0 ? chaseSpeedOverride : enemyData.chaseSpeed;
        }
        else
        {
            patrolSpeed = patrolSpeedOverride > 0 ? patrolSpeedOverride : 2f;
            chaseSpeed = chaseSpeedOverride > 0 ? chaseSpeedOverride : 3.5f;
            Debug.LogWarning($"{gameObject.name}: No EnemyDataSO assigned! Using default speeds.");
        }

        originalPatrolSpeed = patrolSpeed;
        originalChaseSpeed = chaseSpeed;
    }

    /// <summary>
    /// Configure NavMeshAgent for 2D movement
    /// </summary>
    private void ConfigureAgent()
    {
        if (agent == null) return;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = stopDistance;

        // Set movement properties from enemyData if available
        if (enemyData != null)
        {
            agent.acceleration = enemyData.acceleration;
            agent.angularSpeed = enemyData.angularSpeed;
        }
    }

    /// <summary>
    /// Configure Rigidbody2D for NavMesh compatibility
    /// </summary>
    private void ConfigureRigidbody2D()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    #region Movement Commands

    /// <summary>
    /// Move to a specific position
    /// </summary>
    public void MoveTo(Vector3 position, bool isChasing = false)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        float baseSpeed = isChasing ? chaseSpeed : patrolSpeed;
        SetSpeed(baseSpeed);
        
        agent.stoppingDistance = isChasing ? stopDistance : 0f;
        agent.SetDestination(position);
        
        // Reset stuck state when starting new move
        isStuck = false;
        stuckTimer = 0f;
    }

    /// <summary>
    /// Stop current movement
    /// </summary>
    public void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Check if agent has reached its destination
    /// </summary>
    public bool HasReachedDestination(float reachDistance = 0.1f)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;

        if (agent.pathPending) return false;
        
        float dist = agent.remainingDistance;
        return dist != float.PositiveInfinity && dist <= reachDistance;
    }

    #endregion

    #region Stuck Detection

    private void UpdateStuckDetection()
    {
        if (agent == null || !agent.enabled || !agent.hasPath) 
        {
            isStuck = false;
            stuckTimer = 0f;
            return;
        }

        // Stuck detection (velocity-based)
        float currentVelocity = agent.velocity.magnitude;

        if (currentVelocity < stuckVelocityThreshold)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckCheckDuration)
            {
                isStuck = true;
            }
        }
        else
        {
            stuckTimer = 0f;
            isStuck = false;
        }
    }

    #endregion

    #region Chase System (Legacy Wrapper)

    /// <summary>
    /// Chase a target position
    /// </summary>
    public void ChaseTarget(Vector3 targetPosition)
    {
        MoveTo(targetPosition, true);
    }

    #endregion

    #region Speed Management

    /// <summary>
    /// Set agent speed
    /// </summary>
    private void SetSpeed(float baseSpeed)
    {
        currentBaseSpeed = baseSpeed;
        if (agent != null)
        {
            agent.speed = currentBaseSpeed * speedBoostMultiplier;
        }
    }

    /// <summary>
    /// Apply speed boost multiplier
    /// </summary>
    public void ApplySpeedBoost(float multiplier)
    {
        speedBoostMultiplier = multiplier;
        // Re-apply speed using stored base speed
        if (agent != null)
        {
            agent.speed = currentBaseSpeed * speedBoostMultiplier;
        }
    }

    /// <summary>
    /// Remove speed boost
    /// </summary>
    public void RemoveSpeedBoost()
    {
        speedBoostMultiplier = 1f;
        // Re-apply speed using stored base speed
        if (agent != null)
        {
            agent.speed = currentBaseSpeed * speedBoostMultiplier;
        }
    }

    #endregion

    #region Public Queries

    /// <summary>
    /// Is enemy currently moving?
    /// </summary>
    public bool IsMoving()
    {
        return agent != null && agent.velocity.magnitude > 0.1f;
    }

    /// <summary>
    /// Get current velocity magnitude
    /// </summary>
    public float GetVelocity()
    {
        return agent != null ? agent.velocity.magnitude : 0f;
    }

    /// <summary>
    /// Get current movement direction
    /// </summary>
    public Vector3 GetMovementDirection()
    {
        return agent != null && agent.velocity.magnitude > 0.1f ? agent.velocity.normalized : Vector3.zero;
    }

    /// <summary>
    /// Enable/disable NavMeshAgent
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (agent != null)
        {
            agent.enabled = enabled;
        }
    }

    #endregion
}
