using UnityEngine;
using UnityEngine.AI;
// using BubblePop.Enemy.States; // Namespace for states

/// <summary>
/// Enemy AI Coordinator
/// Manages state machine and coordinates between component systems
/// Core rule: Enemy cannot chase without a weapon - must seek weapon first
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyVisionSystem))]
[RequireComponent(typeof(EnemyMovementController))]
[RequireComponent(typeof(EnemySoundDetector))]
[RequireComponent(typeof(EnemyStunController))]
[RequireComponent(typeof(EnemySoundInvestigator))]
[RequireComponent(typeof(EnemyWeaponController))]
[RequireComponent(typeof(EnemyItemSeeker))]
[RequireComponent(typeof(EnemyProximityDetector))]
public class EnemyAI : MonoBehaviour, ISpeedBoostable
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing all enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Target Settings")]
    [Tooltip("Auto-find player on start (Overrides SO if true)")]
    public bool autoFindPlayerOverride = false;

    [Header("Chase Settings")]
    [Tooltip("Once spotted, chase forever until too far (Overrides SO if true)")]
    public bool persistentChaseOverride = false;

    public bool PersistentChase => persistentChaseOverride || (enemyData != null && enemyData.persistentChase);

    [Header("Debug")]
    [Tooltip("Show state changes in console")]
    public bool debugMode = false;
    public AIStateBase CurrentState => _currentState;

    // Component references (public getters for states)
    public EnemyHealth Health { get; private set; }
    public EnemyVisionSystem VisionSystem { get; private set; }
    public EnemyMovementController MovementController { get; private set; }
    public EnemySoundDetector SoundDetector { get; private set; }
    public EnemyStunController StunController { get; private set; }
    public EnemySoundInvestigator SoundInvestigator { get; private set; }
    public EnemyWeaponController WeaponController { get; private set; }
    public EnemyItemSeeker ItemSeeker { get; private set; }
    public EnemyProximityDetector ProximityDetector { get; private set; }
    public BounceController BounceController { get; private set; }
    public NavMeshAgent Agent { get; private set; }

    // State pattern instances
    private AIStateBase _currentState;
    public PatrolState PatrolStateInstance { get; private set; }
    public ChaseState ChaseStateInstance { get; private set; }
    public SearchState SearchStateInstance { get; private set; }
    public SeekItemState SeekItemStateInstance { get; private set; }
    public InvestigateState InvestigateStateInstance { get; private set; }
    public StunnedState StunnedStateInstance { get; private set; }
    public WaitState WaitStateInstance { get; private set; }

    // Target tracking (public for states)
    public Transform Target { get; private set; }
    public bool HasDetectedTarget { get; private set; }
    public float LastSeenTime { get; set; }
    public Vector3 LastKnownTargetPosition { get; set; }
    public bool ProximityDetected { get; private set; }
    public bool DebugMode => debugMode; // Public getter for debug mode

    // Speed boost
    private bool isSpeedBoosted = false;
    private float speedBoostMultiplier = 1f;
    private float currentSpeedBoostFadeTime = 0f;
    private float initialFadeDuration = 0f;
    private bool isFadingBoost = false;

    #region Initialization

    private void Awake()
    {
        // Get component references
        Health = GetComponent<EnemyHealth>();
        VisionSystem = GetComponent<EnemyVisionSystem>();
        MovementController = GetComponent<EnemyMovementController>();
        SoundDetector = GetComponent<EnemySoundDetector>();
        StunController = GetComponent<EnemyStunController>();
        SoundInvestigator = GetComponent<EnemySoundInvestigator>();
        WeaponController = GetComponent<EnemyWeaponController>();
        ItemSeeker = GetComponent<EnemyItemSeeker>();
        ProximityDetector = GetComponent<EnemyProximityDetector>();
        BounceController = GetComponent<BounceController>();
        Agent = GetComponent<NavMeshAgent>();

        // Initialize states
        PatrolStateInstance = new PatrolState(this);
        ChaseStateInstance = new ChaseState(this);
        SearchStateInstance = new SearchState(this);
        SeekItemStateInstance = new SeekItemState(this);
        InvestigateStateInstance = new InvestigateState(this);
        StunnedStateInstance = new StunnedState(this);
        WaitStateInstance = new WaitState(this);
    }

    private void Start()
    {
        // Subscribe to events
        Health.OnEnemyDeath += HandleDeath;
        ProximityDetector.OnPlayerDetected += HandleProximityDetection;
        SoundInvestigator.OnSoundDetected += HandleSoundDetected; // New event for sound

        // Find target
        bool shouldAutoFind = autoFindPlayerOverride || (enemyData != null && enemyData.autoFindPlayer);
        if (shouldAutoFind)
        {
            FindTarget();
        }

        // Initialize detector
        ProximityDetector.Initialize(enemyData, Target);

        // Set initial state
        if (WeaponController.HasWeapon())
        {
            TransitionToState(PatrolStateInstance);
        }
        else
        {
            TransitionToState(SeekItemStateInstance);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (Health != null)
            Health.OnEnemyDeath -= HandleDeath;
        if (ProximityDetector != null)
            ProximityDetector.OnPlayerDetected -= HandleProximityDetection;
        if (SoundInvestigator != null)
            SoundInvestigator.OnSoundDetected -= HandleSoundDetected; // Unsubscribe new event
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        // Don't update if dead
        if (Health.IsDead) return;

        // Don't update if bouncing
        if (BounceController != null && BounceController.IsBouncing) return;

        // Check for stun trigger
        if (StunController.IsStunTriggered && _currentState != StunnedStateInstance)
        {
            TransitionToState(StunnedStateInstance);
            return;
        }

        // Find target if lost
        bool shouldAutoFind = autoFindPlayerOverride || (enemyData != null && enemyData.autoFindPlayer);
        if (Target == null && shouldAutoFind)
        {
            FindTarget();
        }

        _currentState?.UpdateState(); // Update the current active state

        UpdateSpeedBoostFade();
    }

    private void UpdateSpeedBoostFade()
    {
        if (isFadingBoost)
        {
            currentSpeedBoostFadeTime -= Time.deltaTime;

            if (currentSpeedBoostFadeTime <= 0)
            {
                // Fade complete
                isFadingBoost = false;
                isSpeedBoosted = false;
                speedBoostMultiplier = 1f;
                MovementController.RemoveSpeedBoost();
            }
            else
            {
                // Calculate interpolated multiplier
                // Lerp from stored multiplier to 1
                float t = currentSpeedBoostFadeTime / initialFadeDuration;
                float currentMultiplier = Mathf.Lerp(1f, speedBoostMultiplier, t);
                MovementController.ApplySpeedBoost(currentMultiplier);
            }
        }
    }

    #endregion

    #region State Transitions (Managed by State Classes)

    public void TransitionToState(AIStateBase newState)
    {
        if (newState == _currentState) return;

        _currentState?.ExitState(); // Call Exit on previous state

        if (debugMode)
            Debug.Log($"{gameObject.name}: {(_currentState != null ? _currentState.GetType().Name : "None")} -> {newState.GetType().Name}");

        _currentState = newState; // Set new current state
        _currentState.EnterState(); // Call Enter on new state
    }

    public void TriggerWait(float duration)
    {
        WaitStateInstance.SetDuration(duration);
        TransitionToState(WaitStateInstance);
    }

    #endregion

    #region Movement/Vision Updates (Still managed by EnemyAI for now)

    // UpdateVisionDirection removed - logic moved to AIStateBase.UpdateVision()

    #endregion

    #region Event Handlers

    private void HandleProximityDetection(Transform detectedTarget)
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Proximity detected player at {detectedTarget.position}");

        ProximityDetected = true;
        SetHasDetectedTarget(true); // Set hasDetectedTarget via public setter
        SetTarget(detectedTarget); // Set target via public setter
        LastKnownTargetPosition = detectedTarget.position;

        // This event might trigger a state change, e.g., from Patrol to Chase/SeekItem
        // The current state's UpdateState will handle the transition
    }

    private void HandleSoundDetected(Vector2 soundPosition)
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Sound detected at {soundPosition}");

        // Only transition to investigate if not chasing or stunned
        if (_currentState != ChaseStateInstance && _currentState != StunnedStateInstance)
        {
            // Don't investigate if no weapon and player was detected (still relevant for initial investigation trigger)
            if (!WeaponController.HasWeapon() && HasDetectedTarget) return;

            InvestigateStateInstance.SetInvestigationPosition(soundPosition);
            TransitionToState(InvestigateStateInstance);
        }
    }

    private void HandleDeath()
    {
        // Drop all items before disabling
        if (WeaponController != null)
        {
            WeaponController.DropAllItems();
        }

        this.enabled = false;
        // Optionally, transition to a "Dead" state here.
    }

    #endregion

    #region Target Management

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SetTarget(player.transform);
            return;
        }

        PlayerControls playerScript = FindFirstObjectByType<PlayerControls>();
        if (playerScript != null)
        {
            SetTarget(playerScript.transform);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        Target = newTarget;
        VisionSystem.SetTarget(newTarget);
        if (ProximityDetector != null) ProximityDetector.SetTarget(newTarget);
        // Reset hasDetectedTarget if a new target is set or target is lost
        if (newTarget == null) SetHasDetectedTarget(false);
    }

    public void SetHasDetectedTarget(bool detected)
    {
        HasDetectedTarget = detected;
        if (!detected) ProximityDetected = false; // Reset proximity detected if target is lost
    }

    #endregion

    #region ISpeedBoostable Implementation (remains as is)

    public void ApplySpeedBoost(float multiplier)
    {
        // If we were fading, stop fading and apply new boost
        bool wasFading = isFadingBoost;
        isFadingBoost = false;

        if (!isSpeedBoosted || wasFading || multiplier > speedBoostMultiplier)
        {
            speedBoostMultiplier = multiplier;
            isSpeedBoosted = true;
            MovementController.ApplySpeedBoost(multiplier);
        }
    }

    public void RemoveSpeedBoost(float fadeDuration = 0f)
    {
        if (isSpeedBoosted)
        {
            if (fadeDuration > 0f)
            {
                // Start fading
                isFadingBoost = true;
                initialFadeDuration = fadeDuration;
                currentSpeedBoostFadeTime = fadeDuration;
            }
            else
            {
                // Instant removal
                speedBoostMultiplier = 1f;
                isSpeedBoosted = false;
                isFadingBoost = false;
                MovementController.RemoveSpeedBoost();
            }
        }
    }

    public bool IsSpeedBoosted => isSpeedBoosted;

    #endregion

    #region Public API / Helper Methods for States

    public bool IsPlayerDetected()
    {
        return (Target != null && VisionSystem.CanSeeTarget()) || ProximityDetected;
    }

    public bool IsSoundHeard()
    {
        return SoundInvestigator.HasDetectedSoundStatus;
    }

    public bool HasWeapon() => WeaponController.HasWeapon();
    public Vector3 GetVisionDirection() => VisionSystem.GetVisionDirection();
    public void SetVisionDirection(Vector3 direction) => VisionSystem.SetVisionDirection(direction);
    public bool IsMoving() => MovementController.IsMoving();
    public float DistanceToTarget() => Target != null ? Vector3.Distance(transform.position, Target.position) : float.MaxValue;

    public void ResetDetection()
    {
        SetHasDetectedTarget(false);
        ProximityDetector.ResetDetection();
    }

    /// <summary>
    /// Called by EnemyItemSeeker when it picks up a weapon
    /// </summary>
    public void OnWeaponAcquired()
    {
        // The current state's UpdateState will handle the transition based on weapon acquired.
        // For example, SeekItemState will transition out if a weapon is acquired.
    }

    #endregion
}
