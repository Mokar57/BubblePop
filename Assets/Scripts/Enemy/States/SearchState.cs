using UnityEngine;

public class SearchState : AIStateBase
{
    public SearchState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Search State");

        // Set destination to last known target position
        movementController.SetEnabled(true);
        movementController.ChaseTarget(enemyAI.LastKnownTargetPosition);
    }

    public override void UpdateState()
    {
        // Check for player seen while searching
        if (enemyAI.IsPlayerDetected())
        {
            if (weaponController.HasWeapon())
            {
                enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
            }
            else
            {
                enemyAI.TransitionToState(enemyAI.SeekItemStateInstance);
            }
            return;
        }

        // Check if reached last known position
        // Using movementController.Agent.remainingDistance for more accurate NavMesh completion check
        if (movementController.Agent != null && movementController.Agent.enabled && movementController.Agent.isOnNavMesh &&
            !movementController.Agent.pathPending && movementController.Agent.remainingDistance <= movementController.Agent.stoppingDistance + 0.1f) // Adding a small tolerance
        {
            enemyAI.SetHasDetectedTarget(false); // No longer detected, so clear the flag
            enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
            return;
        }
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Search State");
        
        // Next state will take over movement
    }
}
