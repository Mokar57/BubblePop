using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Death UI manager - decoupled from player
/// Listens to GameEvents.OnPlayerDied and shows death screen
/// Connects to LevelManager for restart functionality
/// </summary>
public class DeathUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Death screen panel (requires CanvasGroup for fade animation)")]
    public GameObject deathPanel;

    [Tooltip("Restart button - calls LevelManager.RestartLevel()")]
    public Button restartButton;

    [Header("Optional References")]
    [Tooltip("Quit button - calls LevelManager.QuitGame()")]
    public Button quitButton;

    [Header("Animation Settings")]
    [Tooltip("Duration of UI fade-in animation (seconds)")]
    public float fadeInDuration = 0.5f;

    [Tooltip("Delay before showing UI after death (seconds)")]
    public float showDelay = 1f;

    private LevelManager levelManager;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Get CanvasGroup for fade animation (optional)
        canvasGroup = deathPanel.GetComponent<CanvasGroup>();

        // Find LevelManager in scene
        levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager == null)
        {
            Debug.LogError("DeathUI: LevelManager not found! Scene must have a LevelManager component.");
        }

        // Hide UI initially
        HideDeathPanel();
    }

    private void OnEnable()
    {
        // Subscribe to player death event
        GameEvents.OnPlayerDied += HandlePlayerDeath;

        // Connect button click events
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events (prevent memory leaks)
        GameEvents.OnPlayerDied -= HandlePlayerDeath;

        // Remove button listeners
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }
    }

    /// <summary>
    /// Called when player dies (via GameEvents)
    /// </summary>
    private void HandlePlayerDeath()
    {
        Debug.Log("DeathUI: Player death detected, showing death panel");

        // Show UI with delay
        if (showDelay > 0)
        {
            Invoke(nameof(ShowDeathPanel), showDelay);
        }
        else
        {
            ShowDeathPanel();
        }
    }

    /// <summary>
    /// Show the death panel with fade-in animation
    /// </summary>
    private void ShowDeathPanel()
    {
        if (deathPanel == null)
        {
            Debug.LogError("DeathUI: Death panel reference not assigned!");
            return;
        }

        deathPanel.SetActive(true);

        // Fade-in animation
        if (canvasGroup != null && fadeInDuration > 0)
        {
            StartCoroutine(FadeIn());
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Show cursor (in case it was hidden during gameplay)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Hide the death panel
    /// </summary>
    private void HideDeathPanel()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
    }

    /// <summary>
    /// Fade-in animation coroutine
    /// </summary>
    private System.Collections.IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time (works even if Time.timeScale = 0)
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Called when restart button is clicked
    /// Calls LevelManager.RestartLevel() to reload current scene
    /// </summary>
    private void OnRestartButtonClicked()
    {
        Debug.Log("DeathUI: Restart button clicked");

        if (levelManager != null)
        {
            levelManager.RestartLevel();
        }
        else
        {
            Debug.LogError("DeathUI: LevelManager not found!");
        }
    }

    /// <summary>
    /// Called when quit button is clicked
    /// </summary>
    private void OnQuitButtonClicked()
    {
        Debug.Log("DeathUI: Quit button clicked");

        if (levelManager != null)
        {
            levelManager.QuitGame();
        }
    }
}
