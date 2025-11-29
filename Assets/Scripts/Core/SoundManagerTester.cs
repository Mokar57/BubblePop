using UnityEngine;

/// <summary>
/// Debug/Test script for SoundManager
/// Attach to any GameObject and use keyboard to test sound playback
///
/// Controls:
/// - Press 1-5: Play test sounds with increasing volumes
/// - Press Space: Play multiple sounds simultaneously (stress test)
/// - Press S: Stop all sounds
/// - Press A: Play sound with alert (triggers enemy detection)
/// </summary>
public class SoundManagerTester : MonoBehaviour
{
    [Header("Test Audio Clips")]
    [Tooltip("Assign test audio clips to test the sound system")]
    public AudioClip[] testClips;

    [Header("Test Settings")]
    [Tooltip("Number of sounds to play simultaneously in stress test")]
    public int stressTestCount = 15;

    [Tooltip("Alert radius for sound alert test")]
    public float testAlertRadius = 20f;

    private void Update()
    {
        // Test simple sound playback
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestPlaySound(0, 0.2f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestPlaySound(0, 0.4f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestPlaySound(0, 0.6f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestPlaySound(0, 0.8f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TestPlaySound(0, 1.0f);
        }

        // Stress test - play many sounds at once
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StressTest();
        }

        // Stop all sounds
        if (Input.GetKeyDown(KeyCode.S))
        {
            StopAllSounds();
        }

        // Test sound with alert
        if (Input.GetKeyDown(KeyCode.A))
        {
            TestSoundWithAlert();
        }

        // Test 3D sound
        if (Input.GetKeyDown(KeyCode.D))
        {
            Test3DSound();
        }
    }

    private void TestPlaySound(int clipIndex, float volume)
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogError("SoundManager not found in scene!");
            return;
        }

        if (testClips == null || testClips.Length == 0)
        {
            Debug.LogWarning("No test clips assigned to SoundManagerTester!");
            return;
        }

        clipIndex = Mathf.Clamp(clipIndex, 0, testClips.Length - 1);
        AudioClip clip = testClips[clipIndex];

        if (clip != null)
        {
            SoundManager.Instance.PlaySound(clip, volume);
            Debug.Log($"Playing sound: {clip.name} at volume {volume}");
        }
    }

    private void StressTest()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogError("SoundManager not found in scene!");
            return;
        }

        if (testClips == null || testClips.Length == 0)
        {
            Debug.LogWarning("No test clips assigned!");
            return;
        }

        Debug.Log($"Starting stress test: Playing {stressTestCount} sounds simultaneously...");

        for (int i = 0; i < stressTestCount; i++)
        {
            // Random clip and volume
            AudioClip clip = testClips[Random.Range(0, testClips.Length)];
            float volume = Random.Range(0.5f, 1f);

            if (clip != null)
            {
                SoundManager.Instance.PlaySound(clip, volume);
            }
        }

        Debug.Log($"Stress test complete. Pool status: {SoundManager.Instance.GetPlayingSoundCount()} sounds playing");
    }

    private void StopAllSounds()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllSounds();
            Debug.Log("All sounds stopped");
        }
    }

    private void TestSoundWithAlert()
    {
        if (SoundManager.Instance == null || testClips == null || testClips.Length == 0)
        {
            Debug.LogWarning("Cannot test sound with alert - missing SoundManager or clips");
            return;
        }

        AudioClip clip = testClips[0];
        if (clip != null)
        {
            SoundManager.Instance.PlaySoundWithAlert(clip, transform.position, testAlertRadius, 1f);
            Debug.Log($"Playing sound with alert at {transform.position} with radius {testAlertRadius}");
        }
    }

    private void Test3DSound()
    {
        if (SoundManager.Instance == null || testClips == null || testClips.Length == 0)
        {
            Debug.LogWarning("Cannot test 3D sound - missing SoundManager or clips");
            return;
        }

        AudioClip clip = testClips[0];
        if (clip != null)
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * 5f;
            SoundManager.Instance.PlaySound3D(clip, randomPos, 1f, 1f, 10f);
            Debug.Log($"Playing 3D sound at {randomPos}");
        }
    }

    private void OnGUI()
    {
        if (SoundManager.Instance == null) return;

        GUILayout.BeginArea(new Rect(10, 120, 400, 300));
        GUILayout.Box("=== SoundManager Tester ===");
        GUILayout.Label("Controls:");
        GUILayout.Label("1-5: Play sound at different volumes");
        GUILayout.Label("Space: Stress test (play many sounds)");
        GUILayout.Label("S: Stop all sounds");
        GUILayout.Label("A: Play sound with enemy alert");
        GUILayout.Label("D: Play 3D positional sound");
        GUILayout.Space(10);
        GUILayout.Label($"Test Clips: {(testClips != null ? testClips.Length : 0)}");
        GUILayout.Label($"Playing Sounds: {SoundManager.Instance.GetPlayingSoundCount()}");
        GUILayout.Label($"Pool Full: {SoundManager.Instance.IsPoolFull()}");
        GUILayout.EndArea();
    }
}
