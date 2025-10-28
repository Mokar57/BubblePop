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
- **Persistent Chase Mode**: Once player is spotted, enemy chases forever
- **Dynamic Vision Direction**: Vision cone rotates based on movement direction
- **Cone-Shaped Vision**: Configurable range and angle for realistic sight
- **Smooth Rotation**: Vision direction changes smoothly, not instantly
- **Obstacle Detection**: Walls and objects can block enemy vision
- **Relentless Pursuit**: No escape once spotted (unless you go very far)
- **Smart Following**: Different behavior when target is visible vs. lost
- **Performance Optimization**: Updates destination periodically instead of every frame
- **Visual Debug System**: Shows vision cone, ranges, and target states in Scene view
- **Public API**: Methods for external scripts to control the enemy

### Inspector Settings:

#### Target Settings:
- **Target**: Manual target assignment (optional if Auto Find Player is enabled)
- **Auto Find Player**: Automatically finds the player GameObject

#### Vision Settings:
- **Vision Range**: Maximum distance the enemy can see (default: 8)
- **Vision Angle**: Total cone angle in degrees (default: 60°)
- **Obstacle Layer Mask**: What layers block the enemy's vision
- **Draw Vision Cone**: Show vision cone in Scene view for debugging
- **Rotation Speed**: How fast vision rotates when moving (degrees/second, default: 90°)
- **Min Move Speed For Rotation**: Minimum movement speed to start rotating vision (default: 0.1)

#### AI Settings:
- **Follow Distance**: Maximum distance to follow once target is spotted (default: 15)
- **Stop Distance**: Distance to stop moving toward player (default: 1)
- **Update Rate**: How often to update destination in seconds (default: 0.1)
- **Persistent Chase**: Once spotted, chase forever until manually reset (default: true)
- **Max Chase Distance**: Maximum distance before giving up persistent chase (default: 50)

## 🎮 **Yeni Oynanış Mekanikleri:**

### ⚠️ **Persistent Chase Mode (Sürekli Kovalama)**:
1. **İlk Keşif**: Düşmanın görüş alanına ilk girişin çok kritik!
2. **Sürekli Kovalama**: Bir kez görüldükten sonra düşman seni asla bırakmaz
3. **Uzak Kaçış**: Sadece çok uzağa (Max Chase Distance) kaçarak kurtulabilirsin
4. **Strateji**: Düşmanın görüş açısını ve menzilini hesaplayarak gizli kal
5. **Reset Gerekliliği**: Başka düşmanlarla savaş veya script ile chase'i resetle

### 🎯 **Taktiksel Öneriler**:
- **Gizli Kalın**: İlk görülmemek en iyi strateji
- **Uzaktan Planlama**: Düşmanın konumunu ve görüş yönünü gözlemle
- **Hızlı Karar**: Görüldüğün an hızla uzaklaş veya savaşa hazırlan
- **Menzil Bilgisi**: Max Chase Distance'ı bil ve o mesafeyi koru

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

### 4. Obstacle Setup (for vision blocking):
1. **Create Obstacle Layer**:
   - Go to `Edit > Project Settings > Tags and Layers`
   - Add "Obstacles" layer
   - Assign wall/obstacle GameObjects to this layer

2. **Configure Enemy Vision**:
   - Set "Obstacle Layer Mask" in EnemyAI to include Obstacles layer
   - This allows walls to block enemy vision

### 5. Testing:
1. **Create Player Layer**:
   - Go to `Edit > Project Settings > Tags and Layers`
   - Add "Player" layer
   - Assign player GameObject to Player layer

2. **Configure EnemyController**:
   - Set "Player Layer" in EnemyController to match your Player layer

### 5. Testing:
1. **Vision Testing**:
   - Play the game
   - Enemy should only follow when you enter its vision cone
   - Check Scene view to see vision cone visualization (yellow/red cone)
   - Test hiding behind obstacles to break line of sight

2. **Debug Visualization**:
   - Select enemy in Scene view during play
   - **Yellow cone**: Normal vision state
   - **Red cone**: Target spotted and following
   - **White arrow**: Current vision direction
   - **Green line**: Direct line of sight to target
   - **Magenta circle**: Persistent chase range (max distance)
   - **Orange line**: Following last known position (non-persistent mode)
   - **Cyan circle**: Follow range when target is spotted
   - Vision cone rotates as enemy moves!
   - Once spotted, enemy will chase you across the entire level!

---

## Script APIs:

### EnemyAI Public Methods:
```csharp
// Target control
enemyAI.SetTarget(newTarget);
enemyAI.ForceSpotTarget(); // Make enemy immediately spot current target

// Movement control
enemyAI.SetSpeed(5f);
enemyAI.StopFollowing();
enemyAI.ResumeFollowing();

// Vision control
enemyAI.SetVisionRange(10f);
enemyAI.SetVisionAngle(90f);
enemyAI.SetRotationSpeed(120f);
enemyAI.SetVisionDirection(Vector3.up);
Vector3 currentDirection = enemyAI.GetVisionDirection();

// Chase control
enemyAI.SetPersistentChase(true);
enemyAI.SetMaxChaseDistance(100f);
enemyAI.ResetChase(); // Stop chasing and reset to patrol mode
bool isPersistent = enemyAI.IsPersistentChaseEnabled();
float maxDistance = enemyAI.GetMaxChaseDistance();

// Status checking
bool isMoving = enemyAI.IsMoving();
bool canSeeTarget = enemyAI.CanSeeTargetPublic();
bool isFollowing = enemyAI.IsFollowingTarget();
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