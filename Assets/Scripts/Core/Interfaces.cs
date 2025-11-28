using UnityEngine;

/// <summary>
/// Interface for entities that can hold and use items/weapons
/// Implemented by PlayerControls and EnemyWeaponController
/// </summary>
public interface IItemHolder
{
    /// <summary>
    /// Get the transform position where items should be held
    /// </summary>
    Transform GetHoldPosition(ItemHoldType holdType);

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
/// Implemented by PlayerHealth, EnemyHealth
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

/// <summary>
/// Interface for objects that can receive speed boosts from SpeedBoostZone
/// Implemented by EnemyAI (and potentially PlayerControls)
/// </summary>
public interface ISpeedBoostable
{
    /// <summary>
    /// Apply speed boost with given multiplier
    /// </summary>
    void ApplySpeedBoost(float multiplier);

    /// <summary>
    /// Remove currently applied speed boost
    /// </summary>
    /// <param name="fadeDuration">Time in seconds to fade out the boost. 0 for instant removal.</param>
    void RemoveSpeedBoost(float fadeDuration = 0f);

    /// <summary>
    /// Check if speed boost is currently active
    /// </summary>
    bool IsSpeedBoosted { get; }
}
