using UnityEngine;

public class PatrolState : AIStateBase
{
    public PatrolState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Patrol State");
        
        if (movementController.enablePatrol)
        {
            movementController.StartPatrol();
        }
        else
        {
            movementController.StopMovement();
        }
    }

    public override void UpdateState()
    {
        // The movement controller is now self-managing the patrol logic (moving between waypoints, waiting, etc.)
        // This state's primary job is to check for transitions to other states.

        // Check for transitions
        if (enemyAI.IsPlayerDetected())
        {
            if (itemHolder.HasWeapon())
            {
                enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
            }
            else
            {
                enemyAI.TransitionToState(enemyAI.SeekItemStateInstance);
            }
            return;
        }

        // Note: Sound detection will be handled by an event that forces a transition,
        // so we don't need to check it every frame here. This will be implemented
        // when refactoring the EnemyAI context.
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Patrol State");

        // The next state will be responsible for starting its own movement logic,
        // so we don't necessarily need to stop the NavMeshAgent here, but it can be good practice.
        // movementController.StopMovement(); 
    }
}
