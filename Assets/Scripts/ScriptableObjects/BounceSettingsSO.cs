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

    [Header("Audio")]
    [Tooltip("Sound played when bouncing off walls")]
    public AudioClip wallBounceSound;

    [Tooltip("Volume of wall bounce sound")]
    [Range(0f, 1f)]
    public float wallBounceSoundVolume = 0.8f;

    [Header("Stun Visual Effect")]
    [Tooltip("Sprite to show above character's head during bounce (e.g., stars, birds)")]
    public Sprite stunSprite;

    [Tooltip("Enable stun sprite effect during bounce")]
    public bool enableStunSprite = true;

    [Tooltip("Offset position above character's head (in units)")]
    public Vector2 stunSpriteOffset = new Vector2(0f, 1.5f);

    [Tooltip("Size of the stun sprite")]
    public Vector2 stunSpriteSize = new Vector2(1f, 1f);

    [Tooltip("Enable floating animation for stun sprite")]
    public bool enableFloatingAnimation = true;

    [Tooltip("Float animation speed (cycles per second)")]
    public float floatAnimationSpeed = 2f;

    [Tooltip("Float animation range (in units)")]
    public float floatAnimationRange = 0.2f;
}
