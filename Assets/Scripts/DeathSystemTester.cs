using UnityEngine;

/// <summary>
/// Death sistem testleri için yardımcı script
/// Test amaçlı kullanılır, production'da silinebilir
/// </summary>
public class DeathSystemTester : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Player objesine referans (otomatik bulunur)")]
    public PlayerControls player;
    
    [Tooltip("Test için kullanılacak damage miktarı")]
    public float testDamage = 50f;

    private void Start()
    {
        // Player'ı otomatik bul
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerControls>();
            
            if (player == null)
            {
                Debug.LogWarning("DeathSystemTester: Player bulunamadı!");
            }
        }
    }

    private void Update()
    {
        // K tuşu ile damage test
        if (Input.GetKeyDown(KeyCode.K))
        {
            TestDamage();
        }
        
        // L tuşu ile instant death test
        if (Input.GetKeyDown(KeyCode.L))
        {
            TestInstantDeath();
        }
    }

    /// <summary>
    /// Player'a damage verir (K tuşu)
    /// </summary>
    [ContextMenu("Test Damage")]
    public void TestDamage()
    {
        if (player != null)
        {
            Debug.Log($"DeathSystemTester: Giving {testDamage} damage to player");
            player.TakeDamage(testDamage);
        }
        else
        {
            Debug.LogWarning("DeathSystemTester: Player referansı yok!");
        }
    }

    /// <summary>
    /// Player'ı anında öldürür (L tuşu)
    /// </summary>
    [ContextMenu("Test Instant Death")]
    public void TestInstantDeath()
    {
        if (player != null)
        {
            Debug.Log("DeathSystemTester: Instantly killing player");
            player.TakeDamage(1000f); // Yüksek damage
        }
        else
        {
            Debug.LogWarning("DeathSystemTester: Player referansı yok!");
        }
    }

    /// <summary>
    /// Event sistemini test eder
    /// </summary>
    [ContextMenu("Test Event System")]
    public void TestEventSystem()
    {
        Debug.Log("DeathSystemTester: Triggering player death event manually");
        GameEvents.TriggerPlayerDeath();
    }

    /// <summary>
    /// Restart event'ini test eder
    /// </summary>
    [ContextMenu("Test Restart Event")]
    public void TestRestartEvent()
    {
        Debug.Log("DeathSystemTester: Triggering level restart event manually");
        GameEvents.TriggerLevelRestart();
    }
}
