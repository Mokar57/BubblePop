using UnityEngine;

/// <summary>
/// Handles enemy stun triggers.
/// Logic is now driven by StunnedState.
/// </summary>
public class EnemyStunController : MonoBehaviour
{
    [Header("Stun Settings")]
    [Tooltip("Random rotation speed range during stun (min, max degrees/second)")]
    public Vector2 rotationSpeedRange = new Vector2(90f, 180f);

    [Tooltip("Distance threshold to consider reached stun center")]
    public float reachCenterDistance = 0.5f;

    // Stun parameters to be passed to state
    public Vector3 StunCenter { get; private set; }
    public float StunDuration { get; private set; }
    public bool IsStunTriggered { get; private set; }

    private EnemyAI enemyAI;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    /// <summary>
    /// Trigger stun effect
    /// </summary>
    public void EnterStun(Vector3 centerPosition, float duration)
    {
        StunCenter = centerPosition;
        StunDuration = duration;
        IsStunTriggered = true;
        
        // Force transition to StunnedState
        if (enemyAI != null)
        {
            enemyAI.TransitionToState(enemyAI.StunnedStateInstance);
        }
    }

    /// <summary>
    /// Called by StunnedState when stun is finished or interrupted
    /// </summary>
    public void ResetStunTrigger()
    {
        IsStunTriggered = false;
    }
    
    // Helper methods for StunnedState to access settings
    public float GetRandomRotationSpeed()
    {
        float speed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        return speed * (Random.value > 0.5f ? 1f : -1f);
    }

    // Compatibility methods for other scripts
    public bool IsStunned()
    {
        return enemyAI.CurrentState == enemyAI.StunnedStateInstance;
    }

    public bool HasReachedStunCenter()
    {
        return enemyAI.StunnedStateInstance != null && enemyAI.StunnedStateInstance.HasReachedCenter;
    }

    public void ExitStun()
    {
        if (IsStunned())
        {
            // Force transition to Patrol (or let AI decide based on context in next frame)
            // But since this is a forced exit, we should probably transition directly.
            enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
        }
        ResetStunTrigger();
    }
}
