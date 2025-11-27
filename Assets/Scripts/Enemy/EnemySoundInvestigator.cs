using UnityEngine;

/// <summary>
/// Handles sound detection events and holds configuration for investigation.
/// Behavior logic is now in InvestigateState.
/// </summary>
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
    private EnemySoundDetector soundDetector;

    // Event
    public event System.Action<Vector2> OnSoundDetected;

    // State
    private bool _hasDetectedSound = false;

    private void Awake()
    {
        soundDetector = GetComponent<EnemySoundDetector>();
    }

    private void Start()
    {
        // Subscribe to sound detection
        if (soundDetector != null)
        {
            soundDetector.OnSoundDetected += HandleSoundDetected;
        }
    }

    private void OnDestroy()
    {
        if (soundDetector != null)
        {
            soundDetector.OnSoundDetected -= HandleSoundDetected;
        }
    }

    /// <summary>
    /// Handle sound detected event - invokes OnSoundDetected event
    /// </summary>
    private void HandleSoundDetected(Vector2 soundPosition)
    {
        _hasDetectedSound = true;
        OnSoundDetected?.Invoke(soundPosition);
    }

    public void ResetSoundDetection()
    {
        _hasDetectedSound = false;
    }
    
    // Public getter for sound detected status
    public bool HasDetectedSoundStatus => _hasDetectedSound;

    // Helper methods for State to get config
    public float GetInvestigationDuration()
    {
        if (investigationDurationOverride > 0) return investigationDurationOverride;
        return enemyData != null ? enemyData.soundInvestigationDuration : 3f;
    }

    public float GetRotationInterval()
    {
        if (rotationIntervalOverride > 0) return rotationIntervalOverride;
        return enemyData != null ? enemyData.soundSearchRotationInterval : 1f;
    }
    
    public float GetRotationSpeed()
    {
        return enemyData != null ? enemyData.rotationSpeed : 90f;
    }
}
