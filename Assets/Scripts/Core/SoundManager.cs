using UnityEngine;

/// <summary>
/// Centralized sound management system with AudioSource pooling
/// Handles sound playback and enemy alerting through GameEvents
///
/// Usage:
/// - Simple sound: SoundManager.Instance.PlaySound(clip, volume)
/// - Sound with enemy alert: SoundManager.Instance.PlaySoundWithAlert(clip, position, alertRadius, volume)
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source Pool")]
    [Tooltip("Number of AudioSources to pool for simultaneous sounds")]
    [SerializeField] private int poolSize = 10;

    [Header("Debug")]
    [Tooltip("Log sound playback events to console")]
    [SerializeField] private bool debugMode = false;

    // Audio source pool
    private AudioSource[] audioSourcePool;
    private int currentPoolIndex = 0;
    private float[] sourceStartTimes; // Track when each source started playing

    #region Initialization

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

        InitializeAudioPool();
    }

    /// <summary>
    /// Create AudioSource pool for sound playback
    /// </summary>
    private void InitializeAudioPool()
    {
        audioSourcePool = new AudioSource[poolSize];
        sourceStartTimes = new float[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound by default
            audioSourcePool[i] = source;
            sourceStartTimes[i] = -1f; // Not playing
        }

        if (debugMode)
        {
            Debug.Log($"SoundManager initialized with {poolSize} AudioSources");
        }
    }

    #endregion

    #region Sound Playback

    /// <summary>
    /// Play a simple sound without enemy alerting
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="volume">Volume (0-1)</param>
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundManager: Attempted to play null AudioClip");
            return;
        }

        AudioSource source = GetNextAvailableSource();
        source.volume = Mathf.Clamp01(volume);
        source.PlayOneShot(clip);

        if (debugMode)
        {
            Debug.Log($"Playing sound: {clip.name} (volume: {volume})");
        }
    }

    /// <summary>
    /// Play a sound with enemy alert radius (broadcasts to GameEvents)
    /// Used by ranged weapons to alert nearby enemies
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="position">World position where sound originated</param>
    /// <param name="alertRadius">How far enemies can hear this sound</param>
    /// <param name="volume">Volume (0-1)</param>
    public void PlaySoundWithAlert(AudioClip clip, Vector2 position, float alertRadius, float volume = 1f)
    {
        // Play the sound
        PlaySound(clip, volume);

        // Broadcast sound event to enemies
        if (alertRadius > 0)
        {
            GameEvents.TriggerSoundEmitted(position, alertRadius);

            if (debugMode)
            {
                Debug.Log($"Sound alert at {position} with radius {alertRadius}");
            }
        }
    }

    /// <summary>
    /// Play a 3D positional sound (spatialBlend = 1)
    /// Good for world sounds that should fade with distance
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="position">World position</param>
    /// <param name="volume">Volume (0-1)</param>
    /// <param name="minDistance">Minimum distance for 3D falloff</param>
    /// <param name="maxDistance">Maximum distance for 3D falloff</param>
    public void PlaySound3D(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 1f, float maxDistance = 10f)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundManager: Attempted to play null AudioClip");
            return;
        }

        AudioSource source = GetNextAvailableSource();
        source.volume = Mathf.Clamp01(volume);
        source.spatialBlend = 1f; // Full 3D
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.transform.position = position;
        source.PlayOneShot(clip);

        if (debugMode)
        {
            Debug.Log($"Playing 3D sound: {clip.name} at {position}");
        }
    }

    #endregion

    #region Pool Management

    /// <summary>
    /// Get the next available AudioSource from the pool
    /// Uses round-robin with interruption of oldest sound if all busy
    /// </summary>
    private AudioSource GetNextAvailableSource()
    {
        // Try to find an available (not playing) source
        for (int i = 0; i < poolSize; i++)
        {
            int index = (currentPoolIndex + i) % poolSize;
            if (!audioSourcePool[index].isPlaying)
            {
                currentPoolIndex = (index + 1) % poolSize;
                sourceStartTimes[index] = Time.time;
                ResetSourceSettings(audioSourcePool[index]);
                return audioSourcePool[index];
            }
        }

        // All sources busy - interrupt the oldest one
        int oldestIndex = GetOldestSourceIndex();
        currentPoolIndex = (oldestIndex + 1) % poolSize;
        sourceStartTimes[oldestIndex] = Time.time;

        if (debugMode)
        {
            Debug.LogWarning($"SoundManager: All {poolSize} sources busy, interrupting oldest sound");
        }

        ResetSourceSettings(audioSourcePool[oldestIndex]);
        return audioSourcePool[oldestIndex];
    }

    /// <summary>
    /// Find the AudioSource that has been playing the longest
    /// </summary>
    private int GetOldestSourceIndex()
    {
        int oldestIndex = 0;
        float oldestTime = sourceStartTimes[0];

        for (int i = 1; i < poolSize; i++)
        {
            if (sourceStartTimes[i] < oldestTime)
            {
                oldestTime = sourceStartTimes[i];
                oldestIndex = i;
            }
        }

        return oldestIndex;
    }

    /// <summary>
    /// Reset AudioSource to default 2D settings
    /// </summary>
    private void ResetSourceSettings(AudioSource source)
    {
        source.spatialBlend = 0f; // 2D by default
        source.transform.position = transform.position; // Reset to manager position
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Stop all currently playing sounds
    /// </summary>
    public void StopAllSounds()
    {
        foreach (AudioSource source in audioSourcePool)
        {
            source.Stop();
        }

        if (debugMode)
        {
            Debug.Log("SoundManager: Stopped all sounds");
        }
    }

    /// <summary>
    /// Get the number of currently playing sounds
    /// </summary>
    public int GetPlayingSoundCount()
    {
        int count = 0;
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying) count++;
        }
        return count;
    }

    /// <summary>
    /// Check if the pool is at capacity
    /// </summary>
    public bool IsPoolFull()
    {
        return GetPlayingSoundCount() >= poolSize;
    }

    #endregion

    #region Debug

    private void OnGUI()
    {
        if (debugMode)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Box($"SoundManager Debug");
            GUILayout.Label($"Pool Size: {poolSize}");
            GUILayout.Label($"Playing: {GetPlayingSoundCount()}/{poolSize}");
            GUILayout.Label($"Next Index: {currentPoolIndex}");
            GUILayout.EndArea();
        }
    }

    #endregion
}
