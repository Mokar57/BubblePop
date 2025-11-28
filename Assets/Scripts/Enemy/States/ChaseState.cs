using UnityEngine;
using UnityEngine.AI; // For NavMeshAgent specific checks

public class ChaseState : AIStateBase
{
    private float lastUpdateTime;
    private const float UPDATE_RATE = 0.1f; // From EnemyAI

    // Attack vars
    private float lastAttackTime;
    private LayerMask obstacleMask;

    public ChaseState(EnemyAI context) : base(context)
    {
        // Default obstacle mask (everything except Ignore Raycast, Player, Enemy, etc if needed)
        // Ideally this should come from config, but for now we use a sensible default
        obstacleMask = LayerMask.GetMask("Default", "Obstacle", "Wall");
    }

    public override void EnterState()
    {
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

        // Movement Logic
        if (Time.time - lastUpdateTime >= UPDATE_RATE)
        {
            UpdateChaseDestination();
            lastUpdateTime = Time.time;
        }

        // Attack Logic
        HandleAttack();

        // Transition Logic
        CheckTransitions();

        // Vision Logic
        UpdateChaseVision();
    }

    private void UpdateChaseVision()
    {
        // Hotline Miami style: Very fast/snappy rotation
        float snappyRotationSpeed = enemyAI.enemyData.chaseRotationSpeed;

        // If we have a target and can see it, look at it
        if (enemyAI.Target != null && visionSystem.CanSeeTarget())
        {
            Vector3 dirToTarget = (enemyAI.Target.position - enemyAI.transform.position).normalized;
            visionSystem.RotateVisionTowards(dirToTarget, snappyRotationSpeed);
        }
        else
        {
            // Otherwise look where we are going, but faster than normal
            if (movementController.IsMoving())
            {
                Vector3 moveDirection = movementController.GetMovementDirection();
                if (moveDirection.magnitude > 0.1f)
                {
                    visionSystem.RotateVisionTowards(moveDirection, snappyRotationSpeed);
                }
            }
        }
    }

    private void HandleAttack()
    {
        if (enemyAI.Target == null) return;

        // Check weapon status
        if (!weaponController.HasWeapon())
        {
            // No weapon - should probably seek one
            enemyAI.TransitionToState(enemyAI.SeekItemStateInstance);
            return;
        }

        GameObject currentWeaponObj = weaponController.GetCurrentWeapon();
        Weapon currentWeapon = currentWeaponObj.GetComponent<Weapon>();

        // Check if depleted
        if (currentWeapon != null && currentWeapon.IsBroken)
        {
            ThrowDepletedWeapon();
            enemyAI.TransitionToState(enemyAI.SeekItemStateInstance);
            return;
        }

        // --- Determine Attack Stats ---
        float currentAttackCooldown = 1f;
        float currentAttackRange = 1.5f;

        if (currentWeapon != null && currentWeapon.Data != null)
        {
            currentAttackCooldown = currentWeapon.Data.enemyAttackCooldown;
            currentAttackRange = currentWeapon.Data.enemyAttackRange; // Use explicit enemy attack range
        }

        // Check cooldown
        if (Time.time - lastAttackTime < currentAttackCooldown) return;

        float distanceToTarget = Vector3.Distance(enemyAI.transform.position, enemyAI.Target.position);
        bool canAttack = false;
        bool hasLOS = HasLineOfSight(enemyAI.Target.position);

        // Simple check: Are we close enough and have LOS?
        // We add a small buffer (e.g. 10%) to the range so we don't jitter at the edge
        if (distanceToTarget <= currentAttackRange * 1.1f && hasLOS)
        {
            canAttack = true;
        }

        if (canAttack)
        {
            weaponController.PerformAttack(enemyAI.Target.position);
            lastAttackTime = Time.time;
        }
    }

