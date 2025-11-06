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
    
    [Header("Line of Sight Settings")]
    [SerializeField] private LayerMask obstacleMask = -1; // Engelleri belirleyen layer mask (duvarlar vs.)
    
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
        if (enemyAI.GetCurrentState() != EnemyAI.AIState.Chasing)
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
                    if (enemyAI.CanSeeTargetPublic() && HasLineOfSight(target.position))
                    {
                        itemHolder.PerformAttack(target.position);
                        lastAttackTime = Time.time;
                    }
                }
                // Secondary weapon (sopa) - yakından
                // Melee silahlar için de sadece Chasing modundayken saldır
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
        
        // Enemy ve target layer'larını hariç tut
        int enemyLayer = gameObject.layer;
        int targetLayer = target != null ? target.gameObject.layer : -1;
        
        // Obstacle mask'ten enemy ve target layer'larını çıkar
        int raycastMask = obstacleMask;
        if (enemyLayer >= 0)
            raycastMask &= ~(1 << enemyLayer);
        if (targetLayer >= 0)
            raycastMask &= ~(1 << targetLayer);
        
        // Raycast ile engel kontrolü
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, distanceToTarget, raycastMask);
        
        // Eğer raycast bir şeye çarptıysa, engel var demektir
        return hit.collider == null;
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
