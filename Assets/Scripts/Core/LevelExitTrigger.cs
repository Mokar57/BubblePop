using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Exit trigger zone that loads next scene when player enters
/// Activated by LevelManager when all enemies are cleared
///
/// Setup:
/// 1. Create GameObject with this component
/// 2. Add 2D Collider (set as Trigger)
/// 3. Assign next scene name
/// 4. Assign this GameObject to LevelManager.exitArea
/// 5. LevelManager will activate it when enemies cleared
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExitTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load when player enters")]
    [SerializeField] private string nextSceneName;

    [Header("Transition")]
    [Tooltip("Delay before loading next scene (seconds)")]
    [SerializeField] private float transitionDelay = 0.5f;

    [Tooltip("Show transition message in console")]
    [SerializeField] private bool debugMode = false;

    private bool isTransitioning = false;

    private void Awake()
    {
        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"LevelExitTrigger: Collider on {name} was not a trigger. Auto-fixed.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if player entered
        if (other.CompareTag("Player") && !isTransitioning)
        {
            if (debugMode)
            {
                Debug.Log($"LevelExitTrigger: Player entered exit area. Loading {nextSceneName}...");
            }

            LoadNextLevel();
        }
    }

    /// <summary>
    /// Load the next scene
    /// </summary>
    private void LoadNextLevel()
    {
        if (isTransitioning) return;

        isTransitioning = true;

        // Validate scene name
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("LevelExitTrigger: Next scene name not assigned!");
            isTransitioning = false;
            return;
        }

        // Optional: Play exit sound
        if (SoundManager.Instance != null)
        {
            // Could add exitSound field and play here
            // SoundManager.Instance.PlaySound(exitSound, 1f);
        }

        // Load scene with optional delay
        if (transitionDelay > 0)
        {
            Invoke(nameof(LoadScene), transitionDelay);
        }
        else
        {
            LoadScene();
        }
    }

    /// <summary>
    /// Actually load the scene
    /// </summary>
    private void LoadScene()
    {
        // Clean up events before scene change
        GameEvents.ClearAllEvents();

        if (debugMode)
        {
            Debug.Log($"LevelExitTrigger: Loading scene '{nextSceneName}'");
        }

        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Public method to trigger transition (can be called by UI or other systems)
    /// </summary>
    public void TriggerTransition()
    {
        LoadNextLevel();
    }

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        // Draw exit area in editor
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Green transparent
            Gizmos.DrawCube(transform.position, col.bounds.size);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw selected exit area
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }

    #endregion
}
