# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BubblePop is a Unity 6 (6000.2.12f1) 2D top-down action game featuring a physics-based combat system with "bounce mechanics" and AI-driven enemies. The game uses Unity's Universal Render Pipeline (URP) and New Input System.

## Build & Development Commands

### Opening the Project
- Open the project in Unity 6000.2.12f1 or later
- Project uses `.slnx` solution format (BubblePop.slnx)

### Building
- Build from Unity Editor: `File > Build Settings > Build`
- No custom build scripts currently configured

### Testing
- Play mode testing: Open scene files in `Assets/Scenes/` (Ep1, Ep2, SampleScene)
- Use Unity Test Framework: `Window > General > Test Runner`
- Tests configured via `com.unity.test-framework` package

## Architecture & Code Structure

### Core Design Patterns

**State Machine Pattern for AI**
- `EnemyAI` (Assets/Scripts/Enemy/EnemyAI.cs) acts as the state machine coordinator
- All AI states inherit from `AIStateBase` (Assets/Scripts/Enemy/States/AIStateBase.cs)
- States: PatrolState, ChaseState, SearchState, SeekItemState, InvestigateState, StunnedState, WaitState
- Each state implements: `EnterState()`, `UpdateState()`, `ExitState()`
- States access enemy components through the `EnemyAI` context reference

**Interface-Based Systems**
Core interfaces defined in `Assets/Scripts/Core/Interfaces.cs`:
- `IItemHolder`: Entities that can hold weapons (PlayerControls, EnemyWeaponController)
- `IDamageable`: Entities that can take damage (EnemyHealth, PlayerControls)
- `ISpeedBoostable`: Entities that can receive speed boosts (EnemyAI, PlayerControls)

**ScriptableObject Data Architecture**
All configuration data is stored in ScriptableObjects (Assets/Scripts/ScriptableObjects/):
- `EnemyDataSO`: All enemy AI configuration (vision, movement speeds, behavior)
- `WeaponDataSO`: Base weapon configuration
- `MeleeWeaponDataSO`: Melee-specific data
- `RangedWeaponDataSO`: Ranged weapon and projectile data
- `BounceSettingsSO`: Physics bounce configuration
- `SpeedPuddleDataSO`: Speed boost zone configuration

### Component-Based Enemy AI

`EnemyAI` coordinates multiple specialized components:
- `EnemyHealth`: Health management and death handling
- `EnemyVisionSystem`: Cone-based vision detection with raycasts
- `EnemyMovementController`: NavMesh-based movement with speed multipliers
- `EnemySoundDetector`: Listens for sound events via GameEvents
- `EnemySoundInvestigator`: Handles sound investigation behavior
- `EnemyStunController`: Manages stun state from door slams
- `EnemyWeaponController`: Weapon inventory and usage
- `EnemyItemSeeker`: Item detection and pathfinding to items
- `EnemyProximityDetector`: Close-range player detection
- `BounceController`: Physics-based bounce mechanics

All components are accessed through public properties on `EnemyAI` for state classes to use.

### Critical Enemy AI Rule
**Enemies CANNOT chase without a weapon** - they must seek a weapon first via `SeekItemState`. This is enforced in state transitions throughout the AI system.

### Input System

**Singleton Pattern**: `InputManager` (Assets/Scripts/Input/InputManager.cs)
- Wraps Unity's New Input System (GameControls.inputactions)
- Provides event-based input: `OnAttackPressed`, `OnThrowPressed`, `OnPickupPressed`
- Continuous values: `MoveInput`, `MousePosition`, `MouseWorldPosition`
- Subscribe to events in `Start()`, unsubscribe in `OnDestroy()` to prevent memory leaks

### Event System

**Decoupled Event Architecture**: `GameEvents` (Assets/Scripts/GameEvents.cs)
- Static event system to decouple game systems
- Key events: `OnPlayerDied`, `OnSoundEmitted`, `OnEnemyDied`, `OnItemPickedUp`, etc.
- **CRITICAL**: Always unsubscribe in `OnDisable` to prevent memory leaks
- Call `GameEvents.ClearAllEvents()` when changing scenes or restarting levels

### Sound System

