using UnityEngine;
using UnityEngine.AI; // For NavMeshAgent specific checks

public class ChaseState : AIStateBase
{
    private float lastUpdateTime;
    private const float UPDATE_RATE = 0.1f; // From EnemyAI
    
    // Attack vars
    private float lastAttackTime;
    private float attackRange;
    private float meleeRange;
    private float attackCooldown;
    private LayerMask obstacleMask;

    public ChaseState(EnemyAI context) : base(context) 
    {
        // Initialize settings from EnemyData
        if (enemyAI.enemyData != null)
        {
            attackRange = enemyAI.enemyData.weaponAttackRange;
            meleeRange = enemyAI.enemyData.meleeAttackRange;
            attackCooldown = enemyAI.enemyData.weaponAttackCooldown;
        }
        else
        {
            attackRange = 10f;
            meleeRange = 2f;
            attackCooldown = 1f;
        }
        
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

        GameObject currentWeapon = weaponController.GetCurrentWeapon();
        PickupableItem weaponItem = currentWeapon.GetComponent<PickupableItem>();

        // Check if depleted
        if (weaponItem != null && weaponItem.IsDepleted())
        {
            ThrowDepletedWeapon();
            enemyAI.TransitionToState(enemyAI.SeekItemStateInstance);
            return;
        }

        // Check cooldown
        if (Time.time - lastAttackTime < attackCooldown) return;

        float distanceToTarget = Vector3.Distance(enemyAI.transform.position, enemyAI.Target.position);
        bool canAttack = false;
        bool canSee = visionSystem.CanSeeTarget();
        bool hasLOS = HasLineOfSight(enemyAI.Target.position);

        if (weaponItem.holdType == ItemHoldType.Primary) // Ranged
        {
            if (distanceToTarget <= attackRange && canSee && hasLOS)
            {
                canAttack = true;
            }
        }
        else if (weaponItem.holdType == ItemHoldType.Secondary) // Melee
        {
            if (distanceToTarget <= meleeRange && hasLOS)
            {
                canAttack = true;
            }
        }

        if (canAttack)
        {
            // Trigger Animation
            // if (enemyAI.Animator != null)
            // {
            //     enemyAI.Animator.SetTrigger("Attack");
            //     // Pass weapon type to animator (true = Melee, false = Ranged)
            //     enemyAI.Animator.SetBool("IsMelee", weaponItem.holdType == ItemHoldType.Secondary);
            // }

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

        movementController.ChaseTarget(destinationPosition);
    }
}
