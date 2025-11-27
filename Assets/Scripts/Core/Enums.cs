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

// NOTE: AIState enum moved to Assets/Scripts/Enemy/AIState.cs
// as it is enemy-specific and not a core shared enum
