using UnityEngine;

/// <summary>
/// Defines the boundaries for camera movement.
/// Can be set manually or automatically detected from a Collider2D.
/// </summary>
public class CameraBounds : MonoBehaviour
{
    [Header("Bounds Settings")]
    [Tooltip("Automatically calculate bounds from a Collider2D component")]
    public bool autoDetectFromCollider = true;

    [Tooltip("The collider that defines the camera bounds (PolygonCollider2D, BoxCollider2D, etc.)")]
    public Collider2D boundsCollider;

    [Header("Manual Bounds (used if autoDetectFromCollider is false)")]
    public Vector2 minBounds = new Vector2(-10f, -10f);
    public Vector2 maxBounds = new Vector2(10f, 10f);

    [Header("Camera Size Adjustment")]
    [Tooltip("Add padding to bounds based on camera size to prevent seeing outside the map")]
    public bool adjustForCameraSize = true;

    [Header("Debug Visualization")]
    [Tooltip("Show debug gizmos in Scene view")]
    public bool showDebugGizmos = true;
    
    [Tooltip("Color for the outer bounds")]
    public Color outerBoundsColor = Color.green;
    
    [Tooltip("Color for the camera-adjusted inner bounds")]
    public Color innerBoundsColor = Color.yellow;
    
    [Tooltip("Show corner markers")]
    public bool showCornerMarkers = true;
    
    [Tooltip("Size of corner marker spheres")]
    public float cornerMarkerSize = 0.5f;
    
    [Tooltip("Show center marker")]
    public bool showCenterMarker = true;
    
    [Tooltip("Show current camera position in play mode")]
    public bool showCameraPosition = true;

    private static CameraBounds instance;
    public static CameraBounds Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (autoDetectFromCollider && boundsCollider == null)
        {
            boundsCollider = GetComponent<Collider2D>();
        }
    }

    /// <summary>
    /// Get the calculated bounds for the camera
    /// </summary>
    public Bounds GetBounds()
    {
        if (autoDetectFromCollider && boundsCollider != null)
        {
            return boundsCollider.bounds;
        }
        else
        {
            Vector3 center = new Vector3(
                (minBounds.x + maxBounds.x) / 2f,
                (minBounds.y + maxBounds.y) / 2f,
                0f
            );
            Vector3 size = new Vector3(
                maxBounds.x - minBounds.x,
                maxBounds.y - minBounds.y,
                0f
            );
            return new Bounds(center, size);
        }
    }

    /// <summary>
    /// Clamp a world position to stay within the camera bounds
    /// </summary>
    public Vector3 ClampPositionToBounds(Vector3 position, Camera cam = null)
    {
        Bounds bounds = GetBounds();
        
        float halfHeight = 5f;
        float halfWidth = 5f;

        if (adjustForCameraSize && cam != null)
        {
            halfHeight = cam.orthographicSize;
            halfWidth = halfHeight * cam.aspect;
        }

        // Calculate the clamped position
        float clampedX = Mathf.Clamp(
            position.x,
            bounds.min.x + halfWidth,
            bounds.max.x - halfWidth
        );

        float clampedY = Mathf.Clamp(
            position.y,
            bounds.min.y + halfHeight,
            bounds.max.y - halfHeight
        );

        return new Vector3(clampedX, clampedY, position.z);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos)
            return;

        Bounds bounds = GetBounds();
        Camera cam = Camera.main;

        // Draw outer bounds (map boundaries)
        Gizmos.color = outerBoundsColor;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Draw corner markers for outer bounds
        if (showCornerMarkers)
        {
            Gizmos.color = outerBoundsColor;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(bounds.min.x, bounds.min.y, 0f), // Bottom-left
                new Vector3(bounds.max.x, bounds.min.y, 0f), // Bottom-right
                new Vector3(bounds.min.x, bounds.max.y, 0f), // Top-left
                new Vector3(bounds.max.x, bounds.max.y, 0f)  // Top-right
            };

            foreach (Vector3 corner in corners)
            {
                Gizmos.DrawSphere(corner, cornerMarkerSize);
            }
        }

        // Draw center marker
        if (showCenterMarker)
        {
            Gizmos.color = outerBoundsColor;
            Gizmos.DrawWireSphere(bounds.center, cornerMarkerSize * 0.75f);
            Gizmos.DrawLine(bounds.center + Vector3.left * 0.5f, bounds.center + Vector3.right * 0.5f);
            Gizmos.DrawLine(bounds.center + Vector3.down * 0.5f, bounds.center + Vector3.up * 0.5f);
        }

        // Draw camera-adjusted inner bounds (actual camera movement area)
        if (adjustForCameraSize && cam != null)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            float innerMinX = bounds.min.x + halfWidth;
            float innerMaxX = bounds.max.x - halfWidth;
            float innerMinY = bounds.min.y + halfHeight;
            float innerMaxY = bounds.max.y - halfHeight;

            // Only draw if bounds are valid
            if (innerMaxX > innerMinX && innerMaxY > innerMinY)
            {
                Vector3 innerCenter = new Vector3(
                    (innerMinX + innerMaxX) / 2f,
                    (innerMinY + innerMaxY) / 2f,
                    0f
                );
                Vector3 innerSize = new Vector3(
                    innerMaxX - innerMinX,
                    innerMaxY - innerMinY,
                    0f
                );

                Gizmos.color = innerBoundsColor;
                Gizmos.DrawWireCube(innerCenter, innerSize);

                // Draw camera FOV representation
                Gizmos.color = new Color(innerBoundsColor.r, innerBoundsColor.g, innerBoundsColor.b, 0.3f);
                Vector3 cameraSize = new Vector3(halfWidth * 2f, halfHeight * 2f, 0f);
                Gizmos.DrawWireCube(innerCenter, cameraSize);
            }
        }

        // Draw current camera position in play mode
        if (Application.isPlaying && showCameraPosition && cam != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cam.transform.position, 0.3f);
            
            // Draw camera viewport rectangle
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 cameraSize = new Vector3(halfWidth * 2f, halfHeight * 2f, 0f);
            Gizmos.DrawWireCube(cam.transform.position, cameraSize);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        Bounds bounds = GetBounds();
        Camera cam = Camera.main;

        // Draw semi-transparent outer bounds
        Gizmos.color = new Color(outerBoundsColor.r, outerBoundsColor.g, outerBoundsColor.b, 0.1f);
        Gizmos.DrawCube(bounds.center, bounds.size);

        // Draw semi-transparent inner bounds
        if (adjustForCameraSize && cam != null)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            float innerMinX = bounds.min.x + halfWidth;
            float innerMaxX = bounds.max.x - halfWidth;
            float innerMinY = bounds.min.y + halfHeight;
            float innerMaxY = bounds.max.y - halfHeight;

            if (innerMaxX > innerMinX && innerMaxY > innerMinY)
            {
                Vector3 innerCenter = new Vector3(
                    (innerMinX + innerMaxX) / 2f,
                    (innerMinY + innerMaxY) / 2f,
                    0f
                );
                Vector3 innerSize = new Vector3(
                    innerMaxX - innerMinX,
                    innerMaxY - innerMinY,
                    0f
                );

                Gizmos.color = new Color(innerBoundsColor.r, innerBoundsColor.g, innerBoundsColor.b, 0.15f);
                Gizmos.DrawCube(innerCenter, innerSize);
            }
        }
    }
}
