using UnityEngine;

/// <summary>
/// Handles enemy sound detection (hearing system)
/// Subscribes to GameEvents.OnSoundEmitted and notifies AI when sounds are heard
/// </summary>
public class EnemySoundDetector : MonoBehaviour
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Sound Detection (Override)")]
    [Tooltip("Leave at 0 to use enemyData.soundDetectionRange")]
    public float detectionRangeOverride = 0f;

    [Header("Debug")]
    [Tooltip("Show debug logs when sounds are detected")]
    public bool debugMode = false;

    // Detection range
    private float detectionRange;

    // Event callback
    public System.Action<Vector2> OnSoundDetected;

    private void Start()
    {
        UpdateDetectionRange();
    }

    private void Update()
    {
        // Optional: Check for runtime changes in editor if needed
#if UNITY_EDITOR
        UpdateDetectionRange();
#endif
    }

    private void UpdateDetectionRange()
    {
        // Initialize detection range from ScriptableObject or override
        if (enemyData != null)
        {
            detectionRange = detectionRangeOverride > 0 ? detectionRangeOverride : enemyData.soundDetectionRange;
        }
        else
        {
            detectionRange = detectionRangeOverride > 0 ? detectionRangeOverride : 15f;
        }
    }

    private void OnEnable()
    {
        // Subscribe to sound events
        GameEvents.OnSoundEmitted += HandleSoundEmitted;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        GameEvents.OnSoundEmitted -= HandleSoundEmitted;
    }

    /// <summary>
    /// Handle sound emitted event
    /// </summary>
    private void HandleSoundEmitted(Vector2 soundPosition, float soundRadius)
    {
        // Check if sound is within detection range
        float distanceToSound = Vector2.Distance(transform.position, soundPosition);

        if (distanceToSound <= soundRadius && distanceToSound <= detectionRange)
        {
            if (debugMode)
                Debug.Log($"{gameObject.name} heard sound at {soundPosition} (distance: {distanceToSound:F2})");

            // Notify listeners (EnemySoundInvestigator)
            OnSoundDetected?.Invoke(soundPosition);
        }
    }

    /// <summary>
    /// Get current detection range
    /// </summary>
    public float GetDetectionRange()
    {
        return detectionRange;
    }

    /// <summary>
    /// Set detection range
    /// </summary>
    public void SetDetectionRange(float range)
    {
        detectionRange = Mathf.Max(0f, range);
    }

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        // Draw sound detection range
        float drawRange = detectionRange;

        // If not playing, calculate what it would be
        if (!Application.isPlaying)
        {
            drawRange = detectionRangeOverride > 0 ? detectionRangeOverride : (enemyData != null ? enemyData.soundDetectionRange : 15f);
        }

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan with transparency
        Gizmos.DrawWireSphere(transform.position, drawRange);
    }

    #endregion
}
