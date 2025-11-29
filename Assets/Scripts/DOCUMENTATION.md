# BubblePop - Systems Documentation

**Project**: Hotline Miami-style 2D Unity Game  
**Unity Version**: 6000.2.12f1  
**Last Updated**: 2025-11-29

Quick reference for all implemented systems in BubblePop.

---

## Sound System (SoundManager)

**Location**: `Assets/Scripts/Core/SoundManager.cs`

### Usage

```csharp
// Simple sound
SoundManager.Instance.PlaySound(clip, volume);

// Sound with enemy alert
SoundManager.Instance.PlaySoundWithAlert(clip, position, alertRadius, volume);
```

### Integrated In

- RangedWeapon (gunshots with alert)
- MeleeWeapon (swing sounds)
- Weapon break sounds
- SpeedPuddle boost sounds
- BounceController wall impacts
- Projectile wall hits
- Enemy death sounds

---

## Level Management System

### Components

**LevelManager** - Tracks enemies, activates exit

- Always restarts current scene
- Debug mode shows enemy count
- Force activate exit button

**LevelExitTrigger** - Loads next scene

- Requires trigger collider
- Set nextSceneName in Inspector

**DeathUI** - Shows death screen

- Restart button reloads current scene
- Fade-in animation support

### Event Flow

```
Enemy spawns → GameEvents.OnEnemySpawned → LevelManager tracks
Enemy dies → GameEvents.OnEnemyDied → LevelManager removes
Count = 0 → Exit activates → Player enters → Next scene loads
```

---

## File Organization

```
Scripts/
├── CLAUDE.md (comprehensive documentation)
├── DOCUMENTATION.md (this file - quick reference)
├── GameEvents.cs (core event system)
├── Core/ (LevelManager, LevelExitTrigger, SoundManager, BounceController, SpeedPuddle, etc.)
├── Enemy/ (AI components + States/)
├── Weapons/ (Weapon classes + Projectile)
├── Player/ (PlayerControls)
├── Items/ (PickupableItem, ThrownItem)
├── UI/ (DeathUI)
├── Camera/ (MousePanCameraExtension)
├── Input/ (InputManager, GameControls)
├── ScriptableObjects/ (All data configuration classes)
└── Tests/ (Debug helpers)
```

---

## Input System (InputManager)

**Location**: `Assets/Scripts/Input/InputManager.cs`

### Overview

Wraps Unity's New Input System with event-based architecture. Singleton pattern provides centralized input access.

### Event Subscription

```csharp
// In Start()
InputManager.Instance.OnAttackPressed += HandleAttack;
InputManager.Instance.OnThrowPressed += HandleThrow;

// In OnDestroy()
InputManager.Instance.OnAttackPressed -= HandleAttack;
InputManager.Instance.OnThrowPressed -= HandleThrow;
```

### Continuous Input Values

```csharp
Vector2 moveInput = InputManager.Instance.MoveInput;
Vector2 mousePos = InputManager.Instance.MousePosition;      // Screen space
Vector2 worldPos = InputManager.Instance.MouseWorldPosition; // World space
```

### Available Events

- `OnAttackPressed` - Left mouse button
- `OnThrowPressed` - Right mouse button
- `OnPickupPressed` - E key
- `OnRestartPressed` - R key

### Adding New Inputs

1. Open `Assets/Settings/GameControls.inputactions`
2. Add action in Input Actions window
3. Generate C# class (Inspector button)
4. Add event/property in InputManager
5. Wire up in Enable/Disable methods

### Input Actions Asset

Located at `Assets/Settings/GameControls.inputactions`

- Action Map: "Player"
- Control Scheme: Keyboard & Mouse
- Generated class: `GameControls.cs`

---

## Quick Setup

### New Level

1. Add LevelManager to scene
2. Create exit with LevelExitTrigger + trigger collider
3. Set nextSceneName
4. Assign exit to LevelManager

### New Sound

1. Add AudioClip field to ScriptableObject
2. Call SoundManager.Instance.PlaySound()
3. Assign clip in Inspector

### New Input

1. Edit GameControls.inputactions asset
2. Generate C# class from Inspector
3. Add to InputManager (event + enable/disable)
4. Subscribe in your script (Start/OnDestroy)

---

For detailed documentation, see `/Assets/Scripts/CLAUDE.md`
