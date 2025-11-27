using UnityEngine;

public class SeekItemState : AIStateBase
{
    public SeekItemState(EnemyAI context) : base(context) { }

    public override void EnterState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Entering Seek Item State");
        
        itemSeeker.TryFindAndSeekItem();
    }

    public override void UpdateState()
    {
        // Check for transitions
        bool hasWeapon = itemHolder.HasWeapon();
        bool playerDetected = enemyAI.IsPlayerDetected();

        if (hasWeapon)
        {
            // Got weapon - return to patrol or chase if player detected
            if (playerDetected)
            {
                enemyAI.TransitionToState(enemyAI.ChaseStateInstance);
            }
            else
            {
                enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
            }
            return;
        }
        else if (!itemSeeker.IsSeekingItem)
        {
            // Not actively seeking - try to find item or patrol
            PickupableItem nearestItem = itemSeeker.FindNearestItem();
            if (nearestItem != null)
            {
                itemSeeker.TryFindAndSeekItem();
            }
            else
            {
                // No items nearby - patrol while unarmed
                enemyAI.TransitionToState(enemyAI.PatrolStateInstance);
            }
            return;
        }

        // If player is detected while still seeking an item, and we don't have a weapon, we remain in this state
        // because the core rule is "cannot chase without a weapon".
        // The item seeker will continue its work.
    }

    public override void ExitState()
    {
        if (enemyAI.DebugMode)
            Debug.Log("Exiting Seek Item State");
        
        // Item seeker manages its own cleanup
    }
}
