# New Input System - Setup Guide

## What Was Changed

Your project has been migrated from Unity's old Input Manager to the new Input System using a clean, modular wrapper approach.

### Files Created:
1. **GameControls.inputactions** - Input Actions asset defining all game inputs
2. **InputManager.cs** - Singleton wrapper providing easy-to-use input events
3. **INPUT_SYSTEM_SETUP.md** - This guide

### Files Modified:
1. **PlayerControls.cs** - Now uses InputManager events instead of Input.GetKey
2. **MousePanCameraExtension.cs** - Now uses InputManager for mouse position

---

## Setup Steps (IMPORTANT!)

### Step 1: Generate C# Class from Input Actions

1. In Unity, navigate to `Assets/Settings/GameControls.inputactions`
2. Select the file in the Project window
3. In the Inspector, check the box: **"Generate C# Class"**
4. Click **"Apply"**
5. This will generate `GameControls.cs` next to the .inputactions file

**Why?** The InputManager needs this generated class to work properly.

---

### Step 2: Create InputManager GameObject

1. In your main scene (or a scene that loads first), create an **empty GameObject**
2. Name it **"InputManager"**
3. Add the **InputManager** component to it
4. **Important**: This GameObject should exist when the game starts

**Tip**: You can make this a prefab and add it to every scene, OR use DontDestroyOnLoad (already handled in code).

---

### Step 3: Configure Project Settings

1. Go to **Edit > Project Settings > Player**
2. Under **"Active Input Handling"**, select:
   - **"Input System Package (New)"** (recommended)
   - OR **"Both"** if you want to keep old input temporarily

3. Unity will ask to restart - click **"Yes"**

---

## How to Use InputManager in Your Code

### Reading Continuous Input (Movement, Mouse)

```csharp
// Get movement input (returns Vector2)
Vector2 moveInput = InputManager.Instance.MoveInput;

// Get mouse screen position
Vector2 mousePos = InputManager.Instance.MousePosition;

// Get mouse world position (auto-calculated)
Vector2 mouseWorldPos = InputManager.Instance.MouseWorldPosition;

// Get direction from a position to mouse
Vector2 direction = InputManager.Instance.GetMouseDirectionFrom(transform.position);

// Check if shift is held for camera pan
bool isShiftHeld = InputManager.Instance.IsExtendingCameraPan;
```

### Subscribing to Button Events

```csharp
private void Start()
{
    InputManager.Instance.OnAttackPressed += HandleAttack;
    InputManager.Instance.OnThrowPressed += HandleThrow;
    InputManager.Instance.OnPickupPressed += HandlePickup;
}

private void OnDestroy()
{
    // ALWAYS unsubscribe to prevent memory leaks!
    if (InputManager.Instance != null)
    {
        InputManager.Instance.OnAttackPressed -= HandleAttack;
        InputManager.Instance.OnThrowPressed -= HandleThrow;
        InputManager.Instance.OnPickupPressed -= HandlePickup;
    }
}

private void HandleAttack()
{
    // Attack logic here
}
```

---

## Current Input Mappings

| Action | Keyboard Binding | Type |
|--------|------------------|------|
| **Move** | WASD / Arrow Keys | Vector2 (continuous) |
| **Attack** | Left Mouse Button | Button (event) |
| **Throw** | Right Mouse Button | Button (event) |
| **Pickup** | E | Button (event) |
| **Mouse Position** | Mouse | Vector2 (continuous) |
| **Extend Camera Pan** | Left Shift | Button (hold) |

---

## How to Add New Inputs

### Option 1: Using Unity Editor (Easiest)

1. Open `GameControls.inputactions`
2. Click **"Edit asset"** in Inspector
3. Add new Action in the **Player** action map
4. Add binding(s) for that action
5. Click **"Save Asset"**
6. Click **"Apply"** to regenerate the C# class

### Option 2: Adding to InputManager (After editing .inputactions)

1. Open `InputManager.cs`
2. Add a property or event:

```csharp
// For continuous input (like axis)
public float NewAxisInput { get; private set; }

// For button press
public event System.Action OnNewButtonPressed;
```

3. Add callback in `SetupInputCallbacks()`:

```csharp
controls.Player.NewAction.performed += ctx => OnNewButtonPressed?.Invoke();
```

---

## Troubleshooting

### "GameControls does not exist in the current context"
- You forgot to generate the C# class from the .inputactions file
- Select the .inputactions file, check "Generate C# Class", click "Apply"

### "InputManager.Instance is null"
- You forgot to create the InputManager GameObject in your scene
- Create an empty GameObject and add the InputManager component

### Input not working
- Make sure the InputManager GameObject exists and is active
- Check Console for errors
- Make sure you enabled the new Input System in Project Settings

### Want to temporarily use old input?
- Set "Active Input Handling" to "Both" in Project Settings
- You can mix old and new input during migration

---

## Benefits of This Approach

✅ **Modular** - All input logic in one place (InputManager)
✅ **Clean** - No scattered `Input.GetKey()` calls
✅ **Event-driven** - Button presses use C# events (cleaner than polling)
✅ **Easy to extend** - Adding gamepad support = just add bindings in .inputactions
✅ **Testable** - Can easily mock InputManager for testing
✅ **Performance** - Input System is more efficient than old Input Manager

---

## Migration Complete!

All gameplay input has been migrated to the new Input System. The old `Input.` calls have been replaced with `InputManager.Instance` calls.

**Next Steps:**
1. Generate the C# class (Step 1 above)
2. Create InputManager GameObject (Step 2 above)
3. Test all inputs in play mode
4. Optionally add gamepad support by adding bindings in GameControls.inputactions
