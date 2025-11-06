using UnityEngine;

/// <summary>
/// EnemyAI için item kullanımını entegre eden örnek script
/// Bu script EnemyAI ile EnemyItemHolder'ı birleştirir
/// </summary>
[RequireComponent(typeof(EnemyAI))]
[RequireComponent(typeof(EnemyItemHolder))]
public class EnemyItemIntegration : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 10f; // Ateş etme menzili
    [SerializeField] private float attackCooldown = 1f; // Saldırılar arası bekleme süresi
    [SerializeField] private bool useItemsInCombat = true; // Savaşta item kullan
    
    [Header("Melee Settings")]
    [SerializeField] private float meleeRange = 2f; // Yakın dövüş menzili
    
    private EnemyAI enemyAI;
    private EnemyItemHolder itemHolder;
    private Transform target;
    private float lastAttackTime;
    
    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        itemHolder = GetComponent<EnemyItemHolder>();
        
        // Enemy öldüğünde item'ları düşür
        if (enemyAI != null)
        {
            enemyAI.OnEnemyDeath += OnEnemyDeath;
        }
    }
    
    private void Update()
    {
        if (!useItemsInCombat || !itemHolder.HasWeapon())
            return;
        
        // Target'ı bul (player'ı ara)
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        
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
                // Enemy'nin görüş yönünü kullanarak saldır
                Vector3 visionDirection = enemyAI.GetVisionDirection();
                Vector3 attackDirection = (target.position - transform.position).normalized;
                
                // Görüş yönü ile hedef arasındaki açıyı kontrol et (isteğe bağlı)
                float angleToTarget = Vector3.Angle(visionDirection, attackDirection);
                
                // Primary weapon (tabanca) - uzaktan
                if (item.holdType == ItemHoldType.Primary && distanceToTarget <= attackRange)
                {
                    // Hedef görüş açısında mı kontrol et (opsiyonel - yorumu kaldırabilirsin)
                    // if (angleToTarget <= 45f) // Sadece 45 derece içindeyken ateş et
                    itemHolder.PerformAttack(target.position);
                    lastAttackTime = Time.time;
                }
                // Secondary weapon (sopa) - yakından
                else if (item.holdType == ItemHoldType.Secondary && distanceToTarget <= meleeRange)
                {
                    itemHolder.PerformAttack(target.position);
                    lastAttackTime = Time.time;
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
