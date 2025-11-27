using UnityEngine;

/// <summary>
/// Enhanced Gizmo visualizer for enemy debugging
/// Shows all enemy settings from EnemyDataSO in real-time with color-coded layers
/// </summary>
public class EnemyDebugVisualizer : MonoBehaviour
{
    [Header("Visualization Toggles")]
    [Tooltip("Master toggle for all visualizations")]
    public bool showDebugVisuals = true;

    [Tooltip("Show vision cone")]
    public bool showVision = true;

    [Tooltip("Show proximity detection ring")]
    public bool showProximity = true;

    [Tooltip("Show sound detection radius")]
    public bool showSoundDetection = true;

    [Tooltip("Show item seeking ranges")]
    public bool showItemSeeking = true;

    [Tooltip("Show weapon attack ranges")]
    public bool showWeaponRanges = true;

    [Tooltip("Show chase distance limit")]
    public bool showChaseLimit = true;

    [Tooltip("Show patrol waypoint connections")]
    public bool showPatrolPath = true;

    [Tooltip("Show text labels with stats")]
    public bool showTextLabels = true;

    [Header("Color Settings")]
    [Tooltip("Color when patrolling")]
    public Color patrolColor = Color.green;

    [Tooltip("Color when chasing")]
    public Color chaseColor = Color.red;

    [Tooltip("Color when stunned")]
    public Color stunnedColor = Color.yellow;

    [Tooltip("Color when idle")]
    public Color idleColor = Color.gray;

    [Header("Label Settings")]
    [Tooltip("Offset for text label above enemy")]
    public Vector3 labelOffset = new Vector3(0, 2, 0);

    [Tooltip("Show health percentage")]
    public bool showHealth = true;

    [Tooltip("Show current state")]
    public bool showState = true;

    [Tooltip("Show velocity")]
    public bool showVelocity = true;

    // Component references
    private EnemyAI enemyAI;
    private EnemyHealth health;
    private EnemyVisionSystem visionSystem;
    private EnemyMovementController movementController;
    private EnemySoundDetector soundDetector;
    private EnemyStunController stunController;
    private EnemyItemIntegration itemIntegration;
    private EnemyItemSeeker itemSeeker;
    private EnemyDataSO enemyData;

    private void Awake()
    {
        // Cache all component references
        enemyAI = GetComponent<EnemyAI>();
        health = GetComponent<EnemyHealth>();
        visionSystem = GetComponent<EnemyVisionSystem>();
        movementController = GetComponent<EnemyMovementController>();
        soundDetector = GetComponent<EnemySoundDetector>();
        stunController = GetComponent<EnemyStunController>();
        itemIntegration = GetComponent<EnemyItemIntegration>();
        itemSeeker = GetComponent<EnemyItemSeeker>();

        // Get EnemyDataSO from any component
        if (enemyAI != null && enemyAI.enemyData != null)
            enemyData = enemyAI.enemyData;
        else if (health != null && health.enemyData != null)
            enemyData = health.enemyData;
        else if (visionSystem != null && visionSystem.enemyData != null)
            enemyData = visionSystem.enemyData;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals) return;

        // Update component references if needed (for edit mode)
        if (enemyAI == null) Awake();

        Vector3 pos = transform.position;

        // Get current state and set color
        Color currentColor = GetStateColor();

        // Draw proximity detection ring
        if (showProximity && enemyData != null && enemyData.proximityRadius > 0)
        {
            Gizmos.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
            Gizmos.DrawWireSphere(pos, enemyData.proximityRadius);
        }

        // Draw chase distance limit
        if (showChaseLimit && movementController != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // Orange
            Gizmos.DrawWireSphere(pos, movementController.maxChaseDistance);
        }

        // Draw sound detection
        if (showSoundDetection && soundDetector != null)
        {
            float soundRange = soundDetector.GetDetectionRange();
            Gizmos.color = new Color(0f, 1f, 1f, 1f); // Cyan
            Gizmos.DrawWireSphere(pos, soundRange);
        }

        // Draw item seeking ranges
        if (showItemSeeking && itemSeeker != null && enemyData != null)
        {
            // Seek radius
            Gizmos.color = new Color(0f, 1f, 1f, 1f);
            Gizmos.DrawWireSphere(pos, enemyData.itemSeekRadius);

            // Pickup radius
            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            Gizmos.DrawWireSphere(pos, enemyData.itemPickupRadius);
        }

        // Draw weapon ranges
        if (showWeaponRanges && itemIntegration != null && enemyData != null)
        {
            // Ranged attack range
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            Gizmos.DrawWireSphere(pos, enemyData.weaponAttackRange);

            // Melee attack range
            Gizmos.color = new Color(1f, 1f, 0f, 1f);
            Gizmos.DrawWireSphere(pos, enemyData.meleeAttackRange);
        }

        // Draw patrol path
        if (showPatrolPath && movementController != null && movementController.patrolWaypoints != null)
        {
            DrawPatrolPath();
        }

        // Draw vision cone
        if (showVision && visionSystem != null)
        {
            DrawVisionCone();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugVisuals) return;

