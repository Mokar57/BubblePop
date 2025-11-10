using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level yönetimi için decoupled manager
/// Event sistemi üzerinden player death'i dinler ve gerekli aksiyonları alır
/// Player sınıfına hiçbir referans bulunmaz
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Restart edildiğinde yüklenecek sahne. Boş bırakılırsa aktif sahne yeniden yüklenir.")]
    public string sceneToLoad = "";
    
    [Tooltip("Restart etmeden önce beklenecek süre (saniye)")]
    public float restartDelay = 0.5f;

    private bool isRestarting = false;

    private void OnEnable()
    {
        // Event'e subscribe ol
        GameEvents.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        // Event'ten unsubscribe ol (memory leak önleme)
        GameEvents.OnPlayerDied -= HandlePlayerDeath;
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

        Debug.Log("LevelManager: Restarting level...");
        
        // Time scale'i normale döndür
        Time.timeScale = 1f;
        
        // Restart event'ini tetikle
        GameEvents.TriggerLevelRestart();
        
        // Sahneyi belirle
        string targetScene = string.IsNullOrEmpty(sceneToLoad) 
            ? SceneManager.GetActiveScene().name 
            : sceneToLoad;
        
        // Delay ile veya direkt yükle
        if (restartDelay > 0)
        {
            Invoke(nameof(LoadScene), restartDelay);
        }
        else
        {
            LoadScene();
        }
    }

    /// <summary>
    /// Sahneyi yükler
    /// </summary>
    private void LoadScene()
    {
        string targetScene = string.IsNullOrEmpty(sceneToLoad) 
            ? SceneManager.GetActiveScene().name 
            : sceneToLoad;
            
        SceneManager.LoadScene(targetScene);
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
}
