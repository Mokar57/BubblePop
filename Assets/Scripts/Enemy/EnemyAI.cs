using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    
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
    
    [Header("Movement Settings")]
    [SerializeField] private float speed = 3.5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 120f;
    
    private NavMeshAgent agent;
    private float lastUpdateTime;
    private Vector3 lastTargetPosition;
    private bool hasSeenTarget = false;
    private float lastSeenTime;
    private Vector3 lastKnownTargetPosition;
    private Vector3 currentVisionDirection;
    private Vector3 lastPosition;
    
    private void Start()
    {
        InitializeAgent();
        FindTarget();
        
        // Initialize vision direction to current transform up (forward in 2D)
        currentVisionDirection = transform.up;
        lastPosition = transform.position;
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
    }
    
    private void FindTarget()
    {
        if (autoFindPlayer && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("Enemy found player: " + player.name);
            }
            else
            {
                // Try to find by component if tag doesn't exist
                PlayerControls playerScript = FindFirstObjectByType<PlayerControls>();
                if (playerScript != null)
                {
                    target = playerScript.transform;
                    Debug.Log("Enemy found player by script: " + playerScript.name);
                }
                else
                {
                    Debug.LogWarning("Player not found! Make sure the player has 'Player' tag or PlayerControls component.");
                }
            }
        }
    }
    
    private void Update()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }
        
        if (agent == null) return;
        
        // Update vision direction based on movement
        UpdateVisionDirection();
        
        // Check if target is visible
        bool canSeeTarget = CanSeeTarget();
        
        if (canSeeTarget)
        {
            hasSeenTarget = true;
            lastSeenTime = Time.time;
            lastKnownTargetPosition = target.position;
        }
        
        // Update destination periodically for better performance
        if (Time.time - lastUpdateTime >= updateRate)
        {
            UpdateDestination();
            lastUpdateTime = Time.time;
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
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleLayerMask);
        
        // If raycast hits something, target is blocked
        return hit.collider == null;
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
}