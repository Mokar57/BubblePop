using UnityEngine;

public class StunnedState : AIStateBase
{
    public StunnedState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Stunned State");
        
        // This state assumes stunController.EnterStun() has already been called
        // by EnemyAI, so the stun is active and the timer is running.
    }

    public override void UpdateState()
    {
        // Check if player is visible while stunned (early exit if armed)
        if (visionSystem.CanSeeTarget() && itemHolder.HasWeapon())
        {
            stunController.ForceExitStun(); // Force exit stun in controller
            enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
            return;
        }

        // Check if stun duration ended (this check is within the stunController's Update)
        if (!stunController.IsStunned())
        {
            // After stun, check if player is visible
            if (visionSystem.CanSeeTarget())
            {
                if (itemHolder.HasWeapon())
                {
                    enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
                }
                else
                {
                    enemyAI.TransitionToState(enemyAI.SeekItemStateInstance);
                }
            }
            else
            {
                enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
            }
        }
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Stunned State");
        
        // StunController will have handled its own ExitStun() via its Update.
    }
}