**Centralized Sound Management**: `SoundManager` (Assets/Scripts/Core/SoundManager.cs)
- Singleton pattern with AudioSource pooling (configurable pool size, default 10)
- Handles all audio playback in the game
- **Two main methods**:
  - `PlaySound(clip, volume)`: Simple sound playback
  - `PlaySoundWithAlert(clip, position, alertRadius, volume)`: Plays sound + broadcasts `GameEvents.OnSoundEmitted` for enemy detection
- **AudioSource Pool**: Automatically cycles through pooled sources, interrupts oldest if all busy
- **3D Sound Support**: `PlaySound3D()` for positional audio with distance falloff
- **Debug Mode**: Enable in Inspector to see pool usage and sound playback logs

**Usage Examples**:
```csharp
// Simple sound (melee attack, UI click)
SoundManager.Instance.PlaySound(swingSound, 0.8f);

// Sound that alerts enemies (gunshot)
SoundManager.Instance.PlaySoundWithAlert(gunshotSound, transform.position, 20f, 1f);

// 3D positional sound
SoundManager.Instance.PlaySound3D(ambientSound, worldPosition, 0.7f, 1f, 10f);
```

**Integration Points**:
- `RangedWeapon`: Uses `PlaySoundWithAlert()` for gunshots to alert nearby enemies
- `MeleeWeapon`: Uses `PlaySound()` for swing sounds
- `EnemyHealth`: Uses `PlaySound()` for death sounds (random from array)
- `Weapon.Throw()`: Uses `PlaySound()` for throw sound effects
- `SpeedPuddle`: Uses `PlaySound()` for boost activation when entity enters zone
- `BounceController`: Uses `PlaySound()` for wall bounce impacts
- `Weapon.OnWeaponBroken()`: Uses `PlaySound()` for weapon break sound
- `Projectile`: Uses `PlaySound()` for wall impact (nail hitting wall)

### Damage & Combat System

**DamageInfo Struct** (Assets/Scripts/Core/DamageInfo.cs):
- Unified damage handling across all damage sources
- Contains: damage amount, source team, damage type, knockback direction

**DamageType Enum** (Assets/Scripts/Core/Enums.cs):
- `Piercing`: Instant kill damage (bullets, stabbing)
- `Blunt`: Causes bounce/knockback (baseball bat, projectiles)

**Bounce Mechanics**:
- First blunt hit → triggers `BounceController.StartBounce()`
- Second hit while bouncing → instant kill
- Bouncing entities use physics (Rigidbody2D), disables NavMeshAgent temporarily
- Bounce parameters controlled by `BounceSettingsSO`

### Weapon System

**Inheritance Hierarchy**:
```
PickupableItem (base)
  └─ Weapon (abstract)
       ├─ MeleeWeapon
       └─ RangedWeapon
```

**Key Weapon Behavior**:
- Durability system: weapons break after N uses
- Throwing system: `Weapon.Throw()` adds `ThrownItem` component dynamically
- Visual states: dropped sprite, held sprite, depleted sprite (from WeaponDataSO)
- `ItemHoldType` determines hold position (Melee vs Ranged)
- Weapons are configured via ScriptableObjects, NOT in-script fields

### NavMesh for 2D

This project uses **NavMeshComponents** (Assets/NavMeshComponents/) for 2D pathfinding:
- NavMesh is generated in the Unity Editor using NavMeshSurface components
- Enemies use `NavMeshAgent` with 2D constraints
- Custom 2D extensions: `AgentRotate2d`, `NavMeshBuilder2d`, `CollectSources2d`

## Key Unity Packages

- **Input System** (1.14.2): New Input System for all player/game controls
- **AI Navigation** (2.0.9): NavMesh pathfinding (with custom 2D extensions)
- **URP** (17.2.0): Universal Render Pipeline for rendering
- **Cinemachine** (3.1.5): Camera controls (used with `CameraMFollow`, `MousePanCameraExtension`)
- **2D Packages**: Animation, Tilemap, Sprite utilities

## Important Code Conventions

### Team-Based Damage
Always check team before applying damage:
```csharp
if (damageInfo.sourceTeam == Team.Player) return; // Friendly fire check
```

Teams defined in `Team` enum: Player, Enemy, Neutral

### AI State Transitions
States handle their own transitions in `UpdateState()`:
```csharp
// Example from ChaseState
if (!HasWeapon()) {
    enemyAI.TransitionToState(enemyAI.SeekItemStateInstance);
}
```

