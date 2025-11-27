using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Enemy'nin yakındaki item'ları bulmasını ve almasını sağlar
/// REFACTORED: Now uses EnemyDataSO for configuration
/// </summary>
[RequireComponent(typeof(EnemyItemHolder))]
public class EnemyItemSeeker : MonoBehaviour
{
    [Header("Enemy Data")]
    [Tooltip("ScriptableObject containing enemy configuration")]
    public EnemyDataSO enemyData;

    [Header("Item Seeking Settings (Override)")]
    [Tooltip("Leave at 0 to use enemyData values")]
    [SerializeField] private float seekRadiusOverride = 0f;
    [SerializeField] private float pickupRadiusOverride = 0f;
    [SerializeField] private float seekIntervalOverride = 0f;

    [Header("Detection")]
    [SerializeField] private LayerMask itemLayerMask = -1;

    [Header("Navigation")]
    [SerializeField] private bool pauseAIDuringSeek = false;

    [Header("Priority Settings")]
    [Tooltip("Pause AI patrol until initial weapon is found")]
    [SerializeField] private bool pausePatrolUntilArmed = true;

    private EnemyItemHolder itemHolder;
    private EnemyAI enemyAI;
    private NavMeshAgent agent;
    private float lastSeekTime;

    // Runtime values from SO
    private bool autoSeekItems;
    private float seekRadius;
    private float pickupRadius;
    private float seekInterval;
    private bool preferPrimaryItems;
    private bool onlySeekWhenUnarmed;

    // Initial weapon search state
    private bool hasCompletedInitialSearch = false;
    
    // Item takip durumu
    private PickupableItem targetItem;
    private bool isSeekingItem = false;
    private Vector3 lastItemPosition;
    
    private void Start()
    {
        itemHolder = GetComponent<EnemyItemHolder>();
        enemyAI = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
        lastSeekTime = Time.time;

        // Initialize values from ScriptableObject or override
        if (enemyData != null)
        {
            autoSeekItems = enemyData.canSeekItems;
            seekRadius = seekRadiusOverride > 0 ? seekRadiusOverride : enemyData.itemSeekRadius;
            pickupRadius = pickupRadiusOverride > 0 ? pickupRadiusOverride : enemyData.itemPickupRadius;
            seekInterval = seekIntervalOverride > 0 ? seekIntervalOverride : 2f; // Faster initial check
            preferPrimaryItems = enemyData.preferPrimaryWeapons;
            onlySeekWhenUnarmed = enemyData.onlySeekWhenUnarmed;
        }
        else
        {
            autoSeekItems = true;
            seekRadius = seekRadiusOverride > 0 ? seekRadiusOverride : 10f;
            pickupRadius = pickupRadiusOverride > 0 ? pickupRadiusOverride : 1.5f;
            seekInterval = seekIntervalOverride > 0 ? seekIntervalOverride : 2f;
            preferPrimaryItems = true;
            onlySeekWhenUnarmed = true;
            Debug.LogWarning($"{gameObject.name}: No EnemyDataSO assigned! Using default item seeking settings.");
        }

        // Immediate weapon search on spawn (prioritize arming before patrol)
        if (autoSeekItems && !itemHolder.HasWeapon())
        {
            // Pause AI patrol until weapon is found
            if (pausePatrolUntilArmed && enemyAI != null)
            {
                enemyAI.enabled = false; // Disable AI temporarily
            }

            Invoke(nameof(InitialWeaponSearch), 0.5f); // Small delay for NavMesh initialization
        }
        else
        {
            hasCompletedInitialSearch = true;
        }
    }

    /// <summary>
    /// Initial weapon search on spawn - runs once before patrol starts
    /// </summary>
    private void InitialWeaponSearch()
    {
        if (!itemHolder.HasWeapon())
        {
            TryFindAndSeekItem();
        }
        else
        {
            CompleteInitialSearch();
        }
    }