    private void CheckTransitions()
    {
        if (enemyAI.HasDetectedTarget) // Means we were chasing or saw target recently
        {
            if (enemyAI.PersistentChase && enemyAI.Target != null)
            {
                float distanceToTarget = Vector3.Distance(enemyAI.transform.position, enemyAI.Target.position);
                if (distanceToTarget > movementController.MaxChaseDistance)
                {
                    // Too far - give up chase
                    enemyAI.SetHasDetectedTarget(false);
                    enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
                }
            }
            else if (!visionSystem.CanSeeTarget()) // Lost sight, not persistent chase or target is null
            {
                // Go to last known position (SearchState)
                enemyAI.TransitionToState(enemyAI.SearchStateInstance);
            }
        }
        else // No longer detected target
        {
            enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
        }
    }

    private void ThrowDepletedWeapon()
    {
        if (enemyAI.Target != null)
        {
            Vector2 throwDirection = (enemyAI.Target.position - enemyAI.transform.position).normalized;
            weaponController.ThrowCurrentItem(throwDirection);
        }
        else
        {
            weaponController.ThrowCurrentItem(enemyAI.GetVisionDirection());
        }
    }

    private bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector2 startPos = enemyAI.transform.position;
        Vector2 directionToTarget = (targetPosition - (Vector3)startPos).normalized;
        float distanceToTarget = Vector2.Distance(startPos, targetPosition);

        // Use RaycastAll to handle self-hits and target-hits correctly
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, directionToTarget, distanceToTarget, obstacleMask);

        // Sort by distance to process nearest hits first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            // Ignore self and children
            if (hit.collider.gameObject == enemyAI.gameObject || hit.collider.transform.IsChildOf(enemyAI.transform))
                continue;

            // If we hit the target (Player), line of sight is clear!
            if (hit.collider.transform == enemyAI.Target || hit.collider.CompareTag("Player"))
            {
                return true;
            }

            // If we hit an obstacle (that isn't the target), line of sight is blocked
            return false;
        }

        // If we hit nothing in the mask, line of sight is clear
        return true;
    }

    public override void ExitState()
    {
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

        // --- NEW LOGIC: Movement based on WEAPON, not Enemy Type ---

        float desiredDistance = 1.5f; // Default melee range

        if (enemyAI.WeaponController.IsHoldingWeapon)
        {
            var weapon = enemyAI.WeaponController.GetCurrentWeapon().GetComponent<Weapon>();
            if (weapon != null && weapon.Data != null)
            {
                // Stand slightly inside the max attack range (e.g. 90%) to ensure we can hit
                desiredDistance = weapon.Data.enemyAttackRange * 0.9f;
            }
        }

        // We consider it "ranged behavior" if the ideal range is significant (e.g. > 3m)
        // or strictly based on hold type if you prefer.
        bool isRangedBehavior = desiredDistance > 3.0f;

        float distanceToTarget = Vector3.Distance(enemyAI.transform.position, destinationPosition);

        if (isRangedBehavior)
        {
            // Ranged / Kiting Behavior
            float stopRange = desiredDistance;
            float retreatRange = desiredDistance * 0.5f; // Too close!

            if (distanceToTarget > stopRange)
            {
                // Move closer
                movementController.ChaseTarget(destinationPosition);
            }
            else if (distanceToTarget < retreatRange)
            {
                // Too close! Back away slightly
                // Simple implementation: Stop moving if in good range
                movementController.StopMovement();

                // Optional: Add code here to actually move the NavMeshAgent away from the player
            }
            else
            {
                // In sweet spot
                movementController.StopMovement();
            }
        }
        else
        {
            // Melee / Aggressive Behavior
            // Move until we are within the ideal range (which is usually close for melee)

            if (distanceToTarget <= desiredDistance)
            {
                movementController.StopMovement();
            }
            else
            {
                // Check for direct line of sight for aggressive "Hotline Miami" chase
                if (visionSystem.CanSeeTarget())
                {
                    movementController.MoveDirectly(destinationPosition);
                }
                else
                {
                    movementController.ChaseTarget(destinationPosition);
                }
            }
        }
    }
}
