using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton Input Manager that wraps the new Input System.
/// Provides clean, easy-to-use events for all game inputs.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private GameControls controls;

    #region Input Values

    // Movement
    public Vector2 MoveInput { get; private set; }

    // Mouse
    public Vector2 MousePosition { get; private set; }
    public Vector2 MouseWorldPosition { get; private set; }

    // Camera
    public bool IsExtendingCameraPan { get; private set; }

    #endregion

    #region Events

    // Button events
    public event System.Action OnAttackPressed;
    public event System.Action OnThrowPressed;
    public event System.Action OnPickupPressed;

    #endregion

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize input system
        controls = new GameControls();
        SetupInputCallbacks();
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void SetupInputCallbacks()
    {
        // Movement - continuous value
        controls.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        // Mouse position - continuous value
        controls.Player.MousePosition.performed += ctx => UpdateMousePosition(ctx.ReadValue<Vector2>());

        // Attack - button press
        controls.Player.Attack.performed += ctx => OnAttackPressed?.Invoke();

        // Throw - button press
        controls.Player.Throw.performed += ctx => OnThrowPressed?.Invoke();

        // Pickup - button press
        controls.Player.Pickup.performed += ctx => OnPickupPressed?.Invoke();

        // Camera pan extend - hold button
        controls.Player.ExtendCameraPan.performed += ctx => IsExtendingCameraPan = true;
        controls.Player.ExtendCameraPan.canceled += ctx => IsExtendingCameraPan = false;
    }

    private void UpdateMousePosition(Vector2 screenPosition)
    {
        MousePosition = screenPosition;

        // Convert to world position if camera exists
        if (Camera.main != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
            MouseWorldPosition = new Vector2(worldPos.x, worldPos.y);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Get mouse direction from a world position (useful for rotation)
    /// </summary>
    public Vector2 GetMouseDirectionFrom(Vector2 position)
    {
        return MouseWorldPosition - position;
    }

    /// <summary>
    /// Enable/disable all player input
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
            controls.Player.Enable();
        else
            controls.Player.Disable();
    }

    #endregion

    private void OnDestroy()
    {
        controls?.Disable();
    }
}
