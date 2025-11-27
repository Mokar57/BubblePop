using UnityEngine;

/// <summary>
/// Handles enemy vision detection using cone-based vision
/// Checks line of sight, angle, and range to detect targets
/// </summary>
public class EnemyVisionSystem : MonoBehaviour
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Vision Settings (Override)")]
    [Tooltip("Leave at 0 to use enemyData values")]
    public float visionRangeOverride = 0f;
    public float visionAngleOverride = 0f;

    [Header("Detection")]
    [Tooltip("What blocks vision (walls, obstacles)")]
    public LayerMask obstacleLayerMask = -1;

    [Header("Debug")]
    [Tooltip("Draw vision cone in editor")]
    public bool drawVisionCone = true;

    // Vision parameters
    private float visionRange;
    private float visionAngle;

    // Current vision direction (separate from transform.up)
    private Vector3 currentVisionDirection;

    // Target tracking
    private Transform target;

    private void Start()
    {
        // Initialize vision parameters from ScriptableObject or override
        if (enemyData != null)
        {
            visionRange = visionRangeOverride > 0 ? visionRangeOverride : enemyData.visionRange;
            visionAngle = visionAngleOverride > 0 ? visionAngleOverride : enemyData.visionAngle;
        }
        else
        {
            visionRange = visionRangeOverride > 0 ? visionRangeOverride : 8f;
            visionAngle = visionAngleOverride > 0 ? visionAngleOverride : 60f;
            Debug.LogWarning($"{gameObject.name}: No EnemyDataSO assigned! Using default vision settings.");
        }

        // Initialize vision direction to current transform up (forward in 2D)
        currentVisionDirection = transform.up;

        // Auto-find player target
        FindTarget();
    }

    /// <summary>
    /// Check if target is within vision cone and line of sight
    /// </summary>
    public bool CanSeeTarget()
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

        // Check if target is within vision angle
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

    /// <summary>
    /// Get current vision direction
    /// </summary>
    public Vector3 GetVisionDirection()
    {
        return currentVisionDirection;
    }

    /// <summary>
    /// Set vision direction (used by movement system)
    /// </summary>
    public void SetVisionDirection(Vector3 direction)
    {
        if (direction.magnitude > 0.01f)
        {
            currentVisionDirection = direction.normalized;
        }
    }

    /// <summary>
    /// Smoothly rotate vision towards target direction
    /// </summary>
    public void RotateVisionTowards(Vector3 targetDirection, float rotationSpeed)
    {
        if (targetDirection.magnitude > 0.01f)
        {
            currentVisionDirection = Vector3.Slerp(
                currentVisionDirection,
                targetDirection.normalized,
                rotationSpeed * Time.deltaTime / 90f
            );
            currentVisionDirection = currentVisionDirection.normalized;
        }
    }

    /// <summary>
    /// Find and set the player as target
    /// </summary>
    private void FindTarget()
    {
        if (target != null) return;

        // Try finding by tag first
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            return;
        }

        // Try finding by component
        PlayerControls playerScript = FindFirstObjectByType<PlayerControls>();
        if (playerScript != null)
        {
            target = playerScript.transform;
            return;
        }

        Debug.LogWarning($"{gameObject.name}: Player not found! Vision system needs a target.");
    }

    /// <summary>
    /// Set target manually
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Get current target
    /// </summary>
    public Transform GetTarget()
    {
        return target;
    }

    /// <summary>
    /// Get vision range
    /// </summary>
    public float GetVisionRange()
    {
        return visionRange;
    }

    /// <summary>
    /// Get vision angle
    /// </summary>
    public float GetVisionAngle()
    {
        return visionAngle;
    }

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (!drawVisionCone) return;

        // Use runtime values if available, otherwise use override or defaults
        float drawRange = visionRange > 0 ? visionRange : (visionRangeOverride > 0 ? visionRangeOverride : 8f);
        float drawAngle = visionAngle > 0 ? visionAngle : (visionAngleOverride > 0 ? visionAngleOverride : 60f);

        // Draw vision cone
        Gizmos.color = Color.yellow;

        // Use current vision direction if playing, otherwise use transform.up
        Vector3 forward = Application.isPlaying ? currentVisionDirection : transform.up;
        Vector3 leftBoundary = Quaternion.AngleAxis(-drawAngle / 2f, Vector3.forward) * forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(drawAngle / 2f, Vector3.forward) * forward;

        // Draw vision cone lines
        Gizmos.DrawRay(transform.position, leftBoundary * drawRange);
        Gizmos.DrawRay(transform.position, rightBoundary * drawRange);

        // Draw arc for vision range
        Vector3 previousPoint = transform.position + leftBoundary * drawRange;
        for (int i = 1; i <= 20; i++)
        {
            float angle = -drawAngle / 2f + (drawAngle / 20f) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * forward;
            Vector3 point = transform.position + direction * drawRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Draw line to target if visible
        if (Application.isPlaying && target != null && CanSeeTarget())
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }

    #endregion
}
