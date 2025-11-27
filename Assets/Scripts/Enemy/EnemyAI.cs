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
[RequireComponent(typeof(EnemyItemHolder))]
[RequireComponent(typeof(EnemyItemSeeker))]
[RequireComponent(typeof(EnemyProximityDetector))]
public class EnemyAI : MonoBehaviour, ISpeedBoostable
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing all enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Target Settings")]
    [Tooltip("Auto-find player on start")]
    public bool autoFindPlayer = true;

    [Header("Chase Settings")]
    [Tooltip("Once spotted, chase forever until too far")]
    public bool persistentChase = true;
    public bool PersistentChase => persistentChase;

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
    public EnemyItemHolder ItemHolder { get; private set; }
    public EnemyItemSeeker ItemSeeker { get; private set; }
    public EnemyProximityDetector ProximityDetector { get; private set; }
    public NavMeshAgent Agent { get; private set; }

    // State pattern instances
    private AIStateBase _currentState;
    public PatrolState PatrolStateInstance { get; private set; }
    public ChaseState ChaseStateInstance { get; private set; }
    public SearchState SearchStateInstance { get; private set; }
    public SeekItemState SeekItemStateInstance { get; private set; }
    public InvestigateState InvestigateStateInstance { get; private set; }
    public StunnedState StunnedStateInstance { get; private set; }

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
        ItemHolder = GetComponent<EnemyItemHolder>();
        ItemSeeker = GetComponent<EnemyItemSeeker>();
        ProximityDetector = GetComponent<EnemyProximityDetector>();
        Agent = GetComponent<NavMeshAgent>();

        // Initialize states
        PatrolStateInstance = new PatrolState(this);
        ChaseStateInstance = new ChaseState(this);
        SearchStateInstance = new SearchState(this);
        SeekItemStateInstance = new SeekItemState(this);
        InvestigateStateInstance = new InvestigateState(this);
        StunnedStateInstance = new StunnedState(this);
    }

    private void Start()
    {
        // Subscribe to events
        StunController.OnStunEnded += HandleStunEnded;
        Health.OnEnemyDeath += HandleDeath;
        ProximityDetector.OnPlayerDetected += HandleProximityDetection;
        SoundInvestigator.OnSoundDetected += HandleSoundDetected; // New event for sound

        // Find target
        if (autoFindPlayer)
        {
            FindTarget();
        }

        // Initialize detector
        ProximityDetector.Initialize(enemyData, Target);

        // Set initial state
        if (ItemHolder.HasWeapon())
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
        if (StunController != null)
            StunController.OnStunEnded -= HandleStunEnded;
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

        // Check for stun state outside main state machine as it has higher priority
        if (StunController.IsStunned() && _currentState != StunnedStateInstance)
        {
            TransitionToState(StunnedStateInstance);
            return;
        }

        // Find target if lost
        if (Target == null && autoFindPlayer)
        {
            FindTarget();
        }

        _currentState?.UpdateState(); // Update the current active state

        // Update vision direction based on movement
        UpdateVisionDirection();
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

    #endregion

    #region Movement/Vision Updates (Still managed by EnemyAI for now)

    private void UpdateVisionDirection()
    {
        // Check if waiting at waypoint
        if (MovementController.IsWaitingAtWaypoint)
        {
            Vector3 waypointDirection = MovementController.GetCurrentWaypointDirection();
            if (enemyData != null)
            {
                VisionSystem.RotateVisionTowards(waypointDirection, enemyData.rotationSpeed);
            }
            return;
        }

        // Update vision based on movement
        if (MovementController.IsMoving())
        {
            Vector3 moveDirection = MovementController.GetMovementDirection();
            if (moveDirection.magnitude > 0.1f && enemyData != null)
            {
                VisionSystem.RotateVisionTowards(moveDirection, enemyData.rotationSpeed);
            }
        }
    }

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
            if (!ItemHolder.HasWeapon() && HasDetectedTarget) return;

            InvestigateStateInstance.SetInvestigationPosition(soundPosition);
            TransitionToState(InvestigateStateInstance);
        }
    }

    private void HandleStunEnded()
    {
        if (debugMode)
            Debug.Log($"{gameObject.name}: Stun ended");

        // The stunned state's Update will handle the transition logic after stun ends.
        // We ensure that we transition out of the Stunned state if we are currently in it.
        if (_currentState == StunnedStateInstance)
        {
             // This might be redundant as StunnedState.UpdateState() handles it.
             // However, a direct event can force a transition if needed.
             // For simplicity, let StunnedState.UpdateState handle it.
        }
    }

    private void HandleDeath()
    {
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
        if (!isSpeedBoosted)
        {
            speedBoostMultiplier = multiplier;
            isSpeedBoosted = true;
            MovementController.ApplySpeedBoost(multiplier);
        }
    }

    public void RemoveSpeedBoost()
    {
        if (isSpeedBoosted)
        {
            speedBoostMultiplier = 1f;
            isSpeedBoosted = false;
            MovementController.RemoveSpeedBoost();
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

    public bool HasWeapon() => ItemHolder.HasWeapon();
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
