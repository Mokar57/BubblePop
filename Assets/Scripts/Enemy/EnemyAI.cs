using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour, ISpeedBoostable
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("Patrol Settings")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private List<Waypoint> patrolWaypoints = new List<Waypoint>();
    [SerializeField] private bool loopPatrol = true;
    [SerializeField] private float waypointReachDistance = 0.1f; // Very small distance for precise waypoint reaching
    [SerializeField] private bool randomPatrolOrder = false;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private bool showPatrolPath = true;
    [SerializeField] private float waypointTimeout = 30f; // Increased timeout to give velocity detection priority
    [SerializeField] private float timeoutWaitDuration = 2f; // Wait time after timeout before next waypoint
    
    [Header("Stuck Detection")]
    [SerializeField] private float stuckVelocityThreshold = 0.5f; // Speed threshold to consider as stuck
    [SerializeField] private float stuckCheckDuration = 3f; // How long to wait before considering stuck
    [SerializeField] private float stuckWaitDuration = 2f; // How long to wait after being stuck before moving to next waypoint
    
    [Header("Vision Settings")]
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private float visionAngle = 60f; // Total cone angle in degrees
    [SerializeField] private LayerMask obstacleLayerMask = -1; // What blocks vision
    [SerializeField] private bool drawVisionCone = true;
    [SerializeField] private float rotationSpeed = 90f; // Degrees per second for vision rotation
    [SerializeField] private float minMoveSpeedForRotation = 0.1f; // Minimum speed to start rotating vision
    
    [Header("AI Settings")]
    [SerializeField] private float followDistance = 15f; // Can follow further once spotted
    [SerializeField] private float stopDistance = 1f;
    [SerializeField] private float updateRate = 0.1f; // Update destination every 0.1 seconds for better performance
    [SerializeField] private bool persistentChase = true; // Once spotted, chase forever until manually reset
    [SerializeField] private float maxChaseDistance = 50f; // Maximum distance before giving up chase (if persistentChase is true)
    
    [Header("Proximity Detection")]
    [SerializeField] private float proximityDetectionRange = 5f; // Range to detect player without vision
    [SerializeField] private float proximityTriggerTime = 1f; // Time player must stay in range to trigger chase (default 1 second)
    [SerializeField] private bool enableProximityDetection = true; // Enable/disable proximity detection
    [SerializeField] private bool enableContactDetection = true; // Enable/disable contact detection
    
    [Header("Movement Settings")]
    [SerializeField] private float speed = 3.5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 120f;
    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Death Effects")]
    [SerializeField] private GameObject speedBoostZonePrefab;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    
    // Speed boost variables
    private float originalSpeed;
    private float originalPatrolSpeed;
    private float speedBoostMultiplier = 1f;
    private bool isSpeedBoosted = false;
    
    // Health variables
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnEnemyDeath;
    
    private NavMeshAgent agent;
    private float lastUpdateTime;
    private Vector3 lastTargetPosition;
    private bool hasSeenTarget = false;
    private float lastSeenTime;
    private Vector3 lastKnownTargetPosition;
    private Vector3 currentVisionDirection;
    private Vector3 lastPosition;
    
    // Patrol system variables
    private int currentWaypointIndex = 0;
    private bool isWaitingAtWaypoint = false;
    private float waypointWaitTimer = 0f;
    private bool isPatrolling = false;
    private Vector3 targetVisionDirection;
    private bool isRotatingVision = false;
    private float waypointStartTime = 0f; // Time when started moving to current waypoint
    private bool isWaitingAfterTimeout = false;
    private float timeoutWaitTimer = 0f;
    
    // Stuck detection variables
    private float stuckTimer = 0f; // Timer for how long velocity has been below threshold
    private bool isStuck = false; // Is the enemy currently stuck
    private bool isWaitingAfterStuck = false; // Is waiting after being stuck
    private float stuckWaitTimer = 0f; // Timer for waiting after being stuck
    
    // Proximity detection variables
    private float proximityTimer = 0f; // Timer for how long player has been in proximity
    private bool playerInProximity = false; // Is player currently in proximity range
    private bool proximityTriggered = false; // Has proximity detection triggered chase mode
    
    public enum AIState
    {
        Patrolling,
        Chasing,
        WaitingAtWaypoint,
        SearchingLastKnown,
        WaitingAfterTimeout,
        WaitingAfterStuck
    }
    
    [SerializeField] private AIState currentState = AIState.Patrolling;
    
    private void Start()
    {
        // Store original speed for speed boost functionality
        originalSpeed = speed;
        originalPatrolSpeed = patrolSpeed;
        
        // Initialize health
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        InitializeAgent();
        FindTarget();
        
        // Initialize vision direction to current transform up (forward in 2D)
        currentVisionDirection = transform.up;
        targetVisionDirection = currentVisionDirection;
        lastPosition = transform.position;
        
        // Initialize patrol system
        if (enablePatrol && patrolWaypoints.Count > 0)
        {
            currentState = AIState.Patrolling;
            isPatrolling = true;
            StartPatrol();
        }
    }
    
    private void InitializeAgent()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
            return;
        }
        
        // Configure agent for 2D movement
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        // Set movement properties
        agent.speed = speed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
        agent.stoppingDistance = stopDistance;
        
        // Configure Rigidbody2D for trigger detection (if present)
        ConfigureRigidbody2D();
    }
    
    private void ConfigureRigidbody2D()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            // Set to Kinematic to prevent physics from affecting NavMeshAgent
            rb.bodyType = RigidbodyType2D.Kinematic;
            
            // Freeze all rotation to prevent spinning
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            // Disable gravity (2D doesn't use it for kinematic anyway)
            rb.gravityScale = 0f;
            
            // Set interpolation for smoother movement (optional)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            // Set collision detection to Continuous for better collision detection
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
    
    private void FindTarget()
    {
        if (autoFindPlayer && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                // Try to find by component if tag doesn't exist
                PlayerControls playerScript = FindFirstObjectByType<PlayerControls>();
                if (playerScript != null)
                {
                    target = playerScript.transform;
                }
                else
                {
                    Debug.LogWarning("Player not found! Make sure the player has 'Player' tag or PlayerControls component.");
                }
            }
        }
    }
    
    private void StartPatrol()
    {
        if (patrolWaypoints.Count == 0) return;
        
        currentWaypointIndex = 0;
        isPatrolling = true;
        isWaitingAtWaypoint = false;
        isWaitingAfterTimeout = false; // Reset timeout wait state
        isWaitingAfterStuck = false; // Reset stuck wait state
        isStuck = false; // Reset stuck state
        stuckTimer = 0f; // Reset stuck timer
        
        // Set speed for patrolling
        if (agent != null)
            agent.speed = patrolSpeed;
        
        MoveToCurrentWaypoint();
    }
    
    private void MoveToCurrentWaypoint()
    {
        if (patrolWaypoints.Count == 0 || currentWaypointIndex >= patrolWaypoints.Count)
            return;
        
        Waypoint targetWaypoint = patrolWaypoints[currentWaypointIndex];
        if (targetWaypoint != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // Set stopping distance to 0 for precise waypoint reaching
            agent.stoppingDistance = 0f;
            agent.SetDestination(targetWaypoint.Position);
            currentState = AIState.Patrolling;
            waypointStartTime = Time.time; // Start timeout timer
            isWaitingAfterTimeout = false;
            isWaitingAfterStuck = false; // Reset stuck wait state
            isStuck = false; // Reset stuck state
            stuckTimer = 0f; // Reset stuck timer
        }
    }
    
    private void UpdatePatrol()
    {
        if (!enablePatrol || patrolWaypoints.Count == 0 || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;
        
        if (isWaitingAfterStuck)
        {
            // Handle waiting after being stuck
            stuckWaitTimer -= Time.deltaTime;
            
            if (stuckWaitTimer <= 0f)
            {
                // Move to next waypoint after stuck wait
                NextWaypoint();
            }
        }
        else if (isWaitingAfterTimeout)
        {
            // Handle waiting after timeout
            timeoutWaitTimer -= Time.deltaTime;
            
            if (timeoutWaitTimer <= 0f)
            {
                // Move to next waypoint after timeout wait
                NextWaypoint();
            }
        }
        else if (isWaitingAtWaypoint)
        {
            // Handle waiting at waypoint
            waypointWaitTimer -= Time.deltaTime;
            
            // Smoothly rotate vision towards target direction
            if (isRotatingVision)
            {
                currentVisionDirection = Vector3.Slerp(currentVisionDirection, targetVisionDirection, 
                    rotationSpeed * Time.deltaTime / 90f);
                
                // Check if rotation is complete
                if (Vector3.Angle(currentVisionDirection, targetVisionDirection) < 5f)
                {
                    currentVisionDirection = targetVisionDirection;
                    isRotatingVision = false;
                }
            }
            
            if (waypointWaitTimer <= 0f)
            {
                // Move to next waypoint
                NextWaypoint();
            }
        }
        else
        {
            // First check if reached current waypoint (normal waypoint reaching)
            if (currentWaypointIndex < patrolWaypoints.Count)
            {
                Waypoint currentWaypoint = patrolWaypoints[currentWaypointIndex];
                if (currentWaypoint != null)
                {
                    float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.Position);
                    
                    // Check if waypoint is reached - use very small distance or if agent has reached its destination
                    // Also check if agent is enabled and on NavMesh before accessing remainingDistance
                    bool agentReachedDestination = agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= 0.01f;
                    if (distanceToWaypoint <= waypointReachDistance || agentReachedDestination)
                    {
                        // Reached waypoint, start waiting
                        isWaitingAtWaypoint = true;
                        waypointWaitTimer = currentWaypoint.WaitTime;
                        currentState = AIState.WaitingAtWaypoint;
                        
                        // Set target vision direction
                        targetVisionDirection = currentWaypoint.WaitDirection;
                        isRotatingVision = true;
                        
                        // Stop moving and reset path
                        agent.ResetPath();
                        
                        // Reset stuck state
                        isStuck = false;
                        stuckTimer = 0f;
                        return;
                    }
                }
            }
            
            // Check velocity-based stuck detection (PRIORITY)
            float currentVelocity = agent.velocity.magnitude;
            
            if (currentVelocity < stuckVelocityThreshold)
            {
                // Velocity is below threshold, increment stuck timer
                stuckTimer += Time.deltaTime;
                
                if (stuckTimer >= stuckCheckDuration && !isStuck)
                {
                    // Enemy is stuck!
                    isStuck = true;
                    
                    // Enemy is truly stuck, start stuck wait period
                    isWaitingAfterStuck = true;
                    stuckWaitTimer = stuckWaitDuration;
                    currentState = AIState.WaitingAfterStuck;
                    
                    // Stop current movement
                    agent.ResetPath();
                    return;
                }
            }
            else
            {
                // Velocity is above threshold, reset stuck timer
                stuckTimer = 0f;
                isStuck = false;
            }
            
            // Check timeout for reaching waypoint (FALLBACK - only if not moving fast enough)
            // Only apply timeout if enemy is moving slowly or not at all
            if (currentVelocity < stuckVelocityThreshold && Time.time - waypointStartTime > waypointTimeout)
            {
                // Timeout reached and moving slowly, can't reach waypoint
                
                // Start timeout wait period
                isWaitingAfterTimeout = true;
                timeoutWaitTimer = timeoutWaitDuration;
                currentState = AIState.WaitingAfterTimeout;
                
                // Stop current movement
                agent.ResetPath();
                
                // Reset stuck state
                isStuck = false;
                stuckTimer = 0f;
                return;
            }
        }
    }
    
    private void NextWaypoint()
    {
        if (patrolWaypoints.Count == 0) return;
        
        if (randomPatrolOrder)
        {
            // Random waypoint selection
            int newIndex;
            do
            {
                newIndex = Random.Range(0, patrolWaypoints.Count);
            } while (newIndex == currentWaypointIndex && patrolWaypoints.Count > 1);
            
            currentWaypointIndex = newIndex;
        }
        else
        {
            // Sequential waypoint selection
            currentWaypointIndex++;
            
            if (currentWaypointIndex >= patrolWaypoints.Count)
            {
                if (loopPatrol)
                {
                    currentWaypointIndex = 0;
                }
                else
                {
                    // Stop patrolling
                    isPatrolling = false;
                    currentState = AIState.Patrolling; // Stay in patrol state but don't move
                    return;
                }
            }
        }
        
        isWaitingAtWaypoint = false;
        isWaitingAfterTimeout = false; // Reset timeout wait state
        isWaitingAfterStuck = false; // Reset stuck wait state
        isStuck = false; // Reset stuck state
        stuckTimer = 0f; // Reset stuck timer
        MoveToCurrentWaypoint();
    }
    
    private void Update()
    {
        // Don't update if dead or agent is disabled
        if (isDead || agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        
        if (target == null)
        {
            FindTarget();
        }

        // Check if target is visible
        bool canSeeTarget = target != null && CanSeeTarget();
        
        // Check proximity detection
        bool proximityDetected = false;
        if (enableProximityDetection && target != null)
        {
            proximityDetected = CheckProximityDetection();
        }

        // Handle state transitions
        if (canSeeTarget || proximityDetected)
        {
            // Player spotted or proximity triggered - switch to chase mode
            if (currentState != AIState.Chasing)
            {
                currentState = AIState.Chasing;
                hasSeenTarget = true;
                agent.speed = speed; // Use chase speed
                agent.stoppingDistance = stopDistance; // Reset stopping distance for chasing
                isPatrolling = false;
                isWaitingAtWaypoint = false;
                isWaitingAfterStuck = false; // Reset stuck wait state
                isStuck = false; // Reset stuck state
                stuckTimer = 0f; // Reset stuck timer
            }

            lastSeenTime = Time.time;
            lastKnownTargetPosition = target.position;
        }
        else if (hasSeenTarget && currentState == AIState.Chasing)
        {
            // Lost sight of target
            if (persistentChase)
            {
                // Continue chasing forever until too far
                float distanceToTarget = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
                if (distanceToTarget > maxChaseDistance)
                {
                    // Return to patrol
                    ReturnToPatrol();
                }
            }
            else
            {
                // Go to last known position
                currentState = AIState.SearchingLastKnown;
            }
        }
        else if (currentState == AIState.SearchingLastKnown)
        {
            // Check if reached last known position
            if (Vector3.Distance(transform.position, lastKnownTargetPosition) <= stopDistance)
            {
                // Return to patrol after searching
                ReturnToPatrol();
            }
        }

        // Update vision direction based on current state
        if (currentState != AIState.WaitingAtWaypoint && currentState != AIState.WaitingAfterTimeout)
        {
            UpdateVisionDirection();
        }

        // Update behavior based on current state
        switch (currentState)
        {
            case AIState.Patrolling:
            case AIState.WaitingAtWaypoint:
            case AIState.WaitingAfterTimeout:
            case AIState.WaitingAfterStuck:
                UpdatePatrol();
                break;
                
            case AIState.Chasing:
                if (Time.time - lastUpdateTime >= updateRate)
                {
                    UpdateChaseDestination();
                    lastUpdateTime = Time.time;
                }
                break;
                
            case AIState.SearchingLastKnown:
                if (Time.time - lastUpdateTime >= updateRate)
                {
                    UpdateSearchDestination();
                    lastUpdateTime = Time.time;
                }
                break;
        }
    }
    
    private void ReturnToPatrol()
    {
        hasSeenTarget = false;
        proximityTriggered = false; // Reset proximity detection
        proximityTimer = 0f;
        playerInProximity = false;
        
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        
        if (enablePatrol && patrolWaypoints.Count > 0)
        {
            // Find closest waypoint to return to
            float closestDistance = float.MaxValue;
            int closestWaypointIndex = 0;
            
            for (int i = 0; i < patrolWaypoints.Count; i++)
            {
                if (patrolWaypoints[i] != null)
                {
                    float distance = Vector3.Distance(transform.position, patrolWaypoints[i].Position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestWaypointIndex = i;
                    }
                }
            }
            
            currentWaypointIndex = closestWaypointIndex;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = 0f; // Reset stopping distance for patrol
            isPatrolling = true;
            isWaitingAtWaypoint = false;
            isWaitingAfterTimeout = false; // Reset timeout wait state
            isWaitingAfterStuck = false; // Reset stuck wait state
            isStuck = false; // Reset stuck state
            stuckTimer = 0f; // Reset stuck timer
            currentState = AIState.Patrolling;
            MoveToCurrentWaypoint();
        }
        else
        {
            // No patrol points, just stop
            agent.ResetPath();
            currentState = AIState.Patrolling;
        }
    }
    
    private void UpdateChaseDestination()
    {
        if (target == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        if (hasSeenTarget)
        {
            Vector3 destinationPosition;
            
            if (persistentChase)
            {
                // Once spotted, chase forever (unless too far away)
                if (distanceToTarget <= maxChaseDistance)
                {
                    destinationPosition = target.position;
                    lastKnownTargetPosition = target.position;
                    lastSeenTime = Time.time;
                }
                else
                {
                    // Target is too far away, return to patrol
                    ReturnToPatrol();
                    return;
                }
            }
            else
            {
                if (CanSeeTarget())
                {
                    // Can still see target
                    destinationPosition = target.position;
                    lastKnownTargetPosition = target.position;
                    lastSeenTime = Time.time;
                }
                else
                {
                    // Lost sight, go to last known position
                    destinationPosition = lastKnownTargetPosition;
                }
            }
            
            // Update destination if target moved significantly
            if (distanceToTarget > stopDistance && Vector3.Distance(destinationPosition, lastTargetPosition) > 0.5f)
            {
                agent.SetDestination(destinationPosition);
                lastTargetPosition = destinationPosition;
            }
        }
    }
    
    private void UpdateSearchDestination()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        
        // Move to last known position
        if (Vector3.Distance(lastKnownTargetPosition, lastTargetPosition) > 0.5f)
        {
            agent.SetDestination(lastKnownTargetPosition);
            lastTargetPosition = lastKnownTargetPosition;
        }
    }
    
    private void UpdateVisionDirection()
    {
        // Calculate movement direction
        Vector3 currentPosition = transform.position;
        Vector3 movementDirection = (currentPosition - lastPosition).normalized;
        float movementSpeed = Vector3.Distance(currentPosition, lastPosition) / Time.deltaTime;
        
        // Only update vision direction if moving fast enough
        if (movementSpeed >= minMoveSpeedForRotation && movementDirection.magnitude > 0.1f)
        {
            // Convert movement direction to 2D direction (ignore Z)
            Vector3 targetDirection = new Vector3(movementDirection.x, movementDirection.y, 0).normalized;
            
            // Smoothly rotate vision direction towards movement direction
            currentVisionDirection = Vector3.Slerp(currentVisionDirection, targetDirection, 
                rotationSpeed * Time.deltaTime / 90f); // Normalize rotation speed
            
            // Ensure the direction is normalized
            currentVisionDirection = currentVisionDirection.normalized;
        }
        
        // Update last position for next frame
        lastPosition = currentPosition;
    }
    
    private void UpdateDestination()
    {
        if (target == null || agent == null) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Check if we should follow the target
        bool shouldFollow = false;
        Vector3 destinationPosition = transform.position;
        
        if (hasSeenTarget)
        {
            if (persistentChase)
            {
                // Once spotted, chase forever (unless too far away)
                if (distanceToTarget <= maxChaseDistance)
                {
                    // Always follow current position when in persistent chase mode
                    destinationPosition = target.position;
                    shouldFollow = true;
                    
                    // Update last known position continuously while chasing
                    lastKnownTargetPosition = target.position;
                    lastSeenTime = Time.time; // Keep updating so we never "lose" the target
                }
                else
                {
                    // Target is too far away, stop chasing
                    hasSeenTarget = false;
                    agent.ResetPath();
                    return;
                }
            }
            else
            {
                // Original behavior - stop chasing after losing sight for some time
                if (CanSeeTarget())
                {
                    // Can still see target, follow current position
                    destinationPosition = target.position;
                    shouldFollow = distanceToTarget <= followDistance;
                    lastKnownTargetPosition = target.position;
                    lastSeenTime = Time.time;
                }
                else
                {
                    // Lost sight, go to last known position for a while
                    destinationPosition = lastKnownTargetPosition;
                    shouldFollow = Vector3.Distance(transform.position, lastKnownTargetPosition) > stopDistance;
                }
            }
        }
        
        // Update destination if we should follow and target moved significantly
        if (shouldFollow && Vector3.Distance(destinationPosition, lastTargetPosition) > 0.5f)
        {
            agent.SetDestination(destinationPosition);
            lastTargetPosition = destinationPosition;
        }
        else if (!shouldFollow)
        {
            agent.ResetPath();
        }
    }
    
    private bool CanSeeTarget()
    {
        if (target == null) return false;
        
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Check if target is within vision range
        if (distanceToTarget > visionRange)
            return false;
        
        // Use current vision direction instead of transform.up
        Vector3 visionForward = currentVisionDirection;
        float angleToTarget = Vector3.Angle(visionForward, directionToTarget);
        
        if (angleToTarget > visionAngle / 2f)
            return false;
        
        // Check if there are obstacles between enemy and target
        // Make sure we don't raycast against the enemy itself or the target
        int enemyLayer = gameObject.layer;
        int targetLayer = target.gameObject.layer;
        
        // Create a temporary layermask that excludes enemy and target layers
        int tempMask = obstacleLayerMask & ~(1 << enemyLayer) & ~(1 << targetLayer);
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, distanceToTarget, tempMask);
        
        // If raycast hits something, target is blocked
        return hit.collider == null;
    }
    
    private bool CheckProximityDetection()
    {
        if (!enableProximityDetection || target == null)
        {
            proximityTimer = 0f;
            playerInProximity = false;
            return false;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Check if player is within proximity range (but not in vision)
        if (distanceToTarget <= proximityDetectionRange && !CanSeeTarget())
        {
            if (!playerInProximity)
            {
                // Player just entered proximity
                playerInProximity = true;
                proximityTimer = 0f;
            }
            
            // Increment timer
            proximityTimer += Time.deltaTime;
            
            // Check if enough time has passed
            if (proximityTimer >= proximityTriggerTime && !proximityTriggered)
            {
                proximityTriggered = true;
                return true;
            }
        }
        else
        {
            // Player left proximity range or is visible
            playerInProximity = false;
            proximityTimer = 0f;
        }
        
        return proximityTriggered && currentState == AIState.Chasing;
    }
    
    // Contact detection methods
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enableContactDetection) return;
        
        // Check if the colliding object is the player
        if (other.transform == target || other.CompareTag("Player") || other.GetComponent<PlayerControls>() != null)
        {
            TriggerChaseMode("contact");
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableContactDetection) return;
        
        // Check if the colliding object is the player
        if (collision.transform == target || collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerControls>() != null)
        {
            TriggerChaseMode("contact");
        }
    }
    
    private void TriggerChaseMode(string reason)
    {
        if (target == null) return;
        
        // Force chase mode
        currentState = AIState.Chasing;
        hasSeenTarget = true;
        proximityTriggered = true; // Set this to true to maintain consistency
        agent.speed = speed; // Use chase speed
        isPatrolling = false;
        isWaitingAtWaypoint = false;
        isWaitingAfterStuck = false; // Reset stuck wait state
        isStuck = false; // Reset stuck state
        stuckTimer = 0f; // Reset stuck timer
        
        // Update last known position
        lastSeenTime = Time.time;
        lastKnownTargetPosition = target.position;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!drawVisionCone) return;
        
        // Draw vision cone
        Gizmos.color = hasSeenTarget ? Color.red : Color.yellow;
        
        // Use current vision direction instead of transform.up
        Vector3 forward = Application.isPlaying ? currentVisionDirection : transform.up;
        Vector3 leftBoundary = Quaternion.AngleAxis(-visionAngle / 2f, Vector3.forward) * forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(visionAngle / 2f, Vector3.forward) * forward;
        
        // Draw vision cone lines
        Gizmos.DrawRay(transform.position, leftBoundary * visionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * visionRange);
        
        // Draw arc for vision range
        Vector3 previousPoint = transform.position + leftBoundary * visionRange;
        for (int i = 1; i <= 20; i++)
        {
            float angle = -visionAngle / 2f + (visionAngle / 20f) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * forward;
            Vector3 point = transform.position + direction * visionRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
        
        // Draw arrow indicating vision direction
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Vector3 arrowEnd = transform.position + forward * (visionRange * 0.3f);
            Gizmos.DrawRay(transform.position, forward * (visionRange * 0.3f));
            
            // Draw arrow head
            Vector3 arrowLeft = Quaternion.AngleAxis(-135, Vector3.forward) * forward * 0.5f;
            Vector3 arrowRight = Quaternion.AngleAxis(135, Vector3.forward) * forward * 0.5f;
            Gizmos.DrawRay(arrowEnd, arrowLeft);
            Gizmos.DrawRay(arrowEnd, arrowRight);
        }
        
        // Draw follow distance range (when target is spotted)
        if (hasSeenTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, followDistance);
        }
        
        // Draw proximity detection range
        if (enableProximityDetection)
        {
            Gizmos.color = playerInProximity ? Color.orange : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityDetectionRange);
            
            // Draw timer indicator if player is in proximity
            if (playerInProximity)
            {
                float progress = proximityTimer / proximityTriggerTime;
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, progress);
                
                // Draw progress arc
                for (int i = 0; i < Mathf.RoundToInt(progress * 20); i++)
                {
                    float angle = (360f / 20f) * i;
                    Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
                    Vector3 point = transform.position + direction * (proximityDetectionRange + 0.5f);
                    Gizmos.DrawCube(point, Vector3.one * 0.1f);
                }
            }
        }
        
        // Draw stop distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        
        // Draw line to target if visible
        if (target != null && CanSeeTarget())
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
        
        // Draw line to last known position if following but can't see
        if (target != null && hasSeenTarget && !persistentChase && !CanSeeTarget())
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawLine(transform.position, lastKnownTargetPosition);
            Gizmos.DrawWireSphere(lastKnownTargetPosition, 0.5f);
        }
        
        // Draw persistent chase indicator
        if (hasSeenTarget && persistentChase)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, maxChaseDistance);
        }
    }
    
    // Public methods for external control
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        hasSeenTarget = false; // Reset vision state when target changes
    }
    
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        if (agent != null)
            agent.speed = speed;
    }
    
    public void StopFollowing()
    {
        if (agent != null)
            agent.ResetPath();
        hasSeenTarget = false;
    }
    
    public void ResumeFollowing()
    {
        // Will resume following when target comes into vision again
        hasSeenTarget = false;
    }
    
    public bool IsMoving()
    {
        return agent != null && agent.velocity.magnitude > 0.1f;
    }
    
    public float DistanceToTarget()
    {
        return target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
    }
    
    public bool CanSeeTargetPublic()
    {
        return CanSeeTarget();
    }
    
    public bool IsFollowingTarget()
    {
        if (persistentChase)
        {
            return hasSeenTarget && Vector3.Distance(transform.position, target.position) <= maxChaseDistance;
        }
        else
        {
            return hasSeenTarget && CanSeeTarget();
        }
    }
    
    public void SetVisionRange(float newRange)
    {
        visionRange = newRange;
    }
    
    public void SetVisionAngle(float newAngle)
    {
        visionAngle = Mathf.Clamp(newAngle, 0f, 360f);
    }
    
    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }
    
    public Vector3 GetVisionDirection()
    {
        return currentVisionDirection;
    }
    
    public void SetVisionDirection(Vector3 direction)
    {
        currentVisionDirection = direction.normalized;
    }
    
    public void SetPersistentChase(bool enabled)
    {
        persistentChase = enabled;
    }
    
    public void SetMaxChaseDistance(float distance)
    {
        maxChaseDistance = distance;
    }
    
    public void ResetChase()
    {
        hasSeenTarget = false;
        proximityTriggered = false;
        proximityTimer = 0f;
        playerInProximity = false;
        if (agent != null)
            agent.ResetPath();
    }
    
    public bool IsPersistentChaseEnabled()
    {
        return persistentChase;
    }
    
    public float GetMaxChaseDistance()
    {
        return maxChaseDistance;
    }
    
    public void ForceSpotTarget()
    {
        if (target != null)
        {
            hasSeenTarget = true;
            lastSeenTime = Time.time;
            lastKnownTargetPosition = target.position;
        }
    }
    
    // Proximity detection methods
    public void SetProximityDetectionRange(float range)
    {
        proximityDetectionRange = Mathf.Max(0f, range);
    }
    
    public void SetProximityTriggerTime(float time)
    {
        proximityTriggerTime = Mathf.Max(0.1f, time);
    }
    
    public void SetProximityDetectionEnabled(bool enabled)
    {
        enableProximityDetection = enabled;
        if (!enabled)
        {
            proximityTimer = 0f;
            playerInProximity = false;
            proximityTriggered = false;
        }
    }
    
    public void SetContactDetectionEnabled(bool enabled)
    {
        enableContactDetection = enabled;
    }
    
    public float GetProximityDetectionRange()
    {
        return proximityDetectionRange;
    }
    
    public float GetProximityTriggerTime()
    {
        return proximityTriggerTime;
    }
    
    public bool IsProximityDetectionEnabled()
    {
        return enableProximityDetection;
    }
    
    public bool IsContactDetectionEnabled()
    {
        return enableContactDetection;
    }
    
    public bool IsPlayerInProximity()
    {
        return playerInProximity;
    }
    
    public float GetProximityTimer()
    {
        return proximityTimer;
    }
    
    public bool IsProximityTriggered()
    {
        return proximityTriggered;
    }
    
    // Waypoint timeout methods
    public void SetWaypointTimeout(float timeout)
    {
        waypointTimeout = Mathf.Max(1f, timeout);
    }
    
    public void SetTimeoutWaitDuration(float duration)
    {
        timeoutWaitDuration = Mathf.Max(0f, duration);
    }
    
    public float GetWaypointTimeout()
    {
        return waypointTimeout;
    }
    
    public float GetTimeoutWaitDuration()
    {
        return timeoutWaitDuration;
    }
    
    public bool IsWaitingAfterTimeout()
    {
        return isWaitingAfterTimeout;
    }
    
    public AIState GetCurrentState()
    {
        return currentState;
    }
    
    #region ISpeedBoostable Implementation
    
    public void ApplySpeedBoost(float multiplier)
    {
        if (!isSpeedBoosted)
        {
            speedBoostMultiplier = multiplier;
            isSpeedBoosted = true;
            
            // Update both normal speed and patrol speed
            float newSpeed = originalSpeed * speedBoostMultiplier;
            float newPatrolSpeed = originalPatrolSpeed * speedBoostMultiplier;
            
            speed = newSpeed;
            patrolSpeed = newPatrolSpeed;
            
            // Update agent speed based on current AI state
            if (agent != null)
            {
                if (currentState == AIState.Patrolling || 
                    currentState == AIState.WaitingAtWaypoint || 
                    currentState == AIState.WaitingAfterTimeout || 
                    currentState == AIState.WaitingAfterStuck)
                {
                    // Use boosted patrol speed when patrolling
                    agent.speed = newPatrolSpeed;
                }
                else
                {
                    // Use boosted normal speed when chasing or searching
                    agent.speed = newSpeed;
                }
            }
        }
    }
    
    public void RemoveSpeedBoost()
    {
        if (isSpeedBoosted)
        {
            speedBoostMultiplier = 1f;
            isSpeedBoosted = false;
            
            // Restore original speeds
            speed = originalSpeed;
            patrolSpeed = originalPatrolSpeed;
            
            if (agent != null)
            {
                // Set agent speed based on current AI state
                if (currentState == AIState.Patrolling || 
                    currentState == AIState.WaitingAtWaypoint || 
                    currentState == AIState.WaitingAfterTimeout || 
                    currentState == AIState.WaitingAfterStuck)
                {
                    // Use patrol speed when patrolling
                    agent.speed = originalPatrolSpeed;
                }
                else
                {
                    // Use normal speed when chasing or searching
                    agent.speed = originalSpeed;
                }
            }
        }
    }
    
    public bool IsSpeedBoosted => isSpeedBoosted;
    
    #endregion
    
    #region Health System
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Visual feedback
        StartCoroutine(DamageFlash());
        
        // Trigger event
        OnHealthChanged?.Invoke(currentHealth);
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Trigger event
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Trigger death event FIRST (before disabling components)
        // This allows EnemyItemHolder to drop items while enemy is still active
        OnEnemyDeath?.Invoke();
        
        // Spawn SpeedBoostZone at death location
        if (speedBoostZonePrefab != null)
        {
            Instantiate(speedBoostZonePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} died but no SpeedBoostZone prefab assigned!");
        }
        
        // Disable components AFTER death event
        if (agent != null)
            agent.enabled = false;
        
        // Destroy the enemy after a short delay to allow for any death effects
        Destroy(gameObject, 0.1f);
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    // Public getters
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    
    #endregion
}