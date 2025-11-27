using UnityEngine;

public class InvestigateState : AIStateBase
{
    private Vector2 investigationPosition; // This will be set by EnemyAI when transitioning

    public InvestigateState(EnemyAI context) : base(context) { }

    public void SetInvestigationPosition(Vector2 position)
    {
        this.investigationPosition = position;
    }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Investigate State");
        
        // Don't interrupt chase (this check will be handled in EnemyAI's transition logic)
        // Don't investigate if no weapon and player was detected (also handled in EnemyAI's transition logic)
        soundInvestigator.StartInvestigation(investigationPosition);
    }

    public override void UpdateState()
    {
        // Check for player seen while investigating
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

        // Check if investigation is complete
        if (!soundInvestigator.IsInvestigating() && !soundInvestigator.IsMovingToInvestigate())
        {
            enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
            return;
        }
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Investigate State");
        
        soundInvestigator.StopInvestigation();
    }
}
