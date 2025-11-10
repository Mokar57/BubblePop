using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player öldüğünde gösterilecek UI'ı yöneten decoupled script
/// Event sistemi üzerinden player death'i dinler
/// </summary>
public class DeathUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Ölüm ekranı paneli (CanvasGroup veya GameObject)")]
    public GameObject deathPanel;
    
    [Tooltip("Restart butonu")]
    public Button restartButton;
    
    [Header("Optional References")]
    [Tooltip("Quit butonu (opsiyonel)")]
    public Button quitButton;
    
    [Header("Animation Settings")]
    [Tooltip("UI fade-in animasyonu için süre")]
    public float fadeInDuration = 0.5f;
    
    [Tooltip("UI gösterilmeden önceki delay")]
    public float showDelay = 1f;

    private LevelManager levelManager;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // CanvasGroup varsa al (fade animasyonu için)
        canvasGroup = deathPanel.GetComponent<CanvasGroup>();
        
        // LevelManager'ı bul
        levelManager = FindFirstObjectByType<LevelManager>();
        
        if (levelManager == null)
        {
            Debug.LogError("DeathUI: LevelManager bulunamadı! Sahnede LevelManager olmalı.");
        }
        
        // UI'ı başlangıçta gizle
        HideDeathPanel();
    }

    private void OnEnable()
    {
        // Event'e subscribe ol
        GameEvents.OnPlayerDied += HandlePlayerDeath;
        
        // Button click eventlerini bağla
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
        // Event'ten unsubscribe ol
        GameEvents.OnPlayerDied -= HandlePlayerDeath;
        
        // Button click eventlerini kaldır
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
    /// Player öldüğünde çağrılır
    /// </summary>
    private void HandlePlayerDeath()
    {
        Debug.Log("DeathUI: Player death detected, showing death panel");
        
        // Delay ile UI'ı göster
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
    /// Ölüm panelini gösterir
    /// </summary>
    private void ShowDeathPanel()
    {
        if (deathPanel == null)
        {
            Debug.LogError("DeathUI: Death panel referansı atanmamış!");
            return;
        }

        deathPanel.SetActive(true);
        
        // Fade-in animasyonu
        if (canvasGroup != null && fadeInDuration > 0)
        {
            StartCoroutine(FadeIn());
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        // Cursor'u göster (oyun sırasında gizliyse)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Ölüm panelini gizler
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
    /// Fade-in animasyonu
    /// </summary>
    private System.Collections.IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // unscaledDeltaTime kullan (time scale 0 olsa bile çalışır)
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Restart butonuna basıldığında
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
            Debug.LogError("DeathUI: LevelManager bulunamadı!");
        }
    }

    /// <summary>
    /// Quit butonuna basıldığında
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
