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

    [Tooltip("Maximum chase distance before giving up")]
    public float maxChaseDistance = 50f;

    // Movement speeds
    private float patrolSpeed;
    private float chaseSpeed;
    private float originalPatrolSpeed;
    private float originalChaseSpeed;

    // Speed boost
    private float speedBoostMultiplier = 1f;

    // NavMeshAgent
    private NavMeshAgent agent;

    // Patrol state
    private int currentWaypointIndex = 0;
    private bool isPatrolling = false;
    private bool isWaitingAtWaypoint = false;
    private float waypointWaitTimer = 0f;
    private float waypointStartTime = 0f;

    // Stuck detection
    private float stuckTimer = 0f;
    private bool isStuck = false;
    private bool isWaitingAfterStuck = false;
    private float stuckWaitTimer = 0f;

    // Timeout waiting
    private bool isWaitingAfterTimeout = false;
    private float timeoutWaitTimer = 0f;

    // Public state queries
    public bool IsPatrolling => isPatrolling;
    public bool IsWaitingAtWaypoint => isWaitingAtWaypoint;
    public bool IsStuck => isStuck;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        ConfigureAgent();
        ConfigureRigidbody2D();
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

    #region Patrol System

    /// <summary>
    /// Start patrol behavior
    /// </summary>
    public void StartPatrol()
    {
        if (!enablePatrol || patrolWaypoints.Count == 0) return;

        currentWaypointIndex = 0;
        isPatrolling = true;
        isWaitingAtWaypoint = false;
        isWaitingAfterTimeout = false;
        isWaitingAfterStuck = false;
        isStuck = false;
        stuckTimer = 0f;

        SetSpeed(patrolSpeed);
        MoveToCurrentWaypoint();
    }

    /// <summary>
    /// Update patrol behavior
    /// </summary>
    public void UpdatePatrol()
    {
        if (!enablePatrol || patrolWaypoints.Count == 0 || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        // Handle waiting after being stuck
        if (isWaitingAfterStuck)
        {
            stuckWaitTimer -= Time.deltaTime;
            if (stuckWaitTimer <= 0f)
            {
                NextWaypoint();
            }
            return;
        }

        // Handle waiting after timeout
        if (isWaitingAfterTimeout)
        {
            timeoutWaitTimer -= Time.deltaTime;
            if (timeoutWaitTimer <= 0f)
            {
                NextWaypoint();
            }
            return;
        }

        // Handle waiting at waypoint
        if (isWaitingAtWaypoint)
        {
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
            {
                NextWaypoint();
            }
            return;
        }

        // Check if reached waypoint
        if (currentWaypointIndex < patrolWaypoints.Count)
        {
            Waypoint currentWaypoint = patrolWaypoints[currentWaypointIndex];
            if (currentWaypoint != null)
            {
                float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.Position);
                bool agentReachedDestination = agent.enabled && agent.isOnNavMesh &&
                                              !agent.pathPending && agent.remainingDistance <= 0.01f;

                if (distanceToWaypoint <= waypointReachDistance || agentReachedDestination)
                {
                    // Reached waypoint
                    isWaitingAtWaypoint = true;
                    waypointWaitTimer = currentWaypoint.WaitTime;
                    agent.ResetPath();

                    // Reset stuck state
                    isStuck = false;
                    stuckTimer = 0f;
                    return;
                }
            }
        }

        // Stuck detection (velocity-based)
        float currentVelocity = agent.velocity.magnitude;

        if (currentVelocity < stuckVelocityThreshold)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckCheckDuration && !isStuck)
            {
                // Enemy is stuck
                isStuck = true;
                isWaitingAfterStuck = true;
                stuckWaitTimer = stuckWaitDuration;
                agent.ResetPath();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
            isStuck = false;
        }

        // Waypoint timeout (fallback)
        if (currentVelocity < stuckVelocityThreshold && Time.time - waypointStartTime > waypointTimeout)
        {
            isWaitingAfterTimeout = true;
            timeoutWaitTimer = timeoutWaitDuration;
            agent.ResetPath();
            isStuck = false;
            stuckTimer = 0f;
        }
    }

    /// <summary>
    /// Move to current waypoint
    /// </summary>
    private void MoveToCurrentWaypoint()
    {
        if (patrolWaypoints.Count == 0 || currentWaypointIndex >= patrolWaypoints.Count)
            return;

        Waypoint targetWaypoint = patrolWaypoints[currentWaypointIndex];
        if (targetWaypoint != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.stoppingDistance = 0f;
            agent.SetDestination(targetWaypoint.Position);
            waypointStartTime = Time.time;
            isWaitingAfterTimeout = false;
            isWaitingAfterStuck = false;
            isStuck = false;
            stuckTimer = 0f;
        }
    }

    /// <summary>
    /// Move to next waypoint
    /// </summary>
    private void NextWaypoint()
    {
        if (patrolWaypoints.Count == 0) return;

        if (randomPatrolOrder)
        {
            int newIndex;
            do
            {
                newIndex = Random.Range(0, patrolWaypoints.Count);
            } while (newIndex == currentWaypointIndex && patrolWaypoints.Count > 1);

            currentWaypointIndex = newIndex;
        }
        else
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= patrolWaypoints.Count)
            {
                if (loopPatrol)
                {
                    currentWaypointIndex = 0;
                }
                else
                {
                    isPatrolling = false;
                    return;
                }
            }
        }

        isWaitingAtWaypoint = false;
        isWaitingAfterTimeout = false;
        isWaitingAfterStuck = false;
        isStuck = false;
        stuckTimer = 0f;
        MoveToCurrentWaypoint();
    }

    /// <summary>
    /// Get current waypoint's wait direction for vision
    /// </summary>
    public Vector3 GetCurrentWaypointDirection()
    {
        if (currentWaypointIndex < patrolWaypoints.Count)
        {
            Waypoint currentWaypoint = patrolWaypoints[currentWaypointIndex];
            if (currentWaypoint != null)
            {
                return currentWaypoint.WaitDirection;
            }
        }
        return transform.up;
    }

    #endregion

    #region Chase System

    /// <summary>
    /// Chase a target position
    /// </summary>
    public void ChaseTarget(Vector3 targetPosition)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        SetSpeed(chaseSpeed);
        agent.stoppingDistance = stopDistance;
        agent.SetDestination(targetPosition);

        // Reset patrol state
        isPatrolling = false;
        isWaitingAtWaypoint = false;
        isWaitingAfterStuck = false;
        isStuck = false;
        stuckTimer = 0f;
    }

    /// <summary>
    /// Stop current movement
    /// </summary>
    public void StopMovement()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
        }
    }

    #endregion

    #region Speed Management

    /// <summary>
    /// Set agent speed
    /// </summary>
    private void SetSpeed(float speed)
    {
        if (agent != null)
        {
            agent.speed = speed * speedBoostMultiplier;
        }
    }

    /// <summary>
    /// Apply speed boost multiplier
    /// </summary>
    public void ApplySpeedBoost(float multiplier)
    {
        speedBoostMultiplier = multiplier;

        // Update current speed
        if (isPatrolling)
        {
            SetSpeed(patrolSpeed);
        }
        else
        {
            SetSpeed(chaseSpeed);
        }
    }

    /// <summary>
    /// Remove speed boost
    /// </summary>
    public void RemoveSpeedBoost()
    {
        speedBoostMultiplier = 1f;

        // Update current speed
        if (isPatrolling)
        {
            SetSpeed(patrolSpeed);
        }
        else
        {
            SetSpeed(chaseSpeed);
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
