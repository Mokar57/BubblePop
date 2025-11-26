/// <summary>
/// Core enums used throughout the game
/// Defines teams, damage types, and AI states
/// </summary>

/// <summary>
/// Represents which team an entity belongs to
/// Used to prevent friendly fire and determine damage application
/// </summary>
public enum Team
{
    Player,   // Player team
    Enemy,    // Enemy team
    Neutral   // Neutral entities (environmental hazards, etc.)
}

/// <summary>
/// Type of damage being dealt
/// Determines what happens when damage is applied
/// </summary>
public enum DamageType
{
    Piercing,  // Instant kill damage (bullets, stabbing)
    Blunt      // Causes bounce/knockback (baseball bat, fan projectiles)
}

/// <summary>
/// AI state machine states for enemy behavior
/// </summary>
public enum AIState
{
    Patrolling,          // Following patrol waypoints
    Chasing,             // Actively pursuing target
    WaitingAtWaypoint,   // Waiting at a waypoint with optional rotation
    SearchingLastKnown,  // Moving to last known position of target
    WaitingAfterTimeout, // Waiting after failing to reach waypoint
    WaitingAfterStuck,   // Waiting after being stuck
    Stunned              // Stunned by area effect (door slam, etc.)
}
