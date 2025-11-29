using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Mouse-based camera panning extension for Cinemachine 3.
/// Pans the camera towards the mouse position when near screen edges.
/// Hold Shift for extended panning range.
/// </summary>
public class MousePanCameraExtension : CinemachineExtension
{
    [Header("Mouse Panning Settings")]
    [Tooltip("Maximum distance to pan the camera from the follow target")]
    public float maxPanDistance = 5f;

    [Tooltip("Additional pan distance when holding Shift")]
    public float shiftExtraPanDistance = 3f;

    [Tooltip("Percentage of screen edge to start panning (0.0 to 1.0). 0.3 means panning starts at 30% from screen edges")]
    [Range(0f, 0.5f)]
    public float edgeThreshold = 0.3f;

    [Tooltip("How smoothly the camera pans (higher = more responsive)")]
    public float panSmoothness = 5f;

    [Header("Input Settings")]
    [Tooltip("Key to hold for extended camera pan")]
    public KeyCode extendedPanKey = KeyCode.LeftShift;

    private Vector3 currentPanOffset = Vector3.zero;
    private Camera cachedMainCamera;

    protected override void Awake()
    {
        base.Awake();
        cachedMainCamera = Camera.main;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Only modify position during the Body stage
        if (stage != CinemachineCore.Stage.Body)
            return;

        // Calculate the target offset based on current mouse position
        Vector3 targetPanOffset = CalculateMousePanOffset();

        // Smoothly interpolate our stored offset towards the target
        // This creates smooth camera movement when mouse moves near edges
        currentPanOffset = Vector3.Lerp(
            currentPanOffset,
            targetPanOffset,
            panSmoothness * deltaTime
        );

        // Apply the offset to the camera position
        // state.RawPosition already contains the player position from Cinemachine's follow
        // We're just adding a slight offset towards the mouse
        state.RawPosition += currentPanOffset;
    }

    private Vector3 CalculateMousePanOffset()
    {
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null)
                return Vector3.zero;
        }

        // Get mouse position in viewport space (0-1 range)
        Vector3 mouseViewportPos = cachedMainCamera.ScreenToViewportPoint(Input.mousePosition);

        // Apply edge threshold - only pan when mouse is near edges
        Vector2 panDirection = Vector2.zero;

        // Horizontal panning - check if mouse is in left or right edge zone
        if (mouseViewportPos.x < edgeThreshold)
        {
            // Left edge - calculate how deep into the edge zone (0 to 1)
            float edgeInfluence = (edgeThreshold - mouseViewportPos.x) / edgeThreshold;
            panDirection.x = -Mathf.Clamp01(edgeInfluence);
        }
        else if (mouseViewportPos.x > (1f - edgeThreshold))
        {
            // Right edge
            float edgeInfluence = (mouseViewportPos.x - (1f - edgeThreshold)) / edgeThreshold;
            panDirection.x = Mathf.Clamp01(edgeInfluence);
        }

        // Vertical panning - check if mouse is in bottom or top edge zone
        if (mouseViewportPos.y < edgeThreshold)
        {
            // Bottom edge
            float edgeInfluence = (edgeThreshold - mouseViewportPos.y) / edgeThreshold;
            panDirection.y = -Mathf.Clamp01(edgeInfluence);
        }
        else if (mouseViewportPos.y > (1f - edgeThreshold))
        {
            // Top edge
            float edgeInfluence = (mouseViewportPos.y - (1f - edgeThreshold)) / edgeThreshold;
            panDirection.y = Mathf.Clamp01(edgeInfluence);
        }

        // Determine max pan distance based on shift key
        float currentMaxPan = maxPanDistance;
        if (Input.GetKey(extendedPanKey))
        {
            currentMaxPan += shiftExtraPanDistance;
        }

        // Convert pan direction to world space offset
        Vector3 panOffset = new Vector3(
            panDirection.x * currentMaxPan,
            panDirection.y * currentMaxPan,
            0f
        );

        return panOffset;
    }

    // Optional: Reset pan offset when the camera transitions in
    public override bool OnTransitionFromCamera(
        ICinemachineCamera fromCam,
        Vector3 worldUp,
        float deltaTime)
    {
        currentPanOffset = Vector3.zero;
        return base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
    }
}
