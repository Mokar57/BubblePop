using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Level management system with event-based architecture
/// - Tracks player death and handles level restart
/// - Tracks alive enemies via event registration
/// - Activates exit area when all enemies cleared
/// - No direct references to Player or Enemy classes (fully decoupled)
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Restart Settings")]
    [Tooltip("Delay before restarting current scene (seconds)")]
    public float restartDelay = 0.5f;

    [Header("Exit Settings")]
    [Tooltip("GameObject to activate when all enemies are cleared (exit area trigger)")]
    public GameObject exitArea;

    [Header("Debug")]
    [Tooltip("Show level status in console and on-screen GUI")]
    public bool debugMode = false;

    // State tracking
    private bool isRestarting = false;
    private bool exitActivated = false;
    private HashSet<GameObject> aliveEnemies = new HashSet<GameObject>();

    // Public getters
    public int AliveEnemyCount => aliveEnemies.Count;
    public bool ExitActivated => exitActivated;
    public bool AllEnemiesCleared => aliveEnemies.Count == 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Deactivate exit initially
        if (exitArea != null)
        {
            exitArea.SetActive(false);
        }

        if (debugMode)
        {
            Debug.Log($"LevelManager initialized. Exit area: {(exitArea != null ? exitArea.name : "None")}");
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        GameEvents.OnPlayerDied += HandlePlayerDeath;
        GameEvents.OnEnemySpawned += RegisterEnemy;
        GameEvents.OnEnemyDied += UnregisterEnemy;
        
        // Subscribe to input events
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnRestartPressed += RestartLevel;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events (prevent memory leaks)
        GameEvents.OnPlayerDied -= HandlePlayerDeath;
        GameEvents.OnEnemySpawned -= RegisterEnemy;
        GameEvents.OnEnemyDied -= UnregisterEnemy;
        
        // Unsubscribe from input events
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnRestartPressed -= RestartLevel;
        }
    }

    /// <summary>
    /// Player öldüğünde çağrılır
    /// </summary>
    private void HandlePlayerDeath()
    {
        Debug.Log("LevelManager: Player death detected");
        
        if (!isRestarting)
        {
            isRestarting = true;
            // İsteğe bağlı: Burada time scale'i durdurabilirsin
            // Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Sahneyi yeniden başlatır (UI'dan çağrılacak)
    /// </summary>
    public void RestartLevel()
    {
        if (!isRestarting)
        {
            isRestarting = true;
        }

        Debug.Log("LevelManager: Restarting current scene...");

        // Reset time scale
        Time.timeScale = 1f;

        // Trigger restart event
        GameEvents.TriggerLevelRestart();

        // Clear all events before reload
        GameEvents.ClearAllEvents();

        // Load with delay or immediately
        if (restartDelay > 0)
        {
            Invoke(nameof(LoadCurrentScene), restartDelay);
        }
        else
        {
            LoadCurrentScene();
        }
    }

    /// <summary>
    /// Reload current scene
    /// </summary>
    private void LoadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"LevelManager: Reloading scene '{currentScene}'");
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// Oyundan çıkış (ana menüye dönüş için kullanılabilir)
    /// </summary>
    public void QuitToMenu(string menuSceneName)
    {
        Time.timeScale = 1f;
        GameEvents.ClearAllEvents();
        SceneManager.LoadScene(menuSceneName);
    }

    /// <summary>
    /// Oyunu tamamen kapat
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("LevelManager: Quitting game...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    #region Enemy Tracking

    /// <summary>
    /// Register an enemy as alive
    /// Called automatically via GameEvents.OnEnemySpawned
    /// </summary>
    private void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        if (aliveEnemies.Add(enemy))
        {
            if (debugMode)
            {
                Debug.Log($"LevelManager: Enemy registered - {enemy.name} | Total alive: {aliveEnemies.Count}");
            }
        }
    }

    /// <summary>
    /// Unregister an enemy (it died)
    /// Called automatically via GameEvents.OnEnemyDied
    /// </summary>
    private void UnregisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        if (aliveEnemies.Remove(enemy))
        {
            if (debugMode)
            {
                Debug.Log($"LevelManager: Enemy died - {enemy.name} | Remaining: {aliveEnemies.Count}");
            }

            // Check if all enemies cleared
            if (aliveEnemies.Count == 0)
            {
                OnAllEnemiesCleared();
            }
        }
    }

    /// <summary>
    /// Called when last enemy dies
    /// </summary>
    private void OnAllEnemiesCleared()
    {
        if (exitActivated) return; // Already activated

        exitActivated = true;

        if (debugMode)
        {
            Debug.Log("LevelManager: All enemies cleared! Activating exit area.");
        }

        // Activate exit area
        ActivateExit();

        // Trigger global event
        GameEvents.TriggerAllEnemiesCleared();
    }

    /// <summary>
    /// Activate the exit area
    /// </summary>
    private void ActivateExit()
    {
        if (exitArea != null)
        {
            exitArea.SetActive(true);

            // Optional: Play sound effect when exit activates
            if (SoundManager.Instance != null)
            {
                // Could add exitActivationSound field and play here
                // SoundManager.Instance.PlaySound(exitActivationSound, 1f);
            }

            if (debugMode)
            {
                Debug.Log($"LevelManager: Exit area activated - {exitArea.name}");
            }
        }
        else
        {
            Debug.LogWarning("LevelManager: Exit area not assigned! Cannot activate exit.");
        }
    }

    /// <summary>
    /// Force activate exit (for testing or special conditions)
    /// </summary>
    public void ForceActivateExit()
    {
        if (exitActivated) return;

        exitActivated = true;
        ActivateExit();
        GameEvents.TriggerAllEnemiesCleared();
    }

    /// <summary>
    /// Reset enemy tracking (called on level restart)
    /// </summary>
    private void ResetEnemyTracking()
    {
        aliveEnemies.Clear();
        exitActivated = false;

        if (exitArea != null)
        {
            exitArea.SetActive(false);
        }
    }

    #endregion

    #region Debug GUI

    private void OnGUI()
    {
        if (debugMode)
        {
            GUILayout.BeginArea(new Rect(10, 240, 300, 120));
            GUILayout.Box("=== Level Manager ===");
            GUILayout.Label($"Alive Enemies: {aliveEnemies.Count}");
            GUILayout.Label($"Exit Activated: {exitActivated}");
            GUILayout.Label($"Exit Area: {(exitArea != null ? exitArea.name : "None")}");
            if (GUILayout.Button("Force Activate Exit"))
            {
                ForceActivateExit();
            }
            GUILayout.EndArea();
        }
    }

    #endregion
}
