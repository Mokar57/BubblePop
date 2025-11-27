using UnityEngine;

public class StunnedState : AIStateBase
{
    private float stunEndTime;
    private bool hasReachedStunCenter;
    private float currentRotationSpeed;
    private Vector3 stunCenter;

    public bool HasReachedCenter => hasReachedStunCenter;

    public StunnedState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Stunned State");
        
        // Initialize stun parameters from controller
        stunCenter = stunController.StunCenter;
        stunEndTime = Time.time + stunController.StunDuration;
        hasReachedStunCenter = false;
        currentRotationSpeed = stunController.GetRandomRotationSpeed();

        // Start moving to center
        movementController.SetEnabled(true);
        movementController.ChaseTarget(stunCenter);
    }

    public override void UpdateState()
    {
        // 1. Check for early exit (Player visible + Armed)
        if (visionSystem.CanSeeTarget() && weaponController.HasWeapon())
        {
            enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
            return;
        }

        // 2. Handle Movement / Rotation
        if (!hasReachedStunCenter)
        {
            float distanceToCenter = Vector3.Distance(enemyAI.transform.position, stunCenter);
            if (distanceToCenter <= stunController.reachCenterDistance)
            {
                hasReachedStunCenter = true;
                movementController.StopMovement();
            }
            else
            {
                // Look where we are going
                Vector3 moveDir = movementController.GetMovementDirection();
                if (moveDir.magnitude > 0.1f)
                {
                    visionSystem.SetVisionDirection(moveDir);
                }
            }
        }
        else
        {
            // Spin around confusedly
            Vector3 currentVision = visionSystem.GetVisionDirection();
            Quaternion rotation = Quaternion.Euler(0, 0, currentRotationSpeed * Time.deltaTime);
            Vector3 newVision = rotation * currentVision;
            visionSystem.SetVisionDirection(newVision);
        }

        // 3. Check Timer
        if (Time.time >= stunEndTime)
        {
            TransitionAfterStun();
        }
    }

    private void TransitionAfterStun()
    {
        if (visionSystem.CanSeeTarget())
        {
            if (weaponController.HasWeapon())
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

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Stunned State");
        
        stunController.ResetStunTrigger();
        movementController.StopMovement();
    }
}
