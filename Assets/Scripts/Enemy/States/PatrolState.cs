using UnityEngine;
using System.Collections.Generic;

public class PatrolState : AIStateBase
{
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    public PatrolState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Patrol State");
        
        if (movementController.enablePatrol && movementController.patrolWaypoints.Count > 0)
        {
            currentWaypointIndex = 0;
            isWaiting = false;
            MoveToCurrentWaypoint();
        }
        else
        {
            // Idle if no patrol
            movementController.StopMovement();
        }
    }

    public override void UpdateState()
    {
        // Check for transitions
        if (CheckTransitions()) return;

        if (!movementController.enablePatrol || movementController.patrolWaypoints.Count == 0)
            return;

        if (isWaiting)
        {
            HandleWaiting();
        }
        else
        {
            HandleMoving();
        }
        
        // Standard vision behavior
        LookWhereMoving();
    }

    private void HandleMoving()
    {
        // Check if stuck
        if (movementController.IsStuck)
        {
            // If stuck, skip to next waypoint immediately
            NextWaypoint();
            return;
        }

        // Check if reached destination
        if (movementController.HasReachedDestination(movementController.waypointReachDistance))
        {
            StartWaiting();
        }
    }

    private void HandleWaiting()
    {
        waitTimer -= Time.deltaTime;
        
        // Rotate towards waypoint direction while waiting
        Waypoint current = GetCurrentWaypoint();
        if (current != null)
        {
            enemyAI.SetVisionDirection(current.WaitDirection);
        }

        if (waitTimer <= 0f)
        {
            NextWaypoint();
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;
        movementController.StopMovement();
        
        Waypoint current = GetCurrentWaypoint();
        if (current != null)
        {
            waitTimer = current.WaitTime;
        }
        else
        {
            waitTimer = 1f; // Default wait
        }
    }

    private void MoveToCurrentWaypoint()
    {
        Waypoint target = GetCurrentWaypoint();
        if (target != null)
        {
            movementController.MoveTo(target.Position);
        }
    }

    private void NextWaypoint()
    {
        isWaiting = false;
        List<Waypoint> waypoints = movementController.patrolWaypoints;
        
        if (movementController.randomPatrolOrder)
        {
            int newIndex;
            do
            {
                newIndex = Random.Range(0, waypoints.Count);
            } while (newIndex == currentWaypointIndex && waypoints.Count > 1);
            currentWaypointIndex = newIndex;
        }
        else
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                if (movementController.loopPatrol)
                {
                    currentWaypointIndex = 0;
                }
                else
                {
                    // End of patrol
                    movementController.StopMovement();
                    return;
                }
            }
        }
        
        MoveToCurrentWaypoint();
    }

    private Waypoint GetCurrentWaypoint()
    {
        var waypoints = movementController.patrolWaypoints;
        if (waypoints != null && currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count)
        {
            return waypoints[currentWaypointIndex];
        }
        return null;
    }

    private bool CheckTransitions()
    {
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
            return true;
        }
        return false;
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Patrol State");
    }
}
