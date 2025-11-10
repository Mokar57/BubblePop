using System;
using UnityEngine;

/// <summary>
/// Decoupled event sistemi için static event manager
/// Bu class oyun içindeki tüm önemli eventleri yönetir
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// Player öldüğünde tetiklenir
    /// </summary>
    public static event Action OnPlayerDied;

    /// <summary>
    /// Player death event'ini tetikle
    /// </summary>
    public static void TriggerPlayerDeath()
    {
        OnPlayerDied?.Invoke();
        Debug.Log("GameEvents: Player death event triggered");
    }

    /// <summary>
    /// Level restart edildiğinde tetiklenir
    /// </summary>
    public static event Action OnLevelRestarted;

    /// <summary>
    /// Level restart event'ini tetikle
    /// </summary>
    public static void TriggerLevelRestart()
    {
        OnLevelRestarted?.Invoke();
        Debug.Log("GameEvents: Level restart event triggered");
    }

    /// <summary>
    /// Tüm eventleri temizle (scene değişimlerinde kullanılabilir)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnPlayerDied = null;
        OnLevelRestarted = null;
    }
}