### Speed Boosts
Speed boosts support fade-out:
```csharp
ApplySpeedBoost(float multiplier);           // Apply boost
RemoveSpeedBoost(float fadeDuration = 0f);   // Remove with optional fade
```

### Component Access Pattern
Enemy states access components through `EnemyAI`:
```csharp
// In state classes
movementController.SetDestination(position);  // Accessed via base class
enemyAI.WeaponController.HasWeapon();         // Accessed via context
```

## Level Management System

**Event-Based Level Progression**: `LevelManager` (Assets/Scripts/Core/LevelManager.cs)
- Singleton pattern - one per scene
- Tracks alive enemies via event registration (fully decoupled)
- Activates exit area when all enemies cleared
- Handles player death and level restart (always reloads current scene)

**How It Works**:
1. Enemies register themselves on spawn via `GameEvents.OnEnemySpawned`
2. LevelManager tracks alive enemy count
3. When last enemy dies → activates exit area
4. Player enters exit → loads next scene

**Event-Based Registration**:
```csharp
// In EnemyAI.Start()
GameEvents.TriggerEnemySpawned(gameObject);  // Auto-registers with LevelManager

// In EnemyHealth.Die()
GameEvents.TriggerEnemyDied(gameObject);     // Auto-unregisters from LevelManager
```

**Setup in Unity**:
1. Add `LevelManager` component to scene
2. Assign `exitArea` GameObject (will be activated when enemies cleared)
3. Create exit area with `LevelExitTrigger` component + 2D trigger collider
4. Assign next scene name to `LevelExitTrigger`

**LevelExitTrigger**:
- Attach to exit area GameObject
- Requires 2D Collider (set as Trigger)
- Loads next scene when player enters
- Automatically calls `GameEvents.ClearAllEvents()` before scene load

**Death UI System**: `DeathUI` (Assets/Scripts/UI/DeathUI.cs)
- Event-driven UI for player death
- Subscribes to `GameEvents.OnPlayerDied`
- Shows death panel with fade-in animation
- Restart button calls `LevelManager.RestartLevel()` (reloads current scene)
- Quit button calls `LevelManager.QuitGame()`

**Setup Death UI**:
1. Add `DeathUI` component to Canvas in scene
2. Assign death panel GameObject (should have CanvasGroup for fade)
3. Assign restart button and optional quit button
4. Configure fade duration and show delay

**Debug Mode**:
- Enable in LevelManager to see enemy count on-screen
- Shows alive enemies, exit status, and force activate button

## Scene Structure

Main scenes located in `Assets/Scenes/`:
- `SampleScene.unity`: Initial test scene
- `Ep1.unity`: Episode 1 level
- `Ep2.unity`: Episode 2 level

## Debugging

### Enemy AI Debug Mode
Enable `debugMode` on `EnemyAI` component to see state transitions in console:
```
EnemyName: PatrolState -> ChaseState
```

### Vision Visualization
`EnemyDebugVisualizer` (Assets/Scripts/Enemy/EnemyDebugVisualizer.cs) provides Gizmos for:
- Vision cone
- Detection ranges
- Target positions

## File Organization

Scripts are organized into logical folders for easy navigation:

