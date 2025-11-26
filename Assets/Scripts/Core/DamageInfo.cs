using UnityEngine;

/// <summary>
/// Struct containing all information about a damage event
/// Passed when dealing damage to ensure consistent damage handling
/// </summary>
public struct DamageInfo
{
    /// <summary>
    /// Amount of damage to deal
    /// </summary>
    public float damageAmount;

    /// <summary>
    /// Type of damage (Piercing = instant kill, Blunt = bounce)
    /// </summary>
    public DamageType damageType;

    /// <summary>
    /// Team that dealt this damage
    /// </summary>
    public Team sourceTeam;

    /// <summary>
    /// GameObject that caused the damage (weapon, projectile, etc.)
    /// </summary>
    public GameObject sourceObject;

    /// <summary>
    /// Direction of knockback (for blunt damage)
    /// </summary>
    public Vector2 knockbackDirection;

    /// <summary>
    /// Force of knockback (for blunt damage)
    /// </summary>
    public float knockbackForce;

    /// <summary>
    /// Constructor for creating DamageInfo with all parameters
    /// </summary>
    public DamageInfo(float amount, DamageType type, Team team, GameObject source, Vector2 knockback, float force)
    {
        damageAmount = amount;
        damageType = type;
        sourceTeam = team;
        sourceObject = source;
        knockbackDirection = knockback;
        knockbackForce = force;
    }

    /// <summary>
    /// Simple constructor for piercing damage (no knockback needed)
    /// </summary>
    public static DamageInfo CreatePiercing(float amount, Team team, GameObject source)
    {
        return new DamageInfo(amount, DamageType.Piercing, team, source, Vector2.zero, 0f);
    }

    /// <summary>
    /// Simple constructor for blunt damage with knockback
    /// </summary>
    public static DamageInfo CreateBlunt(float amount, Team team, GameObject source, Vector2 knockbackDir, float knockbackForce)
    {
        return new DamageInfo(amount, DamageType.Blunt, team, source, knockbackDir, knockbackForce);
    }
}
