using UnityEngine;

/// <summary>
/// Interface for entities that can hold and use items/weapons
/// Implemented by PlayerControls and EnemyItemHolder
/// </summary>
public interface IItemHolder
{
    /// <summary>
    /// Get the transform position where items should be held
    /// </summary>
    Transform GetHoldPosition();

    /// <summary>
    /// Get the team of the entity holding the item
    /// </summary>
    Team GetTeam();

    /// <summary>
    /// Called when an item is picked up by this holder
    /// </summary>
    void OnItemPickedUp(GameObject item);

    /// <summary>
    /// Called when an item is dropped by this holder
    /// </summary>
    void OnItemDropped(GameObject item);
}

/// <summary>
/// Interface for entities that can take damage
/// Implemented by PlayerHealth, EnemyHealth (to be created)
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Apply damage to this entity using DamageInfo struct
    /// </summary>
    void TakeDamage(DamageInfo damageInfo);

    /// <summary>
    /// Get the team of this entity
    /// </summary>
    Team GetTeam();

    /// <summary>
    /// Check if this entity is dead
    /// </summary>
    bool IsDead { get; }
}
