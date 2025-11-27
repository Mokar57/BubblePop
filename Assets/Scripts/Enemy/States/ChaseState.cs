using UnityEngine;
using UnityEngine.AI; // For NavMeshAgent specific checks

public class ChaseState : AIStateBase
{
    private float lastUpdateTime;
    private const float UPDATE_RATE = 0.1f; // From EnemyAI

    public ChaseState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Chase State");
        
        // Ensure the movement controller is active for chasing
        movementController.SetEnabled(true);
        // Ensure EnemyAI knows we are chasing (this flag is used by transitions)
        enemyAI.SetHasDetectedTarget(true); 
    }

    public override void UpdateState()
    {
        // Update last known position if we can see target
        if (enemyAI.Target != null && visionSystem.CanSeeTarget())
        {
            enemyAI.LastKnownTargetPosition = enemyAI.Target.position;
            enemyAI.LastSeenTime = Time.time;
            enemyAI.SetHasDetectedTarget(true);
        }

        if (Time.time - lastUpdateTime >= UPDATE_RATE)
        {
            UpdateChaseDestination();
            lastUpdateTime = Time.time;
        }

        // Transition logic from EnemyAI's EvaluateStateTransitions
        if (enemyAI.HasDetectedTarget) // Means we were chasing or saw target recently
        {
            if (enemyAI.PersistentChase && enemyAI.Target != null)
            {
                float distanceToTarget = Vector3.Distance(enemyAI.transform.position, enemyAI.Target.position);
                if (distanceToTarget > movementController.MaxChaseDistance) 
                {
                    // Too far - give up chase
                    enemyAI.SetHasDetectedTarget(false);
                    enemyAI.TransitionToState(enemyAI.PatrolStateInstance); // Use instance
                }
                // else continue chasing
            }
            else if (!visionSystem.CanSeeTarget()) // Lost sight, not persistent chase or target is null
            {
                // Go to last known position (SearchState)
                enemyAI.TransitionToState(enemyAI.SearchStateInstance); // Use instance
            }
            // If target has weapon and is seen, stay in chase state
            else if (itemHolder.HasWeapon() && visionSystem.CanSeeTarget())
            {
                // Stay in chase
            }
        }
        else // No longer detected target (either gave up chase or never detected)
        {
            // Transition to patrol if not chasing persistently or target is really gone
            enemyAI.TransitionToState(enemyAI.PatrolStateInstance); // Use instance
        }
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Chase State");
        
        // No explicit stop movement needed here, the next state will set its own movement.
        // Also, it's possible we transition to SearchState which continues movement.
    }

    private void UpdateChaseDestination()
    {
        // Check if agent is valid and active
        if (enemyAI.Target == null || movementController.Agent == null || !movementController.Agent.enabled || !movementController.Agent.isOnNavMesh) return;

        Vector3 destinationPosition = enemyAI.Target.position;

        // If persistent chase or still seeing target, update last known position
        if (enemyAI.PersistentChase || visionSystem.CanSeeTarget())
        {
            enemyAI.LastKnownTargetPosition = enemyAI.Target.position;
            enemyAI.LastSeenTime = Time.time;
        }

        movementController.ChaseTarget(destinationPosition);
    }
}
