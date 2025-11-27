using UnityEngine;

/// <summary>
/// EnemyAI için item kullanımını entegre eden örnek script
/// Bu script EnemyAI ile EnemyItemHolder'ı birleştirir
/// REFACTORED: Now uses EnemyDataSO for configuration
/// </summary>
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyItemHolder))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyItemIntegration : MonoBehaviour
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Combat Settings (Override)")]
    [Tooltip("Leave at 0 to use enemyData values")]
    [SerializeField] private float attackRangeOverride = 0f;
    [SerializeField] private float attackCooldownOverride = 0f;
    [SerializeField] private float meleeRangeOverride = 0f;

    [Header("Line of Sight Settings")]
    [SerializeField] private LayerMask obstacleMask = -1;

    private EnemyAI enemyAI;
    private EnemyItemHolder itemHolder;
    private EnemyHealth health;
    private Transform target;
    private float lastAttackTime;

    // Runtime values from SO
    private float attackRange;
    private float attackCooldown;
    private float meleeRange;
    private bool useItemsInCombat;

    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        itemHolder = GetComponent<EnemyItemHolder>();
        health = GetComponent<EnemyHealth>();

        // Initialize values from ScriptableObject or override
        if (enemyData != null)
        {
            attackRange = attackRangeOverride > 0 ? attackRangeOverride : enemyData.weaponAttackRange;
            attackCooldown = attackCooldownOverride > 0 ? attackCooldownOverride : enemyData.weaponAttackCooldown;
            meleeRange = meleeRangeOverride > 0 ? meleeRangeOverride : enemyData.meleeAttackRange;
            useItemsInCombat = enemyData.canUseWeapons;
        }
        else
        {
            attackRange = attackRangeOverride > 0 ? attackRangeOverride : 10f;
            attackCooldown = attackCooldownOverride > 0 ? attackCooldownOverride : 1f;
            meleeRange = meleeRangeOverride > 0 ? meleeRangeOverride : 2f;
            useItemsInCombat = true;
            Debug.LogWarning($"{gameObject.name}: No EnemyDataSO assigned! Using default combat settings.");
        }

        // Enemy öldüğünde item'ları düşür
        if (health != null)
        {
            health.OnEnemyDeath += OnEnemyDeath;
        }
    }
    
    private void Update()
    {
        // Target'ı bul (player'ı ara)
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        
        // Silahın kullanım hakkını kontrol et ve bittiğinde fırlat
        if (itemHolder.HasWeapon())
        {
            GameObject currentWeapon = itemHolder.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                PickupableItem weaponItem = currentWeapon.GetComponent<PickupableItem>();
                if (weaponItem != null && weaponItem.IsDepleted())
                {
                    // Kullanım hakkı biten silahı player'a fırlat
                    ThrowDepletedWeaponAtTarget();
                    return; // Silahı fırlattıktan sonra saldırı yapma
                }
            }
        }
        
        if (!useItemsInCombat || !itemHolder.HasWeapon())
            return;

        // Enemy sadece Chasing modundayken saldırsın
        // (State is now managed by EnemyAI's state pattern, check current state directly)
        if (enemyAI.CurrentState != enemyAI.ChaseStateInstance) // Use the state instance
            return;
        
        if (target == null)
            return;
        
        // Attack cooldown kontrolü
        if (Time.time - lastAttackTime < attackCooldown)
            return;
        
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        
        // Silahın tipine göre saldır
        GameObject weapon = itemHolder.GetCurrentWeapon();
        if (weapon != null)
        {
            PickupableItem item = weapon.GetComponent<PickupableItem>();
            
            if (item != null)
            {
                // Primary weapon (tabanca) - uzaktan
                // Sadece player görüş açısındaysa VE arada engel yoksa ateş et
                if (item.holdType == ItemHoldType.Primary && distanceToTarget <= attackRange)
                {
                    // Hem EnemyAI görüş kontrolünü hem de kendi line of sight kontrolümüzü yap
                    if (enemyAI.VisionSystem.CanSeeTarget() && HasLineOfSight(target.position))
                    {
                        itemHolder.PerformAttack(target.position);
                        lastAttackTime = Time.time;
                    }
                }
                // Secondary weapon (sopa) - yakından
                // Melee silahlar için de sadece Chasing modundayken saldır
                else if (item.holdType == ItemHoldType.Secondary && distanceToTarget <= meleeRange)
                {
                    // For melee/secondary attacks, also ensure no friendly or obstacle is between shooter and target
                    if (HasLineOfSight(target.position))
                    {
                        itemHolder.PerformAttack(target.position);
                        lastAttackTime = Time.time;
                    }
                }
            }
        }
    }
    
    private void OnEnemyDeath()
    {
        // Enemy öldüğünde tüm item'ları düşür
        if (itemHolder != null)
        {
            itemHolder.DropAllItems();
        }
    }
    
    /// <summary>
    /// Kullanım hakkı biten silahı target'a doğru fırlatır
    /// </summary>
    private void ThrowDepletedWeaponAtTarget()
    {
        if (target != null)
        {
            // Player'ın mevcut pozisyonuna doğru fırlat
            Vector2 targetPosition = target.position;
            
            // Player'ın hareketini tahmin et (eğer Rigidbody2D varsa)
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                // Player'ın velocity'sine göre gelecekteki pozisyonunu tahmin et
                float predictionTime = 0.5f; // 0.5 saniye sonrasını tahmin et
                targetPosition += targetRb.linearVelocity * predictionTime;
            }
            
            // Hedef pozisyona doğru yön hesapla
            Vector2 throwDirection = (targetPosition - (Vector2)transform.position).normalized;
            itemHolder.ThrowCurrentItem(throwDirection);
        }
        else
        {
            // Target yoksa görüş yönüne fırlat
            Vector2 throwDirection = enemyAI.GetVisionDirection();
            itemHolder.ThrowCurrentItem(throwDirection);
        }
    }
    
    /// <summary>
    /// Hedef ile arada engel olup olmadığını kontrol eder (raycast ile)
    /// </summary>
    private bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector2 directionToTarget = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        // Use RaycastAll so we can inspect hits in order and detect if another enemy is between shooter and target.
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, directionToTarget, distanceToTarget, obstacleMask);

        if (hits == null || hits.Length == 0)
        {
            // Nothing hit: clear line of sight
            return true;
        }

        // Sort hits by distance to ensure we evaluate nearest first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            GameObject hitObj = hit.collider.gameObject;

            // Ignore self-collisions (colliders attached to this enemy)
            if (hitObj == gameObject) continue;

            // If the first relevant hit is the target (player), clear line of sight
            if ((target != null && hitObj == target.gameObject) || hitObj.GetComponent<PlayerControls>() != null || hitObj.CompareTag("Player"))
            {
                return true;
            }

            // If hit an Enemy before reaching player, block the shot
            if (hitObj.GetComponent<EnemyAI>() != null || hitObj.CompareTag("Enemy"))
            {
                return false;
            }

            // If hit any other obstacle (wall, etc.), block the shot
            return false;
        }

        // If we fell through, consider blocked
        return false;
    }
    
    /// <summary>
    /// Public method: Enemy'yi item atmaya zorla
    /// </summary>
    public void ForceThrowItem()
    {
        if (itemHolder.HasWeapon())
        {
            if (target != null)
            {
                // Hedef varsa hedefe doğru fırlat
                Vector2 throwDirection = (target.position - transform.position).normalized;
                itemHolder.ThrowCurrentItem(throwDirection);
            }
            else if (enemyAI != null)
            {
                // Hedef yoksa görüş yönüne fırlat
                Vector2 throwDirection = enemyAI.GetVisionDirection();
                itemHolder.ThrowCurrentItem(throwDirection);
            }
        }
    }
    
    /// <summary>
    /// Public method: Belirli bir durumda item kullanımını aç/kapat
    /// </summary>
    public void SetItemUsage(bool canUse)
    {
        useItemsInCombat = canUse;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Attack range'i göster
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Melee range'i göster
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
