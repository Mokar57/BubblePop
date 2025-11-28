using UnityEngine;

public class WaitState : AIStateBase
{
    private float waitDuration;
    private float waitStartTime;
    private AIStateBase nextState;

    public WaitState(EnemyAI context) : base(context) { }

    public void SetDuration(float duration, AIStateBase nextState = null)
    {
        this.waitDuration = duration;
        this.nextState = nextState;
    }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Wait State");

        waitStartTime = Time.time;
        movementController.StopMovement();
    }

    public override void UpdateState()
    {
        if (Time.time >= waitStartTime + waitDuration)
        {
            if (nextState != null)
            {
                enemyAI.TransitionToState(nextState);
            }
            else
            {
                // Default back to Chase if target exists, else Patrol
                if (enemyAI.Target != null)
                {
                    enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
                }
                else
                {
                    enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
                }
            }
        }
    }

    public override void ExitState()
    {
        // Nothing to cleanup
    }
}