    /// <summary>
    /// Called when initial weapon search is complete
    /// </summary>
    private void CompleteInitialSearch()
    {
        hasCompletedInitialSearch = true;

        // Re-enable AI if it was paused
        if (pausePatrolUntilArmed && enemyAI != null && !enemyAI.enabled)
        {
            enemyAI.enabled = true;
        }
    }
    
    private void Update()
    {
        // Check if agent is valid and active before doing anything
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        
        // Item takip modunda mıyız?
        if (isSeekingItem)
        {
            UpdateItemSeeking();
            return;
        }
        
        // Otomatik item arama
        if (!autoSeekItems) return;
        
        // Belirli aralıklarla item ara
        if (Time.time - lastSeekTime >= seekInterval)
        {
            lastSeekTime = Time.time;
            
            if (onlySeekWhenUnarmed && itemHolder.HasWeapon())
                return;
            
            TryFindAndSeekItem();
        }
    }
    
    /// <summary>
    /// Item takibini günceller ve yaklaşınca alır
    /// </summary>
    private void UpdateItemSeeking()
    {
        // Target item hala geçerli mi?
        if (targetItem == null || targetItem.IsDepleted() || targetItem.transform.parent != null)
        {
            StopSeekingItem();
            return;
        }
        
        float distanceToItem = Vector2.Distance(transform.position, targetItem.transform.position);
        
        // Item pickup radius içinde mi?
        if (distanceToItem <= pickupRadius)
        {
            // Item'ı almaya çalış
            bool success = targetItem.TryPickupByEnemy(itemHolder);
            if (success)
            {
                StopSeekingItem();
            }
            else
            {
                // Başarısız olduysa vazgeç
                StopSeekingItem();
            }
        }
        else
        {
            // Item hareket ettiyse pozisyonu güncelle
            if (Vector3.Distance(targetItem.transform.position, lastItemPosition) > 0.5f)
            {
                lastItemPosition = targetItem.transform.position;
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.SetDestination(targetItem.transform.position);
                }
            }
        }
    }
    
    /// <summary>
    /// Item takibini durdurur ve normal AI davranışına döner
    /// </summary>
    private void StopSeekingItem()
    {
        isSeekingItem = false;
        targetItem = null;

        // AI'ı yeniden etkinleştir (eğer durdurulmuşsa)
        if (pauseAIDuringSeek && enemyAI != null)
        {
            enemyAI.enabled = true;
        }

        // Complete initial search if this was the first weapon pickup
        if (!hasCompletedInitialSearch && itemHolder.HasWeapon())
        {
            CompleteInitialSearch();
        }
    }
    
    /// <summary>
    /// Yakındaki item'ları bulur ve en uygununa doğru gitmeye başlar
    /// </summary>
    public void TryFindAndSeekItem()
    {
        // Zaten item arıyorsa yeni arama yapma
        if (isSeekingItem) return;
        
        // Yakındaki tüm collider'ları bul
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, seekRadius, itemLayerMask);
        
        PickupableItem bestItem = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            PickupableItem item = col.GetComponent<PickupableItem>();
            
            // Item geçerli mi kontrol et
            if (item == null || item.IsDepleted() || !item.canBePickedByEnemies)
                continue;
            
            // Item zaten tutuluyorsa atla
            if (item.transform.parent != null)
                continue;
            
            float distance = Vector2.Distance(transform.position, item.transform.position);
            
            // Eğer preference varsa ve bu item uygunsa
            if (preferPrimaryItems && item.holdType == ItemHoldType.Primary)
            {
                // Primary item'lar için öncelik ver
                if (bestItem == null || distance < closestDistance)
                {
                    bestItem = item;
                    closestDistance = distance;
                }
            }
            else
            {
                // En yakın item'ı al
                if (bestItem == null || distance < closestDistance)
                {
                    bestItem = item;
                    closestDistance = distance;
                }
            }
        }
        
        // En iyi item'ı bulduysa ona doğru git
        if (bestItem != null)
        {
            StartSeekingItem(bestItem);
        }
    }
    
    /// <summary>
    /// Belirli bir item'a doğru gitmeye başlar
    /// </summary>
    private void StartSeekingItem(PickupableItem item)
    {
        targetItem = item;
        isSeekingItem = true;
        lastItemPosition = item.transform.position;
        
        // NavMeshAgent ile item'a git
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(item.transform.position);
        }
        
        // AI'ı duraklat (eğer ayarlanmışsa)
        if (pauseAIDuringSeek && enemyAI != null)
        {
            enemyAI.enabled = false;
        }
    }
    
    /// <summary>
    /// [DEPRECATED] Yakındaki item'ları bulur ve en uygununu alır
    /// Bu metod artık kullanılmıyor, TryFindAndSeekItem kullanın
    /// </summary>
    [System.Obsolete("Use TryFindAndSeekItem instead")]
    public void TryFindAndPickupNearbyItem()
    {
        TryFindAndSeekItem();
    }
    
    /// <summary>
    /// Belirtilen item'a doğru gitmeye başlar
    /// </summary>
    public bool TrySeekSpecificItem(GameObject itemObject)
    {
        if (itemObject == null) return false;
        
        PickupableItem item = itemObject.GetComponent<PickupableItem>();
        if (item == null) return false;
        
        if (item.IsDepleted() || !item.canBePickedByEnemies || item.transform.parent != null)
            return false;
        
        StartSeekingItem(item);
        return true;
    }
    
    /// <summary>
    /// [DEPRECATED] Belirtilen item'ı almaya çalışır
    /// Bu metod artık item'a gitmeden direkt almaya çalışır
    /// </summary>
    [System.Obsolete("Use TrySeekSpecificItem for navigating to item")]
    public bool TryPickupSpecificItem(GameObject itemObject)
    {
        if (itemObject == null) return false;
        
        PickupableItem item = itemObject.GetComponent<PickupableItem>();
        if (item == null) return false;
        
        // Yakında mı kontrol et
        float distance = Vector2.Distance(transform.position, item.transform.position);
        if (distance <= pickupRadius)
        {
            return item.TryPickupByEnemy(itemHolder);
        }
        else
        {
            // Uzaktaysa ona doğru git
            StartSeekingItem(item);
            return false; // Henüz alınmadı
        }
    }
    
    /// <summary>
    /// En yakın item'ı bulur ve döndürür
    /// </summary>
    public PickupableItem FindNearestItem()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, seekRadius, itemLayerMask);
        
        PickupableItem nearestItem = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            PickupableItem item = col.GetComponent<PickupableItem>();
            
            if (item == null || item.IsDepleted() || !item.canBePickedByEnemies)
                continue;
            
            if (item.transform.parent != null)
                continue;
            
            float distance = Vector2.Distance(transform.position, item.transform.position);
            
            if (distance < closestDistance)
            {
                nearestItem = item;
                closestDistance = distance;
            }
        }
        
        return nearestItem;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Seek radius'u göster (algılama alanı)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, seekRadius);
        
        // Pickup radius'u göster (alma alanı)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        
        // Target item varsa çizgi çek
        if (isSeekingItem && targetItem != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetItem.transform.position);
            
            // Target item'ı vurgula
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetItem.transform.position, 0.5f);
        }
    }
    
    /// <summary>
    /// Public getter: Enemy şu anda item arıyor mu?
    /// </summary>
    public bool IsSeekingItem => isSeekingItem;
    
    /// <summary>
    /// Public getter: Target item
    /// </summary>
    public PickupableItem TargetItem => targetItem;
    
    /// <summary>
    /// Manuel olarak item aramayı iptal et
    /// </summary>
    public void CancelSeeking()
    {
        if (isSeekingItem)
        {
            StopSeekingItem();
        }
    }
}