```
Assets/Scripts/
├── CLAUDE.md                     # This file - comprehensive documentation
├── DOCUMENTATION.md              # Quick reference for all systems
├── GameEvents.cs                 # Core event system (root level)
├── Core/                         # Core systems & utilities
│   ├── LevelManager.cs           # Level progression & enemy tracking
│   ├── LevelExitTrigger.cs       # Scene transition trigger
│   ├── SoundManager.cs           # Centralized audio system
│   ├── SoundManagerTester.cs     # Audio testing helper
│   ├── BounceController.cs       # Physics bounce mechanics
│   ├── SpeedPuddle.cs            # Speed boost zones
│   ├── DamageInfo.cs             # Damage data struct
│   ├── Enums.cs                  # Shared enums (Team, DamageType, etc.)
│   └── Interfaces.cs             # Core interfaces (IItemHolder, IDamageable, etc.)
├── Enemy/                        # Enemy AI system
│   ├── EnemyAI.cs                # State machine coordinator
│   ├── EnemyHealth.cs            # Health & death handling
│   ├── EnemyVisionSystem.cs      # Cone-based vision
│   ├── EnemyMovementController.cs
│   ├── EnemyWeaponController.cs
│   ├── [Other Enemy Components]
│   └── States/                   # AI State classes
│       ├── AIStateBase.cs
│       ├── PatrolState.cs
│       ├── ChaseState.cs
│       ├── SearchState.cs
│       ├── SeekItemState.cs
│       ├── InvestigateState.cs
│       ├── StunnedState.cs
│       └── WaitState.cs
├── Weapons/                      # Weapon system
│   ├── Weapon.cs                 # Abstract base class
│   ├── MeleeWeapon.cs
│   ├── RangedWeapon.cs
│   └── Projectile.cs
├── Player/                       # Player-related scripts
│   └── PlayerControls.cs
├── Items/                        # Pickup item system
│   ├── PickupableItem.cs         # Base class for pickupable items
│   └── ThrownItem.cs             # Thrown weapon physics
├── UI/                           # UI components
│   └── DeathUI.cs                # Death screen & restart
├── Camera/                       # Camera system
│   └── MousePanCameraExtension.cs
├── Input/                        # Input management
│   ├── InputManager.cs           # New Input System wrapper
│   └── GameControls.cs           # Generated input actions class
├── ScriptableObjects/            # Data configuration classes
│   ├── EnemyDataSO.cs
│   ├── WeaponDataSO.cs
│   ├── MeleeWeaponDataSO.cs
│   ├── RangedWeaponDataSO.cs
│   ├── ProjectileDataSO.cs
│   ├── BounceSettingsSO.cs
│   └── SpeedPuddleDataSO.cs
└── Tests/                        # Debug & testing helpers
    └── DeathSystemTester.cs
```

**Key Locations**:
- **CLAUDE.md**: Comprehensive documentation for AI instances (this file)
- **DOCUMENTATION.md**: Quick reference at Scripts root
- **Core/**: Game management and systems used throughout the game
- **Enemy/**: Complete AI system with state machine
- **Player/**: Player-specific functionality
- **Items/**: Pickup and item interaction system
- **UI/**: All UI-related scripts
- **Tests/**: Debug helpers (safe to ignore in builds)

## Common Gotchas

1. **NavMeshAgent in 2D**: Requires special setup
   - Set `updateRotation = false` and `updateUpAxis = false`
   - Use custom 2D rotation components like `AgentRotate2d`

2. **Bounce Physics Conflicts**:
   - BounceController disables NavMeshAgent during bounce
   - Check `BounceController.IsBouncing` before processing movement/AI updates

3. **Input System Subscription**:
   - Subscribe in `Start()` or `OnEnable()`
   - **Always** unsubscribe in `OnDestroy()` or `OnDisable()`

4. **Weapon Hold Type**:
   - `ItemHoldType` is now stored in `WeaponDataSO`, not in Weapon scripts
   - Access via `weaponData.holdType` or `Weapon.HoldType` property

5. **ThrownItem Component**:
   - Dynamically added when weapon is thrown
   - Handles collision damage and physics while airborne
   - Auto-destroys or reverts to PickupableItem on impact

6. **State Machine Updates**:
   - States only update when `EnemyAI.Update()` runs
   - Dead or bouncing enemies skip state updates

## Adding New Features

### Adding a New Enemy AI State
1. Create new class inheriting from `AIStateBase`
2. Implement `EnterState()`, `UpdateState()`, `ExitState()`
3. Add state instance property to `EnemyAI`
4. Initialize state in `EnemyAI.Awake()`
5. Add transition logic in relevant states' `UpdateState()`

### Adding a New Weapon Type
1. Create ScriptableObject inheriting from `WeaponDataSO`
2. Create script inheriting from `Weapon`
3. Override `PerformAttack()` with custom logic
4. Create ScriptableObject asset in `Assets/ScriptableObjects/`
5. Assign to weapon prefab in `Assets/Prefabs/`

### Adding New Game Events
1. Add event declaration in `GameEvents.cs`: `public static event Action<T> OnEventName;`
2. Add trigger method: `public static void TriggerEventName(T data)`
3. Remember to clear event in `ClearAllEvents()`
4. Subscribe/unsubscribe properly to prevent memory leaks
