using UnityEngine;

public class InvestigateState : AIStateBase
{
    private Vector2 investigationPosition;
    
    // State variables
    private bool hasReachedInitialPoint;
    private float investigationEndTime;
    private float lastRotationTime;
    private float lastSearchMoveTime;
    private Vector3 currentSearchTarget;
    private Vector3 targetLookDirection;

    public InvestigateState(EnemyAI context) : base(context) { }

    public void SetInvestigationPosition(Vector2 position)
    {
        this.investigationPosition = position;
    }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Investigate State");
        
        hasReachedInitialPoint = false;
        
        // Start moving to the sound source
        movementController.SetEnabled(true);
        movementController.ChaseTarget(investigationPosition);
    }

    public override void UpdateState()
    {
        // 1. Check for player detection (Interrupt)
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

        // 2. Logic
        if (!hasReachedInitialPoint)
        {
            // Phase 1: Move to sound source
            if (movementController.HasReachedDestination(movementController.StopDistance))
            {
                StartSearchingBehavior();
            }
            
            // Look where we are going while approaching the sound
            LookWhereMoving();
        }
        else
        {
            // Phase 2: Search around
            UpdateSearchBehavior();
        }
    }

    private void StartSearchingBehavior()
    {
        hasReachedInitialPoint = true;
        
        float duration = soundInvestigator.GetInvestigationDuration();
        investigationEndTime = Time.time + duration;
        
        lastRotationTime = Time.time;
        lastSearchMoveTime = Time.time;
        currentSearchTarget = investigationPosition;
        targetLookDirection = visionSystem.GetVisionDirection();
        
        PickNewSearchPoint();
    }

    private void UpdateSearchBehavior()
    {
        // Check timeout
        if (Time.time >= investigationEndTime)
        {
            enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
            return;
        }

        // Move to new search positions periodically
        if (Time.time >= lastSearchMoveTime + soundInvestigator.searchMoveInterval)
        {
            // Check if reached current search target or time to move
            float distToTarget = Vector3.Distance(enemyAI.transform.position, currentSearchTarget);
            if (distToTarget <= movementController.StopDistance + 0.5f)
            {
                PickNewSearchPoint();
            }
        }

        // Vision Logic
        if (movementController.IsMoving())
        {
            // Look where moving (using investigator rotation speed)
            Vector3 moveDir = movementController.GetMovementDirection();
            float rotSpeed = soundInvestigator.GetRotationSpeed();
            visionSystem.RotateVisionTowards(moveDir, rotSpeed);
            
            // Keep target look direction synced so we don't snap when stopping
            targetLookDirection = visionSystem.GetVisionDirection();
            lastRotationTime = Time.time;
        }
        else
        {
            // Pick new look direction periodically when stationary
            float rotationInterval = soundInvestigator.GetRotationInterval();
            if (Time.time >= lastRotationTime + rotationInterval)
            {
                lastRotationTime = Time.time;
                PickNewLookDirection();
            }
            
            // Smoothly rotate vision towards target direction
            float rotSpeed = soundInvestigator.GetRotationSpeed();
            visionSystem.RotateVisionTowards(targetLookDirection, rotSpeed);
        }
    }

    private void PickNewSearchPoint()
    {
        lastSearchMoveTime = Time.time;
        
        // Generate random point within search radius around original investigation point
        float randomAngle = Random.Range(0f, 360f);
        float randomDist = Random.Range(0f, soundInvestigator.searchRadius);
        
        Vector3 offset = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDist,
            Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDist,
            0f
        );
        
        currentSearchTarget = (Vector3)investigationPosition + offset;
        movementController.ChaseTarget(currentSearchTarget);
    }

    private void PickNewLookDirection()
    {
        // Standing still, pick random direction to look
        float randomAngle = Random.Range(0f, 360f);
        targetLookDirection = new Vector3(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad),
            0f
        );
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Investigate State");
        
        soundInvestigator.ResetSoundDetection();
        movementController.StopMovement();
    }
}
