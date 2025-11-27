using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Handles enemy item seeking behavior
/// Works as a behavior helper - state is managed by EnemyAI
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

    // Item tracking
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
            seekInterval = seekIntervalOverride > 0 ? seekIntervalOverride : 2f;
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

        // Initial weapon search on spawn
        if (autoSeekItems && !itemHolder.HasWeapon())
        {
            Invoke(nameof(InitialWeaponSearch), 0.5f);
        }
    }

    /// <summary>
    /// Initial weapon search on spawn
    /// </summary>
    private void InitialWeaponSearch()
    {
        if (!itemHolder.HasWeapon())
        {
            TryFindAndSeekItem();
        }
    }
    
    private void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        
        // If seeking item, update the seeking behavior
        if (isSeekingItem)
        {
            UpdateItemSeeking();
        }
    }
    
    /// <summary>
    /// Updates item seeking behavior
    /// </summary>
    private void UpdateItemSeeking()
    {
        // Target item still valid?
        if (targetItem == null || targetItem.IsDepleted() || targetItem.transform.parent != null)
        {
            StopSeekingItem();
            return;
        }
        
        float distanceToItem = Vector2.Distance(transform.position, targetItem.transform.position);
        
        // Within pickup radius?
        if (distanceToItem <= pickupRadius)
        {
            bool success = targetItem.TryPickupByEnemy(itemHolder);
            StopSeekingItem();
            
            if (success && enemyAI != null)
            {
                // Notify EnemyAI that we got a weapon
                enemyAI.OnWeaponAcquired();
            }
        }
        else
        {
            // Update destination if item moved
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
    /// Stops seeking and clears target
    /// </summary>
    private void StopSeekingItem()
    {
        isSeekingItem = false;
        targetItem = null;
    }
    
    /// <summary>
    /// Finds nearby items and navigates to the best one
    /// </summary>
    public void TryFindAndSeekItem()
    {
        if (isSeekingItem) return;
        
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, seekRadius, itemLayerMask);
        
        PickupableItem bestItem = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            PickupableItem item = col.GetComponent<PickupableItem>();
            
            if (item == null || item.IsDepleted() || !item.canBePickedByEnemies)
                continue;
            
            // Skip if already held
            if (item.transform.parent != null)
                continue;
            
            float distance = Vector2.Distance(transform.position, item.transform.position);
            
            // Prefer primary weapons if configured
            if (preferPrimaryItems && item.holdType == ItemHoldType.Primary)
            {
                if (bestItem == null || bestItem.holdType != ItemHoldType.Primary || distance < closestDistance)
                {
                    bestItem = item;
                    closestDistance = distance;
                }
            }
            else if (bestItem == null || (bestItem.holdType != ItemHoldType.Primary && distance < closestDistance))
            {
                bestItem = item;
                closestDistance = distance;
            }
        }
        
        if (bestItem != null)
        {
            StartSeekingItem(bestItem);
        }
    }
    
    /// <summary>
    /// Starts navigating to a specific item
    /// </summary>
    private void StartSeekingItem(PickupableItem item)
    {
        targetItem = item;
        isSeekingItem = true;
        lastItemPosition = item.transform.position;
        
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(item.transform.position);
        }
    }
    
    /// <summary>
    /// Try to seek a specific item
    /// </summary>
    public bool TrySeekSpecificItem(GameObject itemObject)
    {
        if (itemObject == null) return false;
        
        PickupableItem item = itemObject.GetComponent<PickupableItem>();
        if (item == null || item.IsDepleted() || !item.canBePickedByEnemies || item.transform.parent != null)
            return false;
        
        StartSeekingItem(item);
        return true;
    }
    
    /// <summary>
    /// Finds and returns the nearest valid item (does not navigate)
    /// </summary>
    public PickupableItem FindNearestItem()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, seekRadius, itemLayerMask);
        
        PickupableItem nearestItem = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            PickupableItem item = col.GetComponent<PickupableItem>();
            
            if (item == null || item.IsDepleted() || !item.canBePickedByEnemies || item.transform.parent != null)
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
        float drawSeekRadius = seekRadius > 0 ? seekRadius : (seekRadiusOverride > 0 ? seekRadiusOverride : 10f);
        float drawPickupRadius = pickupRadius > 0 ? pickupRadius : (pickupRadiusOverride > 0 ? pickupRadiusOverride : 1.5f);
        
        // Seek radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, drawSeekRadius);
        
        // Pickup radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, drawPickupRadius);
        
        // Target item line
        if (isSeekingItem && targetItem != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetItem.transform.position);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetItem.transform.position, 0.5f);
        }
    }
    
    // Public getters
    public bool IsSeekingItem => isSeekingItem;
    public PickupableItem TargetItem => targetItem;
    
    /// <summary>
    /// Cancel current item seeking
    /// </summary>
    public void CancelSeeking()
    {
        if (isSeekingItem)
        {
            StopSeekingItem();
        }
    }
}
