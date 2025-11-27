using UnityEngine;

/// <summary>
/// Handles enemy behavior when investigating sounds
/// Moves to sound location and looks around for a duration
/// </summary>
[RequireComponent(typeof(EnemyMovementController))]
[RequireComponent(typeof(EnemyVisionSystem))]
[RequireComponent(typeof(EnemySoundDetector))]
public class EnemySoundInvestigator : MonoBehaviour
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Investigation Settings (Override)")]
    [Tooltip("Leave at 0 to use enemyData values")]
    public float investigationDurationOverride = 0f;
    public float rotationIntervalOverride = 0f;

    [Header("Search Movement")]
    [Tooltip("Radius around sound point to search")]
    public float searchRadius = 3f;
    
    [Tooltip("Time between moving to new search positions")]
    public float searchMoveInterval = 2f;
    
    [Tooltip("Speed multiplier when searching (relative to patrol speed)")]
    public float searchSpeedMultiplier = 0.7f;

    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool debugMode = false;

    // Component references
    private EnemyMovementController movementController;
    private EnemyVisionSystem visionSystem;
    private EnemySoundDetector soundDetector;
    private EnemyStunController stunController;

    // Investigation state
    private bool isInvestigating = false;
    private bool isMovingToInvestigate = false;
    private Vector3 investigationPoint;
    private float investigationEndTime;
    private float lastRotationTime;
    private float lastSearchMoveTime;
    private Vector3 currentSearchTarget;
    private Vector3 targetLookDirection;

    // Runtime values
    private float investigationDuration;
    private float rotationInterval;

    // Events
    public System.Action OnInvestigationStarted;
    public System.Action OnInvestigationEnded;
    public System.Action OnMovingToInvestigate;

    private void Awake()
    {
        movementController = GetComponent<EnemyMovementController>();
        visionSystem = GetComponent<EnemyVisionSystem>();
        soundDetector = GetComponent<EnemySoundDetector>();
        stunController = GetComponent<EnemyStunController>();
    }

    private void Start()
    {
        // Initialize values from ScriptableObject or override
        if (enemyData != null)
        {
            investigationDuration = investigationDurationOverride > 0 ? investigationDurationOverride : enemyData.soundInvestigationDuration;
            rotationInterval = rotationIntervalOverride > 0 ? rotationIntervalOverride : enemyData.soundSearchRotationInterval;
        }
        else
        {
            investigationDuration = investigationDurationOverride > 0 ? investigationDurationOverride : 3f;
            rotationInterval = rotationIntervalOverride > 0 ? rotationIntervalOverride : 1f;
            Debug.LogWarning($"{gameObject.name}: No EnemyDataSO assigned! Using default investigation settings.");
        }

        // Subscribe to sound detection
        if (soundDetector != null)
        {
            soundDetector.OnSoundDetected += HandleSoundDetected;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe
        if (soundDetector != null)
        {
            soundDetector.OnSoundDetected -= HandleSoundDetected;
        }
    }

    private void Update()
    {
        // Check if should start investigating (reached point)
        if (isMovingToInvestigate && !isInvestigating)
        {
            if (HasReachedInvestigationPoint())
            {
                StartInvestigation();
            }
        }

        // Update investigation if active
        if (isInvestigating)
        {
            UpdateInvestigation();
        }
    }

    /// <summary>
    /// Handle sound detected event
    /// </summary>
    private void HandleSoundDetected(Vector2 soundPosition)
    {
        // Don't interrupt if already investigating
        if (isInvestigating || isMovingToInvestigate)
            return;

        if (debugMode)
            Debug.Log($"{gameObject.name}: Heard sound at {soundPosition}, moving to investigate");

        // If enemy is stunned, force exit stun to start investigation
        if (stunController != null && stunController.IsStunned())
            stunController.ForceExitStun();

        // Start moving to sound position
        investigationPoint = soundPosition;
        isMovingToInvestigate = true;
        movementController.ChaseTarget(soundPosition);

        // Notify AI coordinator that we're investigating
        OnMovingToInvestigate?.Invoke();
    }

    /// <summary>
    /// Start investigating at current position
    /// Call this when enemy reaches sound location
    /// </summary>
    public void StartInvestigation()
    {
        if (isInvestigating) return;

        if (debugMode)
            Debug.Log($"{gameObject.name}: Started investigating at {investigationPoint}");

        isInvestigating = true;
        investigationEndTime = Time.time + investigationDuration;
        lastRotationTime = Time.time;
        lastSearchMoveTime = Time.time;
        currentSearchTarget = investigationPoint;
        targetLookDirection = visionSystem.GetVisionDirection(); // Start with current direction

        // Trigger event
        OnInvestigationStarted?.Invoke();
        
        // Immediately pick first search point
        PickNewSearchPoint();
    }

    /// <summary>
    /// Update investigation behavior - actively move around and look
    /// </summary>
    private void UpdateInvestigation()
    {
        // Move to new search positions periodically
        if (Time.time >= lastSearchMoveTime + searchMoveInterval)
        {
            // Check if reached current search target or time to move
            float distToTarget = Vector3.Distance(transform.position, currentSearchTarget);
            if (distToTarget <= movementController.stopDistance + 0.5f)
            {
                PickNewSearchPoint();
            }
        }

        // Pick new look direction periodically
        if (Time.time >= lastRotationTime + rotationInterval)
        {
            lastRotationTime = Time.time;
            
            // Choose new target direction to look at
            if (movementController.IsMoving())
            {
                Vector3 moveDir = movementController.GetMovementDirection();
                if (moveDir.magnitude > 0.1f)
                {
                    // Add some random offset to look around while moving
                    float randomOffset = Random.Range(-45f, 45f);
                    Quaternion rotation = Quaternion.Euler(0, 0, randomOffset);
                    targetLookDirection = (rotation * moveDir).normalized;
                }
            }
            else
            {
                // Standing still, pick random direction to look
                float randomAngle = Random.Range(0f, 360f);
                targetLookDirection = new Vector3(
                    Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                    Mathf.Sin(randomAngle * Mathf.Deg2Rad),
                    0f
                );
            }
        }
        
        // Smoothly rotate vision towards target direction
        float rotSpeed = enemyData != null ? enemyData.rotationSpeed : 90f;
        visionSystem.RotateVisionTowards(targetLookDirection, rotSpeed);

        // Check if investigation time is up
        if (Time.time >= investigationEndTime)
        {
            StopInvestigation();
        }
    }

    /// <summary>
    /// Pick a new random point within search radius to move to
    /// </summary>
    private void PickNewSearchPoint()
    {
        lastSearchMoveTime = Time.time;
        
        // Generate random point within search radius around original investigation point
        float randomAngle = Random.Range(0f, 360f);
        float randomDist = Random.Range(0f, searchRadius);
        
        Vector3 offset = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDist,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDist,
            0f
        );
        
        currentSearchTarget = investigationPoint + offset;
        movementController.ChaseTarget(currentSearchTarget);
        
        if (debugMode)
            Debug.Log($"{gameObject.name}: Moving to search point {currentSearchTarget}");
    }

    /// <summary>
    /// Stop investigation
    /// </summary>
    public void StopInvestigation()
    {
        if (!isInvestigating) return;

        if (debugMode)
            Debug.Log($"{gameObject.name}: Finished investigating");

        isInvestigating = false;
        isMovingToInvestigate = false;
        movementController.StopMovement();

        // Trigger event
        OnInvestigationEnded?.Invoke();
    }

    /// <summary>
    /// Check if currently investigating
    /// </summary>
    public bool IsInvestigating()
    {
        return isInvestigating;
    }

    /// <summary>
    /// Check if moving to investigation point
    /// </summary>
    public bool IsMovingToInvestigate()
    {
        return isMovingToInvestigate;
    }

    /// <summary>
    /// Get investigation point
    /// </summary>
    public Vector3 GetInvestigationPoint()
    {
        return investigationPoint;
    }

    /// <summary>
    /// Check if reached investigation point
    /// </summary>
    public bool HasReachedInvestigationPoint()
    {
        float distance = Vector3.Distance(transform.position, investigationPoint);
        return distance <= movementController.stopDistance;
    }

    /// <summary>
    /// Get remaining investigation time
    /// </summary>
    public float GetRemainingInvestigationTime()
    {
        if (!isInvestigating) return 0f;
        return Mathf.Max(0f, investigationEndTime - Time.time);
    }
}
