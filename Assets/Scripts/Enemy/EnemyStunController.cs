using UnityEngine;

/// <summary>
/// Handles enemy stun state when affected by area effects (door slams, etc.)
/// Pulls enemy to stun area center and applies random rotation
/// </summary>
public class EnemyStunController : MonoBehaviour
{
    [Header("Stun Settings")]
    [Tooltip("Random rotation speed range during stun (min, max degrees/second)")]
    public Vector2 rotationSpeedRange = new Vector2(90f, 180f);

    [Tooltip("Distance threshold to consider reached stun center")]
    public float reachCenterDistance = 0.5f;

    // Stun state
    private bool isStunned = false;
    private Vector3 stunCenter;
    private float stunEndTime;
    private bool hasReachedStunCenter = false;
    private float currentRotationSpeed;

    // Cached components
    private EnemyMovementController movementController;
    private EnemyVisionSystem visionSystem;

    // State before stun
    public System.Action OnStunEnded;

    private void Awake()
    {
        movementController = GetComponent<EnemyMovementController>();
        visionSystem = GetComponent<EnemyVisionSystem>();
    }

    /// <summary>
    /// Enter stunned state
    /// </summary>
    public void EnterStun(Vector3 centerPosition, float duration)
    {
        if (isStunned) return;

        isStunned = true;
        stunCenter = centerPosition;
        stunEndTime = Time.time + duration;
        hasReachedStunCenter = false;

        // Pick random rotation speed
        currentRotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        currentRotationSpeed *= Random.value > 0.5f ? 1f : -1f; // Random direction

        // Move to stun center
        if (movementController != null)
        {
            movementController.ChaseTarget(stunCenter);
        }
    }

    private void Update()
    {
        if (!isStunned) return;

        // Check if reached center
        if (!hasReachedStunCenter)
        {
            float distanceToCenter = Vector3.Distance(transform.position, stunCenter);

            if (distanceToCenter <= reachCenterDistance)
            {
                hasReachedStunCenter = true;

                // Stop movement
                if (movementController != null)
                {
                    movementController.StopMovement();
                }
            }
            else
            {
                // Still moving to center, update vision direction to movement direction
                if (movementController != null && visionSystem != null)
                {
                    Vector3 moveDir = movementController.GetMovementDirection();
                    if (moveDir.magnitude > 0.1f)
                    {
                        visionSystem.SetVisionDirection(moveDir);
                    }
                }
            }
        }
        else
        {
            // At center, apply random rotation to vision
            if (visionSystem != null)
            {
                Vector3 currentVision = visionSystem.GetVisionDirection();
                Quaternion rotation = Quaternion.Euler(0, 0, currentRotationSpeed * Time.deltaTime);
                Vector3 newVision = rotation * currentVision;
                visionSystem.SetVisionDirection(newVision);
            }
        }

        // Check if stun duration ended
        if (Time.time >= stunEndTime)
        {
            ExitStun();
        }
    }

    /// <summary>
    /// Exit stunned state
    /// </summary>
    public void ExitStun()
    {
        if (!isStunned) return;

        isStunned = false;
        hasReachedStunCenter = false;

        // Notify listeners that stun ended
        OnStunEnded?.Invoke();
    }

    /// <summary>
    /// Force exit stun immediately
    /// </summary>
    public void ForceExitStun()
    {
        ExitStun();
    }

    /// <summary>
    /// Check if currently stunned
    /// </summary>
    public bool IsStunned()
    {
        return isStunned;
    }

    /// <summary>
    /// Get remaining stun time
    /// </summary>
    public float GetRemainingStunTime()
    {
        if (!isStunned) return 0f;
        return Mathf.Max(0f, stunEndTime - Time.time);
    }

    /// <summary>
    /// Check if reached stun center
    /// </summary>
    public bool HasReachedStunCenter()
    {
        return hasReachedStunCenter;
    }
}
