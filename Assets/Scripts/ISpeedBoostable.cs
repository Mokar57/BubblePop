using UnityEngine;

/// <summary>
/// Interface for objects that can receive speed boosts from SpeedBoostZone
/// </summary>
public interface ISpeedBoostable
{
    /// <summary>
    /// Apply speed boost with given multiplier
    /// </summary>
    /// <param name="multiplier">Speed multiplier to apply</param>
    void ApplySpeedBoost(float multiplier);
    
    /// <summary>
    /// Remove currently applied speed boost
    /// </summary>
    void RemoveSpeedBoost();
    
    /// <summary>
    /// Check if speed boost is currently active
    /// </summary>
    bool IsSpeedBoosted { get; }
}