        // Draw text labels
        if (showTextLabels)
        {
            DrawDebugLabels();
        }
    }

    /// <summary>
    /// Get color based on current AI state
    /// </summary>
    private Color GetStateColor()
    {
        if (!Application.isPlaying || enemyAI == null)
            return idleColor;

        if (stunController != null && stunController.IsStunned())
            return stunnedColor;

        AIStateBase state = enemyAI.CurrentState;
        if (state == enemyAI.ChaseStateInstance)
        {
            return chaseColor;
        }
        else if (state == enemyAI.SeekItemStateInstance)
        {
            return Color.magenta; // Magenta for seeking items
        }
        else if (state == enemyAI.InvestigateStateInstance)
        {
            return Color.cyan; // Cyan for investigating
        }
        else if (state == enemyAI.PatrolStateInstance)
        {
            return patrolColor;
        }
        else if (state == enemyAI.StunnedStateInstance)
        {
            return stunnedColor;
        }
        else if (state == enemyAI.SearchStateInstance)
        {
            return new Color(1f, 0.5f, 0f); // Orange for searching
        }
        else
        {
            return idleColor;
        }
    }

    /// <summary>
    /// Draw enhanced vision cone with direction
    /// </summary>
    private void DrawVisionCone()
    {
        // Refresh vision system reference if needed
        if (visionSystem == null)
            visionSystem = GetComponent<EnemyVisionSystem>();
        
        if (visionSystem == null) return;

        // Get vision parameters (works in edit mode and runtime)
        float visionRange = visionSystem.GetVisionRange();
        float visionAngle = visionSystem.GetVisionAngle();
        
        // Use default values if not initialized yet (edit mode)
        if (visionRange <= 0)
        {
            visionRange = visionSystem.visionRangeOverride > 0 ? visionSystem.visionRangeOverride : 
                         (enemyData != null ? enemyData.visionRange : 8f);
        }
        if (visionAngle <= 0)
        {
            visionAngle = visionSystem.visionAngleOverride > 0 ? visionSystem.visionAngleOverride : 
                         (enemyData != null ? enemyData.visionAngle : 60f);
        }

        Vector3 visionDir = Application.isPlaying ? visionSystem.GetVisionDirection() : transform.up;

        Color stateColor = GetStateColor();
        Gizmos.color = new Color(stateColor.r, stateColor.g, stateColor.b, 1f);

        // Draw vision cone
        Vector3 leftBoundary = Quaternion.AngleAxis(-visionAngle / 2f, Vector3.forward) * visionDir;
        Vector3 rightBoundary = Quaternion.AngleAxis(visionAngle / 2f, Vector3.forward) * visionDir;

        Gizmos.DrawRay(transform.position, leftBoundary * visionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * visionRange);
        Gizmos.DrawRay(transform.position, visionDir * visionRange);

        // Draw arc
        Vector3 previousPoint = transform.position + leftBoundary * visionRange;
        for (int i = 1; i <= 20; i++)
        {
            float angle = -visionAngle / 2f + (visionAngle / 20f) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * visionDir;
            Vector3 point = transform.position + direction * visionRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Draw line to target if visible
        if (Application.isPlaying && visionSystem.CanSeeTarget() && visionSystem.GetTarget() != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, visionSystem.GetTarget().position);
        }
    }

    /// <summary>
    /// Draw patrol path connections
    /// </summary>
    private void DrawPatrolPath()
    {
        var waypoints = movementController.patrolWaypoints;
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = new Color(0.5f, 0.5f, 1f, 1f); // Light blue

        // Draw lines between waypoints
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;

            Vector3 currentPos = waypoints[i].Position;

            // Draw line to this enemy
            Gizmos.DrawLine(transform.position, currentPos);

            // Draw line to next waypoint
            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
            {
                Vector3 nextPos = waypoints[i + 1].Position;
                Gizmos.DrawLine(currentPos, nextPos);
            }
            else if (movementController.loopPatrol && waypoints[0] != null)
            {
                // Draw line back to first waypoint if looping
                Vector3 firstPos = waypoints[0].Position;
                Gizmos.DrawLine(currentPos, firstPos);
            }
        }
    }

    /// <summary>
    /// Draw text labels with enemy stats (only works in Scene view)
    /// </summary>
    private void DrawDebugLabels()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;

        string label = "";

        // Show state
        if (showState && enemyAI != null)
        {
            AIStateBase state = enemyAI.CurrentState;
            label += $"State: {state.GetType().Name}\n";
        }

        // Show health
        if (showHealth && health != null)
        {
            float healthPercent = (health.CurrentHealth / health.MaxHealth) * 100f;
            label += $"Health: {health.CurrentHealth:F0}/{health.MaxHealth:F0} ({healthPercent:F0}%)\n";
        }

        // Show velocity
        if (showVelocity && movementController != null)
        {
            float velocity = movementController.GetVelocity();
            label += $"Speed: {velocity:F1} m/s\n";
        }

        // Show item info
        if (itemSeeker != null && itemSeeker.IsSeekingItem)
        {
            label += $"Seeking: {itemSeeker.TargetItem?.itemName ?? "Unknown"}\n";
        }

        if (!string.IsNullOrEmpty(label))
        {
            Vector3 labelPos = transform.position + labelOffset;
            UnityEditor.Handles.Label(labelPos, label, GetLabelStyle());
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Get GUIStyle for labels
    /// </summary>
    private GUIStyle GetLabelStyle()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = GetStateColor();
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 11;
        style.alignment = TextAnchor.MiddleCenter;

        // Add background for readability
        Texture2D backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
        backgroundTexture.Apply();
        style.normal.background = backgroundTexture;
        style.padding = new RectOffset(5, 5, 5, 5);

        return style;
    }
#endif

    /// <summary>
    /// Draw a circle wireframe (helper method)
    /// </summary>
    private void DrawWireCircle(Vector3 center, float radius, Color color, int segments = 32)
    {
        Gizmos.color = color;

        Vector3 previousPoint = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (360f / segments) * i;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            Vector3 point = center + new Vector3(x, y, 0);
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }
}
