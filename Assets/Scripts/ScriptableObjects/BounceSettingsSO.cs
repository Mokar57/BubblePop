using UnityEngine;

/// <summary>
/// ScriptableObject for bounce behavior configuration
/// Used by BounceController to configure bounce physics and behavior
/// Separate assets for Player vs Enemy allow different bounce characteristics
/// </summary>
[CreateAssetMenu(fileName = "NewBounceSettings", menuName = "PopTheBubble/Bounce Settings")]
public class BounceSettingsSO : ScriptableObject
{
    [Header("Basic Bounce Settings")]
    [Tooltip("How long the bounce state lasts (seconds)")]
    public float bounceDuration = 1f;

    [Tooltip("Initial force applied when bounce starts")]
    public float bounceForce = 10f;

    [Header("Wall Bounce")]
    [Tooltip("Velocity multiplier when bouncing off walls (1.0 = full bounce, 0.5 = half)")]
    [Range(0f, 1f)]
    public float wallBounceMultiplier = 0.7f;

    [Tooltip("Maximum number of wall bounces before stopping")]
    public int maxBounces = 5;

    [Tooltip("Minimum velocity to continue bouncing (stops if below this)")]
    public float minimumBounceVelocity = 0.5f;

    [Header("Physics")]
    [Tooltip("Linear drag while bouncing (higher = slower)")]
    public float linearDrag = 0.5f;

    [Tooltip("Angular drag while bouncing (rotation dampening)")]
    public float angularDrag = 0.5f;

    [Header("Effects")]
    [Tooltip("Enable screen shake when entity bounces (usually only for player)")]
    public bool enableScreenShake = false;

    [Tooltip("Screen shake intensity")]
    public float screenShakeIntensity = 0.2f;

    [Tooltip("Should entity drop held weapon when bounce starts?")]
    public bool dropWeaponOnBounce = true;

    [Header("Visual Feedback")]
    [Tooltip("Sprite color tint during bounce")]
    public Color bounceColor = new Color(1f, 0.8f, 0.3f, 1f);

    [Tooltip("Enable color tint during bounce")]
    public bool enableColorTint = true;
}
