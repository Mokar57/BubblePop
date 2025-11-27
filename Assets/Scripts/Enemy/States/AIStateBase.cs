using UnityEngine;

/// <summary>
/// Abstract base class for all AI states.
/// Provides a context reference to the main EnemyAI controller and defines the core state methods.
/// </summary>
public abstract class AIStateBase
{
    protected readonly EnemyAI enemyAI;
    protected readonly EnemyVisionSystem visionSystem;
    protected readonly EnemyMovementController movementController;
    protected readonly EnemyItemHolder itemHolder;
    protected readonly EnemyItemSeeker itemSeeker;
    protected readonly EnemySoundInvestigator soundInvestigator;
    protected readonly EnemyStunController stunController;

    public AIStateBase(EnemyAI context)
    {
        this.enemyAI = context;
        // These properties will be exposed on EnemyAI later
        this.visionSystem = context.VisionSystem;
        this.movementController = context.MovementController;
        this.itemHolder = context.ItemHolder;
        this.itemSeeker = context.ItemSeeker;
        this.soundInvestigator = context.SoundInvestigator;
        this.stunController = context.StunController;
    }

    /// <summary>
    /// Called when the state is entered.
    /// </summary>
    public abstract void EnterState();

    /// <summary>
    /// Called every frame while the state is active.
    /// </summary>
    public abstract void UpdateState();

    /// <summary>
    /// Called when the state is exited.
    /// </summary>
    public abstract void ExitState();
}
