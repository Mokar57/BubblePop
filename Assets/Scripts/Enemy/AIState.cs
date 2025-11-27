/// <summary>
/// AI state machine states for enemy behavior
/// Located in Enemy folder as it's enemy-specific
/// </summary>
public enum AIState
{
    /// <summary>
    /// Following patrol waypoints (default state)
    /// </summary>
    Patrolling,

    /// <summary>
    /// Waiting at a patrol waypoint with optional look direction
    /// </summary>
    WaitingAtWaypoint,

    /// <summary>
    /// Seeking a weapon to pick up (priority when unarmed and player detected)
    /// Enemy cannot chase without a weapon
    /// </summary>
    SeekingItem,

    /// <summary>
    /// Actively pursuing and attacking target (requires weapon)
    /// Highest priority state when armed and player is visible
    /// </summary>
    Chasing,

    /// <summary>
    /// Investigating a heard sound location
    /// Can be interrupted by seeing player (if armed)
    /// </summary>
    Investigating,

    /// <summary>
    /// Moving to last known position of target after losing sight
    /// </summary>
    SearchingLastKnown,

    /// <summary>
    /// Waiting after failing to reach waypoint (timeout)
    /// </summary>
    WaitingAfterTimeout,

    /// <summary>
    /// Waiting after being stuck during navigation
    /// </summary>
    WaitingAfterStuck,

    /// <summary>
    /// Stunned by area effect (door slam, etc.)
    /// Cannot act until stun ends
    /// </summary>
    Stunned
}
