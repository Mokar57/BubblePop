# NavMesh Enemy AI Setup Guide

## Overview
This system provides a complete enemy AI that uses Unity's NavMesh to follow and attack the player. The system includes:
- **EnemyAI.cs**: Handles movement and pathfinding
- **EnemyController.cs**: Handles health, combat, and enemy behaviors
- **PlayerHealth.cs**: Handles player damage and death

## Files Created/Modified:
1. `EnemyAI.cs` - Enhanced NavMesh-based movement AI
2. `EnemyController.cs` - Enemy health, combat, and behavior system
3. `PlayerHealth.cs` - Player health management system

---

## EnemyAI.cs Features:

### Core Features:
- **Automatic Player Detection**: Finds the player automatically by "Player" tag or PlayerControls component
- **Distance-Based Following**: Only follows when player is within range
- **Performance Optimization**: Updates destination periodically instead of every frame
- **Configurable Settings**: Adjustable speeds, distances, and update rates
- **Visual Debug Gizmos**: Shows follow/stop distances in Scene view
- **Public API**: Methods for external scripts to control the enemy

### Inspector Settings:

#### Target Settings:
- **Target**: Manual target assignment (optional if Auto Find Player is enabled)
- **Auto Find Player**: Automatically finds the player GameObject

#### AI Settings:
- **Follow Distance**: Maximum distance to start following the player (default: 10)
- **Stop Distance**: Distance to stop moving toward player (default: 1)
- **Update Rate**: How often to update destination in seconds (default: 0.1)

#### Movement Settings:
- **Speed**: Movement speed (default: 3.5)
- **Acceleration**: How quickly the enemy accelerates (default: 8)
- **Angular Speed**: Rotation speed (default: 120)

---

## Complete Setup Instructions:

### 1. Player Setup:
1. **Add Player Tag**: 
   - Select your player GameObject
   - In Inspector, set Tag to "Player" (create if it doesn't exist)
   
2. **Add PlayerHealth Component**:
   - Add the `PlayerHealth.cs` script to your player GameObject
   - Configure health settings in inspector

### 2. Enemy Setup:
1. **Create Enemy GameObject**:
   - Create an empty GameObject for your enemy
   - Add a sprite renderer and sprite for visual representation
   - Add a Collider2D (Circle Collider recommended)

2. **Add NavMesh Agent**:
   - Add NavMeshAgent component to enemy
   - **Important**: Set the NavMeshAgent Y position to 0 in the inspector

3. **Add Enemy Scripts**:
   - Add `EnemyAI.cs` component
   - Add `EnemyController.cs` component (optional, for combat)
   - Configure settings in inspector

### 3. NavMesh Setup:
1. **Prepare Ground**:
   - Select all ground/floor GameObjects where enemies should be able to walk
   - In Inspector, check "Navigation Static" checkbox

2. **Bake NavMesh**:
   - Open Navigation window: `Window > AI > Navigation`
   - Go to "Bake" tab
   - Adjust settings if needed:
     - Agent Radius: ~0.5 for 2D games
     - Agent Height: ~2
     - Max Slope: 45
   - Click "Bake" button

3. **Verify NavMesh**:
   - In Scene view, you should see blue areas indicating walkable NavMesh
   - Make sure the NavMesh covers areas where enemies should move

### 4. Layer Setup (for combat):
1. **Create Player Layer**:
   - Go to `Edit > Project Settings > Tags and Layers`
   - Add "Player" layer
   - Assign player GameObject to Player layer

2. **Configure EnemyController**:
   - Set "Player Layer" in EnemyController to match your Player layer

### 5. Testing:
1. **Basic Movement**:
   - Play the game
   - Enemy should automatically find and follow the player
   - Check console for any error messages

2. **Debug Visualization**:
   - Select enemy in Scene view during play
   - You should see gizmos showing follow range (yellow) and stop distance (red)
   - Green line shows path to target

---

## Script APIs:

### EnemyAI Public Methods:
```csharp
// Change target at runtime
enemyAI.SetTarget(newTarget);

// Change speed at runtime
enemyAI.SetSpeed(5f);

// Stop/Resume following
enemyAI.StopFollowing();
enemyAI.ResumeFollowing();

// Check if enemy is moving
bool isMoving = enemyAI.IsMoving();

// Get distance to target
float distance = enemyAI.DistanceToTarget();
```

### EnemyController Public Methods:
```csharp
// Damage the enemy
enemyController.TakeDamage(25f);

// Heal the enemy
enemyController.Heal(10f);

// Get enemy stats
float health = enemyController.Health;
float maxHealth = enemyController.MaxHealth;
bool isDead = enemyController.IsDead;
```

### PlayerHealth Public Methods:
```csharp
// Damage the player
playerHealth.TakeDamage(10f);

// Heal the player
playerHealth.Heal(20f);

// Full heal
playerHealth.FullHeal();

// Get player stats
float health = playerHealth.CurrentHealth;
bool isDead = playerHealth.IsDead;
```

---

## Advanced Configuration:

### Performance Optimization:
- **Update Rate**: Increase for better performance (0.2-0.5), decrease for more responsive AI (0.05-0.1)
- **Follow Distance**: Smaller values reduce NavMesh calculations
- **LOD System**: Disable AI for enemies far from camera

### Combat Tuning:
- **Attack Range**: Distance at which enemy can attack player
- **Attack Cooldown**: Time between attacks
- **Damage**: Amount of damage per attack

### Visual Feedback:
- **Damage Colors**: Customize flash colors for damage feedback
- **Death Effects**: Add particle systems or animations on death
- **Health Bars**: Connect UI elements to health systems

---

## Troubleshooting:

### Enemy Not Following:
- ✅ Check if NavMesh is baked (blue areas in Scene view)
- ✅ Verify player has "Player" tag or PlayerControls component
- ✅ Make sure enemy has NavMeshAgent component
- ✅ Check Follow Distance setting (increase if needed)
- ✅ Verify NavMeshAgent Y position is 0

### Enemy Moving Erratically:
- 🔧 Adjust Update Rate (increase value)
- 🔧 Check NavMesh quality (rebake with better settings)
- 🔧 Verify agent speed/acceleration settings

### Performance Issues:
- ⚡ Increase Update Rate value (updates less frequently)
- ⚡ Reduce Follow Distance
- ⚡ Limit number of enemies active simultaneously

### Combat Not Working:
- 🎯 Check Layer settings in EnemyController
- 🎯 Verify PlayerHealth component on player
- 🎯 Check Attack Range (visible with gizmos when enemy selected)

### NavMesh Issues:
- 🗺️ Rebake NavMesh after scene changes
- 🗺️ Check Agent Radius in Bake settings
- 🗺️ Ensure ground objects are marked "Navigation Static"

---

## Tips for 2D Games:

1. **NavMeshAgent Settings**:
   - Always set updateRotation = false
   - Always set updateUpAxis = false
   - Set Y position to 0

2. **Performance**:
   - Use Update Rate > 0.1 for better performance
   - Consider LOD system for many enemies

3. **Visual**:
   - Use Gizmos for debugging ranges and paths
   - Add sprite-based visual feedback for attacks/damage

This system provides a solid foundation for enemy AI in your 2D Unity game. You can extend it further by adding different enemy types, behaviors, or more complex combat systems!