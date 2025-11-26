using System;
using UnityEngine;

/// <summary>
/// Decoupled event system for the game
/// Manages all important game events to decouple systems
///
/// IMPORTANT: Always unsubscribe in OnDisable to prevent memory leaks:
/// void OnEnable() => GameEvents.OnSoundEmitted += HandleSound;
/// void OnDisable() => GameEvents.OnSoundEmitted -= HandleSound;
/// </summary>
public static class GameEvents
{
    // ==================== PLAYER EVENTS ====================

    /// <summary>
    /// Player died event
    /// </summary>
    public static event Action OnPlayerDied;

    /// <summary>
    /// Trigger player death event
    /// </summary>
    public static void TriggerPlayerDeath()
    {
        OnPlayerDied?.Invoke();
    }

    // ==================== LEVEL EVENTS ====================

    /// <summary>
    /// Level restarted event
    /// </summary>
    public static event Action OnLevelRestarted;

    /// <summary>
    /// Trigger level restart event
    /// </summary>
    public static void TriggerLevelRestart()
    {
        OnLevelRestarted?.Invoke();
    }

    // ==================== SOUND & AI EVENTS ====================

    /// <summary>
    /// Sound emitted event (position, alertRadius)
    /// Used by enemies to detect sounds like gunshots
    /// </summary>
    public static event Action<Vector2, float> OnSoundEmitted;

    /// <summary>
    /// Trigger sound emission event
    /// </summary>
    public static void TriggerSoundEmitted(Vector2 position, float alertRadius)
    {
        OnSoundEmitted?.Invoke(position, alertRadius);
    }

    // ==================== COMBAT EVENTS ====================

    /// <summary>
    /// Enemy died event (enemy GameObject)
    /// </summary>
    public static event Action<GameObject> OnEnemyDied;

    /// <summary>
    /// Trigger enemy death event
    /// </summary>
    public static void TriggerEnemyDied(GameObject enemy)
    {
        OnEnemyDied?.Invoke(enemy);
    }

    /// <summary>
    /// Enemy hit by projectile event (hit position)
    /// Useful for VFX and statistics
    /// </summary>
    public static event Action<Vector2> OnEnemyHitByProjectile;

    /// <summary>
    /// Trigger enemy hit by projectile event
    /// </summary>
    public static void TriggerEnemyHitByProjectile(Vector2 hitPosition)
    {
        OnEnemyHitByProjectile?.Invoke(hitPosition);
    }

    // ==================== BOUNCE EVENTS ====================

    /// <summary>
    /// Entity started bouncing event (entity GameObject)
    /// </summary>
    public static event Action<GameObject> OnEntityStartedBouncing;

    /// <summary>
    /// Trigger entity started bouncing event
    /// </summary>
    public static void TriggerEntityStartedBouncing(GameObject entity)
    {
        OnEntityStartedBouncing?.Invoke(entity);
    }

    /// <summary>
    /// Entity stopped bouncing event (entity GameObject)
    /// </summary>
    public static event Action<GameObject> OnEntityStoppedBouncing;

    /// <summary>
    /// Trigger entity stopped bouncing event
    /// </summary>
    public static void TriggerEntityStoppedBouncing(GameObject entity)
    {
        OnEntityStoppedBouncing?.Invoke(entity);
    }

    // ==================== ITEM EVENTS ====================

    /// <summary>
    /// Item picked up event (item GameObject, holder GameObject)
    /// </summary>
    public static event Action<GameObject, GameObject> OnItemPickedUp;

    /// <summary>
    /// Trigger item picked up event
    /// </summary>
    public static void TriggerItemPickedUp(GameObject item, GameObject holder)
    {
        OnItemPickedUp?.Invoke(item, holder);
    }

    /// <summary>
    /// Item dropped event (item GameObject, holder GameObject)
    /// </summary>
    public static event Action<GameObject, GameObject> OnItemDropped;

    /// <summary>
    /// Trigger item dropped event
    /// </summary>
    public static void TriggerItemDropped(GameObject item, GameObject holder)
    {
        OnItemDropped?.Invoke(item, holder);
    }

    // ==================== ENVIRONMENT EVENTS ====================

    /// <summary>
    /// Door opened event (position, stun radius)
    /// Can stun nearby enemies
    /// </summary>
    public static event Action<Vector2, float> OnDoorOpened;

    /// <summary>
    /// Trigger door opened event
    /// </summary>
    public static void TriggerDoorOpened(Vector2 position, float stunRadius)
    {
        OnDoorOpened?.Invoke(position, stunRadius);
    }

    // ==================== EVENT CLEANUP ====================

    /// <summary>
    /// Clear all events - call on level unload to prevent memory leaks
    /// CRITICAL: Call this when changing scenes or restarting levels
    /// </summary>
    public static void ClearAllEvents()
    {
        OnPlayerDied = null;
        OnLevelRestarted = null;
        OnSoundEmitted = null;
        OnEnemyDied = null;
        OnEnemyHitByProjectile = null;
        OnEntityStartedBouncing = null;
        OnEntityStoppedBouncing = null;
        OnItemPickedUp = null;
        OnItemDropped = null;
        OnDoorOpened = null;
    }
}
