using UnityEngine;

public class SeekItemState : AIStateBase
{
    private PickupableItem targetItem;
    private float lastSearchTime;
    private const float SEARCH_INTERVAL = 1f;

    public SeekItemState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Seek Item State");
        
        movementController.SetEnabled(true);
        FindAndSetTargetItem();
    }

    public override void UpdateState()
    {
        // 1. Check if we already have a weapon (maybe picked up by collision or other means)
        if (weaponController.HasWeapon())
        {
            TransitionAfterPickup();
            return;
        }

        // 2. Validate target item
        if (targetItem == null || targetItem.IsDepleted() || targetItem.transform.parent != null)
        {
            // Try to find a new one periodically
            if (Time.time - lastSearchTime > SEARCH_INTERVAL)
            {
                FindAndSetTargetItem();
                lastSearchTime = Time.time;
            }

            // If still no item, maybe patrol?
            if (targetItem == null)
            {
                // For now, just wait or patrol. 
                // If we want to force finding a weapon, we might just wander.
                // Let's transition to Patrol if we can't find anything, 
                // but Patrol might transition back to SeekItem if unarmed.
                // To avoid infinite loop, we could wander here.
                // But for simplicity, let's transition to Patrol.
                enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
            }
            return;
        }

        // 3. Move towards item
        movementController.MoveTo(targetItem.transform.position);

        // 4. Check distance and pickup
        float distanceToItem = Vector2.Distance(enemyAI.transform.position, targetItem.transform.position);
        if (distanceToItem <= itemSeeker.GetPickupRadius())
        {
            bool success = targetItem.TryPickupByEnemy(weaponController);
            if (success)
            {
                TransitionAfterPickup();
            }
            else
            {
                // Failed to pickup (maybe someone else got it?), clear target
                targetItem = null;
            }
        }
    }

    private void FindAndSetTargetItem()
    {
        targetItem = itemSeeker.FindBestItem();
    }

    private void TransitionAfterPickup()
    {
        enemyAI.OnWeaponAcquired();
        
        if (enemyAI.IsPlayerDetected())
        {
            enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
        }
        else
        {
            enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
        }
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Seek Item State");
        
        targetItem = null;
    }
}
