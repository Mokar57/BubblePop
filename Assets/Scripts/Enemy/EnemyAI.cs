using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy AI Coordinator
/// Manages state machine and coordinates between component systems
/// Refactored from 1496-line monolith to ~350-line coordinator
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyVisionSystem))]
[RequireComponent(typeof(EnemyMovementController))]
[RequireComponent(typeof(EnemySoundDetector))]
[RequireComponent(typeof(EnemyStunController))]
[RequireComponent(typeof(EnemySoundInvestigator))]
public class EnemyAI : MonoBehaviour, ISpeedBoostable
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing all enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Target Settings")]
    [Tooltip("Auto-find player on start")]
    public bool autoFindPlayer = true;

    [Header("Chase Settings")]
    [Tooltip("Once spotted, chase forever until too far")]
    public bool persistentChase = true;

    [Header("Proximity Detection")]
    [Tooltip("Enable proximity detection (detect player when close even without vision)")]
    public bool enableProximityDetection = true;

    [Tooltip("Time player must stay in proximity before triggering chase")]
    public float proximityTriggerTime = 1f;

    [Header("Contact Detection")]
    [Tooltip("Enable contact detection (trigger chase on collision with player)")]
    public bool enableContactDetection = true;

    [Header("Debug")]
    [Tooltip("Show state changes in console")]
    public bool debugMode = false;

    // Component references
    private EnemyHealth health;
    private EnemyVisionSystem visionSystem;
    private EnemyMovementController movementController;
    private EnemySoundDetector soundDetector;
    private EnemyStunController stunController;
    private EnemySoundInvestigator soundInvestigator;
    private NavMeshAgent agent;

    // State machine
    private AIState currentState = AIState.Patrolling;
    private AIState stateBeforeStun;

    // Target tracking
    private Transform target;
    private bool hasSeenTarget = false;
    private float lastSeenTime;
    private Vector3 lastKnownTargetPosition;

    // Proximity detection
    private float proximityTimer = 0f;
    private bool playerInProximity = false;
    private bool proximityTriggered = false;

    // Speed boost
    private bool isSpeedBoosted = false;
    private float speedBoostMultiplier = 1f;

    // Update timing
    private float lastUpdateTime;
    private const float UPDATE_RATE = 0.1f;

    #region Initialization

    private void Awake()
    {
        // Get component references
        health = GetComponent<EnemyHealth>();
        visionSystem = GetComponent<EnemyVisionSystem>();
        movementController = GetComponent<EnemyMovementController>();
        soundDetector = GetComponent<EnemySoundDetector>();
        stunController = GetComponent<EnemyStunController>();
        soundInvestigator = GetComponent<EnemySoundInvestigator>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Subscribe to events
        stunController.OnStunEnded += HandleStunEnded;
        health.OnEnemyDeath += HandleDeath;
        soundInvestigator.OnMovingToInvestigate += HandleMovingToInvestigate;
        soundInvestigator.OnInvestigationStarted += HandleInvestigationStarted;
        soundInvestigator.OnInvestigationEnded += HandleInvestigationEnded;

        // Find target
        if (autoFindPlayer)
        {
            FindTarget();
        }

        // Start patrol if enabled
        if (movementController.enablePatrol)
        {
            currentState = AIState.Patrolling;
            movementController.StartPatrol();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (stunController != null)
            stunController.OnStunEnded -= HandleStunEnded;
        if (health != null)
            health.OnEnemyDeath -= HandleDeath;
        if (soundInvestigator != null)
        {
            soundInvestigator.OnMovingToInvestigate -= HandleMovingToInvestigate;
            soundInvestigator.OnInvestigationStarted -= HandleInvestigationStarted;
            soundInvestigator.OnInvestigationEnded -= HandleInvestigationEnded;
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        // Don't update if dead or stunned
        if (health.IsDead) return;

        // Handle stunned state separately
        if (stunController.IsStunned())
        {
            currentState = AIState.Stunned;
            stunController.UpdateStun();

            // Check if can see player while stunned (early exit)
            if (visionSystem.CanSeeTarget())
            {
                stunController.ForceExitStun();
                TransitionToChase();
            }

            return;
        }

        // Find target if lost
        if (target == null && autoFindPlayer)
        {
            FindTarget();
        }

        // Check vision
        bool canSeeTarget = target != null && visionSystem.CanSeeTarget();

        // Check proximity detection
        bool proximityDetected = false;
        if (enableProximityDetection && target != null)
        {
            proximityDetected = CheckProximityDetection();
        }

        // Handle state transitions
        HandleStateTransitions(canSeeTarget, proximityDetected);

        // Update vision direction based on movement
        UpdateVisionDirection();

        // Update current state behavior
        UpdateStateBehavior();
    }

    #endregion

    #region State Machine

    private void HandleStateTransitions(bool canSeeTarget, bool proximityDetected)
    {
        // Don't interrupt investigation
        if (currentState == AIState.Investigating || soundInvestigator.IsMovingToInvestigate())
            return;

        // Spotted player or proximity triggered - switch to chase
        if (canSeeTarget || proximityDetected)
        {
            if (currentState != AIState.Chasing)
            {
                TransitionToChase();
            }

            lastSeenTime = Time.time;
            if (target != null)
            {
                lastKnownTargetPosition = target.position;
            }
        }
        // Lost sight while chasing
        else if (hasSeenTarget && currentState == AIState.Chasing)
        {
            if (persistentChase)
            {
                // Continue chasing until too far
                float distanceToTarget = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
                if (distanceToTarget > movementController.maxChaseDistance)
                {
                    ReturnToPatrol();
                }
            }
            else
            {
                // Go to last known position
                currentState = AIState.SearchingLastKnown;
            }
        }
        // Reached last known position while searching
        else if (currentState == AIState.SearchingLastKnown)
        {
            if (Vector3.Distance(transform.position, lastKnownTargetPosition) <= movementController.stopDistance)
            {
                ReturnToPatrol();
            }
        }
    }

    private void UpdateStateBehavior()
    {
        switch (currentState)
        {
            case AIState.Patrolling:
            case AIState.WaitingAtWaypoint:
            case AIState.WaitingAfterTimeout:
            case AIState.WaitingAfterStuck:
                movementController.UpdatePatrol();
                UpdatePatrolVision();
                break;

            case AIState.Chasing:
                if (Time.time - lastUpdateTime >= UPDATE_RATE)
                {
                    UpdateChaseDestination();
                    lastUpdateTime = Time.time;
                }
                break;

            case AIState.SearchingLastKnown:
                if (Time.time - lastUpdateTime >= UPDATE_RATE)
                {
                    UpdateSearchDestination();
                    lastUpdateTime = Time.time;
                }
                break;
        }
    }

    private void TransitionToChase()
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Transitioning to Chase");

        currentState = AIState.Chasing;
        hasSeenTarget = true;
    }

    private void ReturnToPatrol()
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Returning to patrol");

        hasSeenTarget = false;
        proximityTriggered = false;
        proximityTimer = 0f;
        playerInProximity = false;

        if (movementController.enablePatrol)
        {
            currentState = AIState.Patrolling;
            movementController.StartPatrol();
        }
        else
        {
            movementController.StopMovement();
            currentState = AIState.Patrolling;
        }
    }

    #endregion

    #region Movement Updates

    private void UpdateChaseDestination()
    {
        if (target == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 destinationPosition = target.position;

        if (persistentChase || visionSystem.CanSeeTarget())
        {
            lastKnownTargetPosition = target.position;
            lastSeenTime = Time.time;
        }

        movementController.ChaseTarget(destinationPosition);
    }

    private void UpdateSearchDestination()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        movementController.ChaseTarget(lastKnownTargetPosition);
    }

    private void UpdateVisionDirection()
    {
        // Check if waiting at waypoint (use movement controller's flag)
        if (movementController.IsWaitingAtWaypoint)
        {
            // Rotate towards waypoint direction
            Vector3 waypointDirection = movementController.GetCurrentWaypointDirection();
            if (enemyData != null)
            {
                visionSystem.RotateVisionTowards(waypointDirection, enemyData.rotationSpeed);
            }
            return;
        }

        // Update vision based on movement
        if (movementController.IsMoving())
        {
            Vector3 moveDirection = movementController.GetMovementDirection();
            if (moveDirection.magnitude > 0.1f && enemyData != null)
            {
                visionSystem.RotateVisionTowards(moveDirection, enemyData.rotationSpeed);
            }
        }
    }

    private void UpdatePatrolVision()
    {
        // Vision is handled in UpdateVisionDirection
    }

    #endregion

    #region Proximity Detection

    private bool CheckProximityDetection()
    {
        if (!enableProximityDetection || target == null || enemyData == null)
        {
            proximityTimer = 0f;
            playerInProximity = false;
            return false;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Check if player is within proximity range (but not in vision)
        if (distanceToTarget <= enemyData.proximityRadius && !visionSystem.CanSeeTarget())
        {
            if (!playerInProximity)
            {
                playerInProximity = true;
                proximityTimer = 0f;
            }

            proximityTimer += Time.deltaTime;

            if (proximityTimer >= proximityTriggerTime && !proximityTriggered)
            {
                proximityTriggered = true;
                return true;
            }
        }
        else
        {
            playerInProximity = false;
            proximityTimer = 0f;
        }

        return proximityTriggered && currentState == AIState.Chasing;
    }

    #endregion

    #region Contact Detection

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enableContactDetection) return;

        if (other.transform == target || other.CompareTag("Player") || other.GetComponent<PlayerControls>() != null)
        {
            TransitionToChase();
            proximityTriggered = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableContactDetection) return;

        if (collision.transform == target || collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerControls>() != null)
        {
            TransitionToChase();
            proximityTriggered = true;
        }
    }

    #endregion

    #region Event Handlers

    private void HandleMovingToInvestigate()
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Moving to investigate sound");

        currentState = AIState.Investigating;
    }

    private void HandleInvestigationStarted()
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Reached investigation point, searching area");
    }

    private void HandleInvestigationEnded()
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Investigation ended, returning to patrol");

        ReturnToPatrol();
    }

    private void HandleStunEnded()
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Stun ended");

        // Don't interrupt investigation
        if (currentState == AIState.Investigating || soundInvestigator.IsInvestigating() || soundInvestigator.IsMovingToInvestigate())
        {
            currentState = AIState.Investigating;
            return;
        }

        ReturnToPatrol();
    }

    private void HandleDeath()
    {
        // Disable all components
        this.enabled = false;
    }

    #endregion

    #region Target Management

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            visionSystem.SetTarget(target);
            return;
        }

        PlayerControls playerScript = FindFirstObjectByType<PlayerControls>();
        if (playerScript != null)
        {
            target = playerScript.transform;
            visionSystem.SetTarget(target);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        visionSystem.SetTarget(newTarget);
        hasSeenTarget = false;
    }

    #endregion

    #region ISpeedBoostable Implementation

    public void ApplySpeedBoost(float multiplier)
    {
        if (!isSpeedBoosted)
        {
            speedBoostMultiplier = multiplier;
            isSpeedBoosted = true;
            movementController.ApplySpeedBoost(multiplier);
        }
    }

    public void RemoveSpeedBoost()
    {
        if (isSpeedBoosted)
        {
            speedBoostMultiplier = 1f;
            isSpeedBoosted = false;
            movementController.RemoveSpeedBoost();
        }
    }

    public bool IsSpeedBoosted => isSpeedBoosted;

    #endregion

    #region Public API (for backward compatibility)

    public AIState GetCurrentState() => currentState;
    public bool CanSeeTargetPublic() => visionSystem.CanSeeTarget();
    public Vector3 GetVisionDirection() => visionSystem.GetVisionDirection();
    public void SetVisionDirection(Vector3 direction) => visionSystem.SetVisionDirection(direction);
    public bool IsMoving() => movementController.IsMoving();
    public float DistanceToTarget() => target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
    public void ResetChase()
    {
        hasSeenTarget = false;
        proximityTriggered = false;
        proximityTimer = 0f;
        playerInProximity = false;
    }

    #endregion
}
